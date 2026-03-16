using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GoldSystem.Core.Models;
using GoldSystem.Core.Services;
using GoldSystem.Data.Entities;
using Microsoft.Extensions.Logging;

namespace GoldSystem.Data.Services;

/// <summary>
/// Core billing engine – creates bills atomically, locks them after print,
/// and exposes bill retrieval. All monetary calculations are delegated to
/// <see cref="GoldSystem.Core.Services.GoldPriceCalculator"/>.
/// </summary>
public class BillingEngine : IBillingEngine
{
    private readonly IUnitOfWork _uow;
    private readonly GoldSystem.Core.Services.GoldPriceCalculator _calculator;
    private readonly IBillNumberGenerator _billGenerator;
    private readonly IBillingValidationService _validator;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<BillingEngine> _logger;

    public BillingEngine(
        IUnitOfWork uow,
        GoldSystem.Core.Services.GoldPriceCalculator calculator,
        IBillNumberGenerator billGenerator,
        IBillingValidationService validator,
        IAuditLogger auditLogger,
        ILogger<BillingEngine> logger)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _billGenerator = billGenerator ?? throw new ArgumentNullException(nameof(billGenerator));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a bill atomically. All-or-nothing: if any step fails the
    /// transaction is rolled back and nothing is persisted.
    /// </summary>
    public async Task<BillDto> CreateBillAsync(CreateBillRequest request)
    {
        // Step 1: Validate request
        var validation = await _validator.ValidateCreateBillAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(string.Join(", ", validation.Errors));

        // Step 2: Current gold rate must exist
        var currentRate = await _uow.GoldRates.GetLatestRateAsync(request.BranchId)
            ?? throw new InvalidOperationException("Gold rate not available. Cannot create bill.");

        // Step 3: Generate unique bill number
        var billNo = await _billGenerator.GenerateBillNoAsync(request.BranchId);

        // Step 4: Begin atomic transaction
        await _uow.BeginTransactionAsync();
        try
        {
            // Step 5: Calculate all line items
            var billItems = new List<BillItem>();
            var lineResults = new List<GoldSystem.Core.Services.GoldPriceCalculator.BillLineResult>();

            foreach (var itemRequest in request.Items)
            {
                var item = await _uow.Items.GetByIdAsync(itemRequest.ItemId)
                    ?? throw new KeyNotFoundException($"Item {itemRequest.ItemId} not found");

                var grossWeight = itemRequest.AdjustedGrossWeight > 0
                    ? itemRequest.AdjustedGrossWeight
                    : item.GrossWeight;
                var stoneWeight = itemRequest.AdjustedStoneWeight > 0
                    ? itemRequest.AdjustedStoneWeight
                    : item.StoneWeight;

                var input = new GoldSystem.Core.Services.GoldPriceCalculator.BillLineInput(
                    GrossWeight: grossWeight,
                    StoneWeight: stoneWeight,
                    Purity: item.Purity,
                    MakingType: item.MakingType,
                    MakingValue: item.MakingValue,
                    WastagePercent: item.WastagePercent,
                    StoneCharge: 0m,
                    Rate24KPer10g: currentRate.Rate24K);

                var lineResult = _calculator.Calculate(input);
                lineResults.Add(lineResult);

                var billItem = new BillItem
                {
                    ItemId = item.ItemId,
                    ItemName = item.Name,
                    Purity = item.Purity,
                    GrossWeight = grossWeight,
                    StoneWeight = stoneWeight,
                    NetWeight = lineResult.NetWeight,
                    WastagePercent = item.WastagePercent,
                    WastageWeight = lineResult.WastageWeight,
                    BillableWeight = lineResult.NetWeight + lineResult.WastageWeight,
                    PureGoldWeight = lineResult.PureGoldWeight,
                    RateUsed24K = currentRate.Rate24K,
                    GoldValue = lineResult.GoldValue,
                    MakingType = item.MakingType,
                    MakingValue = item.MakingValue,
                    MakingAmount = lineResult.MakingAmount,
                    StoneCharge = 0m,
                    TaxableAmount = lineResult.TaxableAmount,
                    CGST_Amount = (lineResult.GoldGST + lineResult.MakingGST) / 2m,
                    SGST_Amount = (lineResult.GoldGST + lineResult.MakingGST) / 2m,
                    LineTotal = lineResult.LineTotal
                };

                billItems.Add(billItem);
            }

            // Step 6: Calculate bill totals
            var billTotalInput = new GoldSystem.Core.Services.GoldPriceCalculator.BillTotalInput(
                LineItems: lineResults,
                DiscountAmount: request.DiscountAmount,
                ExchangeValue: request.ExchangeValue,
                IsInterState: false);

            var billTotal = _calculator.CalculateTotal(billTotalInput, request.AmountPaid);

            // Step 7: Create bill entity
            var bill = new Bill
            {
                BillNo = billNo,
                BillDate = DateOnly.FromDateTime(DateTime.Today),
                CustomerId = request.CustomerId,
                RateSnapshot22K = currentRate.Rate22K,
                RateSnapshot24K = currentRate.Rate24K,
                GoldValue = lineResults.Sum(l => l.GoldValue),
                MakingAmount = lineResults.Sum(l => l.MakingAmount),
                WastageAmount = lineResults.Sum(l => l.WastageValue),
                StoneCharge = 0m,
                SubTotal = billTotal.SubTotal,
                DiscountAmount = request.DiscountAmount,
                TaxableAmount = billTotal.TaxableAmount,
                CGST = billTotal.CGST,
                SGST = billTotal.SGST,
                IGST = billTotal.IGST,
                RoundOff = billTotal.RoundOff,
                GrandTotal = billTotal.GrandTotal,
                ExchangeValue = request.ExchangeValue,
                AmountPaid = request.AmountPaid,
                BalanceDue = billTotal.BalanceDue,
                Status = billTotal.BalanceDue <= 0m ? "Paid" : "Partial",
                PaymentMode = request.PaymentMode,
                BranchId = request.BranchId,
                UserId = request.UserId,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Bills.AddAsync(bill);
            await _uow.SaveChangesAsync();

            // Step 8: Add bill items now that bill has a PK
            foreach (var billItem in billItems)
            {
                billItem.BillId = bill.BillId;
                await _uow.BillItems.AddAsync(billItem);
            }

            // Step 9: Mark items as sold
            foreach (var itemRequest in request.Items)
            {
                var item = await _uow.Items.GetByIdAsync(itemRequest.ItemId);
                if (item != null)
                {
                    item.Status = "Sold";
                    item.SoldBillId = bill.BillId;
                    await _uow.Items.UpdateAsync(item);
                }
            }

            await _uow.SaveChangesAsync();

            // Step 10: Audit log
            await _auditLogger.LogAsync(
                userId: request.UserId,
                action: "BILL_CREATED",
                tableName: "Bills",
                recordId: bill.BillId,
                newValueJson: JsonSerializer.Serialize(new { bill.BillId, bill.BillNo, bill.GrandTotal, bill.Status }),
                branchId: request.BranchId);

            // Step 11: Commit transaction
            await _uow.CommitAsync();

            // Step 12: Return DTO
            return await GetBillAsync(bill.BillId);
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync();
            _logger.LogError(ex, "Error creating bill for customer {CustomerId}", request.CustomerId);
            throw;
        }
    }

    public async Task<BillDto> GetBillAsync(int billId)
    {
        var bill = await _uow.Bills.GetBillWithItemsAsync(billId)
            ?? throw new KeyNotFoundException($"Bill {billId} not found");

        return MapBillToDto(bill);
    }

    public async Task<BillDto> GetBillByNoAsync(string billNo)
    {
        var bill = await _uow.Bills.GetByBillNoAsync(billNo)
            ?? throw new KeyNotFoundException($"Bill '{billNo}' not found");

        return await GetBillAsync(bill.BillId);
    }

    public async Task<(bool IsLocked, string Reason)> CanEditBillAsync(int billId)
    {
        var bill = await _uow.Bills.GetByIdAsync(billId);
        if (bill?.IsLocked == true)
            return (true, "Bill is locked. Cannot edit locked bills. Use Credit Note for corrections.");
        return (false, string.Empty);
    }

    public async Task LockBillAsync(int billId, int userId = 0)
    {
        var bill = await _uow.Bills.GetByIdAsync(billId)
            ?? throw new KeyNotFoundException($"Bill {billId} not found");

        bill.IsLocked = true;
        await _uow.Bills.UpdateAsync(bill);
        await _uow.SaveChangesAsync();

        await _auditLogger.LogAsync(
            userId: userId,
            action: "BILL_LOCKED",
            tableName: "Bills",
            recordId: billId,
            branchId: bill.BranchId);
    }

    public async Task PrintBillAsync(int billId, int userId = 0)
    {
        var bill = await _uow.Bills.GetByIdAsync(billId)
            ?? throw new KeyNotFoundException($"Bill {billId} not found");

        bill.IsLocked = true;
        await _uow.Bills.UpdateAsync(bill);
        await _uow.SaveChangesAsync();

        await _auditLogger.LogAsync(
            userId: userId,
            action: "BILL_PRINTED",
            tableName: "Bills",
            recordId: billId,
            branchId: bill.BranchId);

        _logger.LogInformation("Bill {BillNo} printed and locked", bill.BillNo);
    }

    // ─── Mapping helpers ─────────────────────────────────────────────────────

    private static BillDto MapBillToDto(Bill bill)
    {
        var itemDtos = bill.BillItems.Select(bi => new BillItemDto(
            BillItemId: bi.BillItemId,
            Item: bi.Item != null
                ? new ItemDto(
                    ItemId: bi.Item.ItemId,
                    HUID: bi.Item.HUID ?? string.Empty,
                    TagNo: bi.Item.TagNo,
                    Name: bi.Item.Name,
                    Purity: bi.Item.Purity,
                    GrossWeight: bi.Item.GrossWeight,
                    StoneWeight: bi.Item.StoneWeight,
                    Status: bi.Item.Status,
                    CreatedAt: bi.Item.CreatedAt)
                : new ItemDto(0, string.Empty, string.Empty, bi.ItemName, bi.Purity,
                    bi.GrossWeight, bi.StoneWeight, string.Empty, DateTime.MinValue),
            GrossWeight: bi.GrossWeight,
            StoneWeight: bi.StoneWeight,
            NetWeight: bi.NetWeight,
            WastagePercent: bi.WastagePercent,
            WastageWeight: bi.WastageWeight,
            BillableWeight: bi.BillableWeight,
            PureGoldWeight: bi.PureGoldWeight,
            RateUsed24K: bi.RateUsed24K,
            GoldValue: bi.GoldValue,
            MakingAmount: bi.MakingAmount,
            StoneCharge: bi.StoneCharge,
            CGST_Amount: bi.CGST_Amount,
            SGST_Amount: bi.SGST_Amount,
            LineTotal: bi.LineTotal)).ToList();

        return new BillDto(
            BillId: bill.BillId,
            BillNo: bill.BillNo,
            BillDate: bill.BillDate,
            Customer: new CustomerDto(
                bill.Customer.CustomerId,
                bill.Customer.Name,
                bill.Customer.Phone),
            Items: itemDtos,
            GoldValue: bill.GoldValue,
            MakingAmount: bill.MakingAmount,
            WastageAmount: bill.WastageAmount,
            StoneCharge: bill.StoneCharge,
            DiscountAmount: bill.DiscountAmount,
            CGST: bill.CGST,
            SGST: bill.SGST,
            IGST: bill.IGST,
            RoundOff: bill.RoundOff,
            GrandTotal: bill.GrandTotal,
            ExchangeValue: bill.ExchangeValue,
            AmountPaid: bill.AmountPaid,
            BalanceDue: bill.BalanceDue,
            Status: bill.Status,
            PaymentMode: bill.PaymentMode,
            IsLocked: bill.IsLocked,
            CreatedAt: bill.CreatedAt);
    }
}
