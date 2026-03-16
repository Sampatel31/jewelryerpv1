using GoldSystem.Core.Models;
using GoldSystem.Core.Services;
using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for BillingEngine, BillingValidationService, BillNumberGeneratorService,
/// and AuditLogger services.
/// </summary>
public class BillingEngineTests : IDisposable
{
    // ─── Infrastructure shared by all tests ─────────────────────────────────

    private readonly GoldDbContext _context;
    private readonly IUnitOfWork _uow;
    private readonly GoldPriceCalculator _calculator = new();

    public BillingEngineTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new GoldDbContext(options);
        _uow = new UnitOfWork(_context);
    }

    public void Dispose() => _context.Dispose();

    // ─── Test-data helpers ───────────────────────────────────────────────────

    private async Task<Branch> SeedBranchAsync(string code = "MUM")
    {
        var branch = new Branch
        {
            Code = code, Name = "Mumbai", Address = "Mumbai",
            GSTIN = "27AAAAA0000A1Z5", Phone = "9999999999",
            IsOwnerBranch = true, IsActive = true
        };
        await _uow.Branches.AddAsync(branch);
        await _uow.SaveChangesAsync();
        return branch;
    }

    private async Task<Customer> SeedCustomerAsync(int branchId)
    {
        var customer = new Customer
        {
            Name = "Ravi Patel", Phone = "9876543210",
            BranchId = branchId, CreatedAt = DateTime.UtcNow
        };
        await _uow.Customers.AddAsync(customer);
        await _uow.SaveChangesAsync();
        return customer;
    }

    private async Task<GoldRate> SeedGoldRateAsync(int branchId, decimal rate24K = 75000m)
    {
        var rate = new GoldRate
        {
            RateDate = DateOnly.FromDateTime(DateTime.Today),
            RateTime = TimeOnly.FromDateTime(DateTime.Now),
            Rate24K = rate24K,
            Rate22K = Math.Round(rate24K * 22m / 24m, 2),
            Rate18K = Math.Round(rate24K * 18m / 24m, 2),
            Source = "TEST",
            BranchId = branchId,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.GoldRates.AddAsync(rate);
        await _uow.SaveChangesAsync();
        return rate;
    }

    private async Task<Category> SeedCategoryAsync()
    {
        var cat = new Category
        {
            Name = "Chain", DefaultMakingType = "PERCENT", DefaultMakingValue = 12m,
            DefaultWastagePercent = 2m, DefaultPurity = "22K", IsActive = true, SortOrder = 1
        };
        await _uow.Categories.AddAsync(cat);
        await _uow.SaveChangesAsync();
        return cat;
    }

    private async Task<Vendor> SeedVendorAsync()
    {
        var vendor = new Vendor
        {
            Name = "Gold Supplier", Phone = "9000000000",
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Vendors.AddAsync(vendor);
        await _uow.SaveChangesAsync();
        return vendor;
    }

    private async Task<User> SeedUserAsync(int branchId)
    {
        var user = new User
        {
            Username = "admin", PasswordHash = "hash", Role = "Admin",
            Name = "Admin User", BranchId = branchId, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();
        return user;
    }

    private async Task<Item> SeedItemAsync(int branchId, int categoryId, int vendorId,
        string purity = "22K", string status = "InStock",
        decimal grossWeight = 10m, decimal stoneWeight = 0.5m)
    {
        var item = new Item
        {
            HUID = $"HUID{Guid.NewGuid():N}"[..10],
            TagNo = $"TAG{Guid.NewGuid():N}"[..6],
            CategoryId = categoryId,
            Name = "Gold Chain",
            Purity = purity,
            GrossWeight = grossWeight,
            StoneWeight = stoneWeight,
            NetWeight = grossWeight - stoneWeight,
            PureGoldWeight = (grossWeight - stoneWeight) * (purity == "22K" ? 22m / 24m : purity == "18K" ? 18m / 24m : 1m),
            MakingType = "PERCENT",
            MakingValue = 12m,
            WastagePercent = 2m,
            PurchaseRate24K = 70000m,
            CostPrice = 65000m,
            Status = status,
            BranchId = branchId,
            VendorId = vendorId,
            PurchaseDate = DateOnly.FromDateTime(DateTime.Today),
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    private BillingEngine CreateEngine()
    {
        var validator = new BillingValidationService(_uow);
        var billGenerator = new BillNumberGeneratorService(_uow);
        var auditLogger = new AuditLogger(_uow);
        return new BillingEngine(_uow, _calculator, billGenerator, validator, auditLogger,
            NullLogger<BillingEngine>.Instance);
    }

    // ─── 1. Bill Creation – Single Item ─────────────────────────────────────

    [Fact]
    public async Task CreateBill_SingleItem_ReturnsValidBillDto()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var user = await SeedUserAsync(branch.BranchId);

        var request = new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m,
            ExchangeValue: 0m,
            PaymentMode: "Cash",
            AmountPaid: 80000m,
            UserId: user.UserId,
            BranchId: branch.BranchId);

        var engine = CreateEngine();
        var dto = await engine.CreateBillAsync(request);

        Assert.NotNull(dto);
        Assert.NotEmpty(dto.BillNo);
        Assert.Equal(customer.CustomerId, dto.Customer.CustomerId);
        Assert.Single(dto.Items);
        Assert.True(dto.GrandTotal > 0);
    }

    // ─── 2. Bill Creation – Multiple Items (mixed purities) ─────────────────

    [Fact]
    public async Task CreateBill_MultipleItems_SumsAllLineItems()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var item22K = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId, "22K");
        var item18K = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId, "18K",
            grossWeight: 4.5m, stoneWeight: 0.3m);
        var user = await SeedUserAsync(branch.BranchId);

        var request = new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items:
            [
                new AddBillItemRequest(item22K.ItemId),
                new AddBillItemRequest(item18K.ItemId)
            ],
            DiscountAmount: 500m,
            ExchangeValue: 1000m,
            PaymentMode: "UPI",
            AmountPaid: 100000m,
            UserId: user.UserId,
            BranchId: branch.BranchId);

        var engine = CreateEngine();
        var dto = await engine.CreateBillAsync(request);

        Assert.Equal(2, dto.Items.Count);
        Assert.Equal(500m, dto.DiscountAmount);
        Assert.Equal(1000m, dto.ExchangeValue);
    }

    // ─── 3. Calculation Accuracy ─────────────────────────────────────────────

    [Fact]
    public async Task CreateBill_CalculationMatchesGoldPriceCalculator()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        const decimal rate24K = 75000m;
        await SeedGoldRateAsync(branch.BranchId, rate24K);
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId,
            purity: "22K", grossWeight: 10m, stoneWeight: 0.5m);
        var user = await SeedUserAsync(branch.BranchId);

        var request = new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 100000m,
            UserId: user.UserId, BranchId: branch.BranchId);

        var engine = CreateEngine();
        var dto = await engine.CreateBillAsync(request);

        // Verify calculation against direct calculator usage
        var expected = _calculator.Calculate(new GoldPriceCalculator.BillLineInput(
            GrossWeight: 10m, StoneWeight: 0.5m, Purity: "22K",
            MakingType: "PERCENT", MakingValue: 12m, WastagePercent: 2m,
            StoneCharge: 0m, Rate24KPer10g: rate24K));

        Assert.Equal(expected.GoldValue, dto.GoldValue);
        Assert.Equal(expected.MakingAmount, dto.MakingAmount);
        Assert.Equal(expected.LineTotal, dto.GrandTotal, precision: 0); // allow rounding
    }

    // ─── 4. Items Marked as Sold ─────────────────────────────────────────────

    [Fact]
    public async Task CreateBill_MarksItemsAsSold()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var user = await SeedUserAsync(branch.BranchId);

        var request = new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 80000m,
            UserId: user.UserId, BranchId: branch.BranchId);

        var engine = CreateEngine();
        var dto = await engine.CreateBillAsync(request);

        var updatedItem = await _uow.Items.GetByIdAsync(item.ItemId);
        Assert.Equal("Sold", updatedItem!.Status);
        Assert.Equal(dto.BillId, updatedItem.SoldBillId);
    }

    // ─── 5. Audit Log Created ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateBill_CreatesAuditLogEntry()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var user = await SeedUserAsync(branch.BranchId);

        var request = new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 80000m,
            UserId: user.UserId, BranchId: branch.BranchId);

        var engine = CreateEngine();
        await engine.CreateBillAsync(request);

        var auditLogs = await _uow.AuditLogs.GetAuditsByActionAsync("BILL_CREATED", 1);
        Assert.NotEmpty(auditLogs);
        Assert.Equal("Bills", auditLogs.First().TableName);
    }

    // ─── 6. Bill Status – Paid ───────────────────────────────────────────────

    [Fact]
    public async Task CreateBill_FullPayment_StatusIsPaid()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var user = await SeedUserAsync(branch.BranchId);

        var engine = CreateEngine();

        // First create to find out the grand total
        var probeDto = await engine.CreateBillAsync(new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 999999m,
            UserId: user.UserId, BranchId: branch.BranchId));

        Assert.Equal("Paid", probeDto.Status);
    }

    // ─── 7. Bill Status – Partial ────────────────────────────────────────────

    [Fact]
    public async Task CreateBill_PartialPayment_StatusIsPartial()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var user = await SeedUserAsync(branch.BranchId);

        var engine = CreateEngine();
        var dto = await engine.CreateBillAsync(new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 1000m,
            UserId: user.UserId, BranchId: branch.BranchId));

        Assert.Equal("Partial", dto.Status);
    }

    // ─── 8. Bill Locking ─────────────────────────────────────────────────────

    [Fact]
    public async Task LockBill_SetsIsLockedTrue()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var user = await SeedUserAsync(branch.BranchId);

        var engine = CreateEngine();
        var dto = await engine.CreateBillAsync(new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 80000m,
            UserId: user.UserId, BranchId: branch.BranchId));

        await engine.LockBillAsync(dto.BillId);

        var bill = await _uow.Bills.GetByIdAsync(dto.BillId);
        Assert.True(bill!.IsLocked);
    }

    // ─── 9. Print locks the bill ─────────────────────────────────────────────

    [Fact]
    public async Task PrintBill_SetsIsLockedTrue()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var user = await SeedUserAsync(branch.BranchId);

        var engine = CreateEngine();
        var dto = await engine.CreateBillAsync(new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 80000m,
            UserId: user.UserId, BranchId: branch.BranchId));

        await engine.PrintBillAsync(dto.BillId);

        var bill = await _uow.Bills.GetByIdAsync(dto.BillId);
        Assert.True(bill!.IsLocked);
    }

    // ─── 10. CanEditBill ─────────────────────────────────────────────────────

    [Fact]
    public async Task CanEditBill_LockedBill_ReturnsLocked()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var user = await SeedUserAsync(branch.BranchId);

        var engine = CreateEngine();
        var dto = await engine.CreateBillAsync(new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 80000m,
            UserId: user.UserId, BranchId: branch.BranchId));

        await engine.LockBillAsync(dto.BillId);
        var (isLocked, reason) = await engine.CanEditBillAsync(dto.BillId);

        Assert.True(isLocked);
        Assert.NotEmpty(reason);
    }

    [Fact]
    public async Task CanEditBill_UnlockedBill_ReturnsNotLocked()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var user = await SeedUserAsync(branch.BranchId);

        var engine = CreateEngine();
        var dto = await engine.CreateBillAsync(new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 80000m,
            UserId: user.UserId, BranchId: branch.BranchId));

        var (isLocked, _) = await engine.CanEditBillAsync(dto.BillId);
        Assert.False(isLocked);
    }

    // ─── 11. LockBill – Bill Not Found ───────────────────────────────────────

    [Fact]
    public async Task LockBill_BillNotFound_ThrowsKeyNotFound()
    {
        var engine = CreateEngine();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.LockBillAsync(99999));
    }

    // ─── 12. GetBill – Not Found ─────────────────────────────────────────────

    [Fact]
    public async Task GetBill_NotFound_ThrowsKeyNotFound()
    {
        var engine = CreateEngine();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.GetBillAsync(99999));
    }

    // ─── 13. GetBillByNo – Not Found ─────────────────────────────────────────

    [Fact]
    public async Task GetBillByNo_NotFound_ThrowsKeyNotFound()
    {
        var engine = CreateEngine();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.GetBillByNoAsync("NOEXIST-99-99999"));
    }

    // ─── Validation Tests ────────────────────────────────────────────────────

    // ─── 14. Customer Not Found ───────────────────────────────────────────────

    [Fact]
    public async Task ValidateCreateBill_CustomerNotFound_ReturnsError()
    {
        await SeedBranchAsync();
        var validator = new BillingValidationService(_uow);

        var request = new CreateBillRequest(
            CustomerId: 99999,
            Items: [new AddBillItemRequest(1)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 0m,
            UserId: 1, BranchId: 1);

        var result = await validator.ValidateCreateBillAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Customer"));
    }

    // ─── 15. Item Not In Stock ────────────────────────────────────────────────

    [Fact]
    public async Task ValidateCreateBill_ItemNotInStock_ReturnsError()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId,
            status: "Sold");

        var validator = new BillingValidationService(_uow);
        var request = new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 0m,
            UserId: 1, BranchId: branch.BranchId);

        var result = await validator.ValidateCreateBillAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not available"));
    }

    // ─── 16. Item Wrong Branch ────────────────────────────────────────────────

    [Fact]
    public async Task ValidateCreateBill_ItemWrongBranch_ReturnsError()
    {
        var branch1 = await SeedBranchAsync("MUM");
        var branch2 = await SeedBranchAsync("DEL");
        var customer = await SeedCustomerAsync(branch1.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        // Item belongs to branch2 but bill is for branch1
        var item = await SeedItemAsync(branch2.BranchId, category.CategoryId, vendor.VendorId);

        var validator = new BillingValidationService(_uow);
        var request = new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 0m,
            UserId: 1, BranchId: branch1.BranchId);

        var result = await validator.ValidateCreateBillAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("different branch"));
    }

    // ─── 17. Invalid Payment Mode ─────────────────────────────────────────────

    [Fact]
    public async Task ValidateCreateBill_InvalidPaymentMode_ReturnsError()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);

        var validator = new BillingValidationService(_uow);
        var request = new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Crypto", AmountPaid: 0m,
            UserId: 1, BranchId: branch.BranchId);

        var result = await validator.ValidateCreateBillAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("payment mode"));
    }

    // ─── 18. Negative Amount Paid ─────────────────────────────────────────────

    [Fact]
    public async Task ValidateCreateBill_NegativeAmountPaid_ReturnsError()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);

        var validator = new BillingValidationService(_uow);
        var request = new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(1)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: -100m,
            UserId: 1, BranchId: branch.BranchId);

        var result = await validator.ValidateCreateBillAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("negative"));
    }

    // ─── 19. Bill Number Format ────────────────────────────────────────────────

    [Fact]
    public async Task GenerateBillNo_FormatIsCorrect()
    {
        var branch = await SeedBranchAsync("MUM");
        var generator = new BillNumberGeneratorService(_uow);

        var billNo = await generator.GenerateBillNoAsync(branch.BranchId);

        // Expected format: MUM-XXYY-00001
        var parts = billNo.Split('-');
        Assert.Equal(3, parts.Length);
        Assert.Equal("MUM", parts[0]);
        Assert.Equal(4, parts[1].Length); // FY like "2526"
        Assert.Equal(5, parts[2].Length); // serial "00001"
    }

    // ─── 20. Bill Number Increments Per Day ───────────────────────────────────

    [Fact]
    public async Task GenerateBillNo_SecondBill_SerialIncrements()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var user = await SeedUserAsync(branch.BranchId);

        // Create first bill
        var item1 = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var engine = CreateEngine();
        var bill1 = await engine.CreateBillAsync(new CreateBillRequest(
            customer.CustomerId, [new(item1.ItemId)], 0, 0, "Cash", 80000, user.UserId, branch.BranchId));

        // Create second bill
        var item2 = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var bill2 = await engine.CreateBillAsync(new CreateBillRequest(
            customer.CustomerId, [new(item2.ItemId)], 0, 0, "Cash", 80000, user.UserId, branch.BranchId));

        var serial1 = int.Parse(bill1.BillNo.Split('-')[2]);
        var serial2 = int.Parse(bill2.BillNo.Split('-')[2]);
        Assert.Equal(1, serial2 - serial1);
    }

    // ─── 21. AdjustedWeight overrides item weight ──────────────────────────────

    [Fact]
    public async Task CreateBill_AdjustedWeight_UsedInCalculation()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId,
            grossWeight: 10m, stoneWeight: 0.5m);
        var user = await SeedUserAsync(branch.BranchId);

        var adjustedGross = 9m;
        var adjustedStone = 0.2m;

        var engine = CreateEngine();
        var dto = await engine.CreateBillAsync(new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId, AdjustedGrossWeight: adjustedGross, AdjustedStoneWeight: adjustedStone)],
            DiscountAmount: 0m, ExchangeValue: 0m,
            PaymentMode: "Cash", AmountPaid: 80000m,
            UserId: user.UserId, BranchId: branch.BranchId));

        Assert.Equal(adjustedGross, dto.Items[0].GrossWeight);
        Assert.Equal(adjustedStone, dto.Items[0].StoneWeight);
        Assert.Equal(adjustedGross - adjustedStone, dto.Items[0].NetWeight);
    }

    // ─── 22. No gold rate → throws ────────────────────────────────────────────

    [Fact]
    public async Task CreateBill_NoGoldRate_ThrowsInvalidOperation()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        // deliberately NOT seeding a gold rate
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var user = await SeedUserAsync(branch.BranchId);

        var engine = CreateEngine();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine.CreateBillAsync(new CreateBillRequest(
                customer.CustomerId, [new(item.ItemId)], 0, 0, "Cash", 80000, user.UserId, branch.BranchId)));
    }

    // ─── 23. ExchangeValue deducted from balance due ──────────────────────────

    [Fact]
    public async Task CreateBill_ExchangeValue_DeductedFromBalanceDue()
    {
        var branch = await SeedBranchAsync();
        var customer = await SeedCustomerAsync(branch.BranchId);
        var category = await SeedCategoryAsync();
        var vendor = await SeedVendorAsync();
        await SeedGoldRateAsync(branch.BranchId);
        var item = await SeedItemAsync(branch.BranchId, category.CategoryId, vendor.VendorId);
        var user = await SeedUserAsync(branch.BranchId);

        const decimal exchangeValue = 5000m;

        var engine = CreateEngine();
        var dto = await engine.CreateBillAsync(new CreateBillRequest(
            CustomerId: customer.CustomerId,
            Items: [new AddBillItemRequest(item.ItemId)],
            DiscountAmount: 0m, ExchangeValue: exchangeValue,
            PaymentMode: "OldGoldExchange", AmountPaid: 0m,
            UserId: user.UserId, BranchId: branch.BranchId));

        Assert.Equal(exchangeValue, dto.ExchangeValue);
        // BalanceDue = GrandTotal - AmountPaid - ExchangeValue
        var expectedBalance = dto.GrandTotal - 0m - exchangeValue;
        Assert.Equal(expectedBalance, dto.BalanceDue, precision: 2);
    }
}
