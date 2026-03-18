using System.IO;
using GoldSystem.Core.Models;
using GoldSystem.Core.Services;
using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Repositories;
using GoldSystem.Data.Services;
using GoldSystem.Reports.Services;
using GoldSystem.WPF.Services;
using GoldSystem.WPF.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GoldSystem.WPF.Tests;

/// <summary>
/// Phase 10 – 25+ unit tests covering BillingViewModel, BillLineItemViewModel,
/// BillingScreenService, BillPdfService, and the GoldPriceCalculator integration.
/// </summary>

// ═══════════════════════════════════════════════════════════════════════════════
// BillLineItemViewModel Tests
// ═══════════════════════════════════════════════════════════════════════════════
public class BillLineItemViewModelTests
{
    [Fact]
    public void MakingDescription_Percent_FormatsCorrectly()
    {
        var vm = new BillLineItemViewModel { MakingType = "PERCENT", MakingValue = 12m };
        Assert.Contains("12", vm.MakingDescription);
        Assert.Contains("Gold", vm.MakingDescription);
    }

    [Fact]
    public void MakingDescription_PerGram_FormatsCorrectly()
    {
        var vm = new BillLineItemViewModel { MakingType = "PER_GRAM", MakingValue = 350m };
        Assert.Contains("350", vm.MakingDescription);
        Assert.Contains("/g", vm.MakingDescription);
    }

    [Fact]
    public void MakingDescription_Fixed_FormatsCorrectly()
    {
        var vm = new BillLineItemViewModel { MakingType = "FIXED", MakingValue = 500m };
        Assert.Contains("500", vm.MakingDescription);
        Assert.Contains("Fixed", vm.MakingDescription);
    }

    [Fact]
    public void PurityTagDisplay_CombinesPurityAndTag()
    {
        var vm = new BillLineItemViewModel { Purity = "22K", TagNo = "TAG-001" };
        Assert.Equal("22K | TAG-001", vm.PurityTagDisplay);
    }

    [Fact]
    public void PropertyChanged_Fires_WhenLineTotalSet()
    {
        var vm = new BillLineItemViewModel();
        bool notified = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BillLineItemViewModel.LineTotal)) notified = true;
        };
        vm.LineTotal = 99999m;
        Assert.True(notified);
    }

    [Fact]
    public void AllMonetaryProperties_DefaultToZero()
    {
        var vm = new BillLineItemViewModel();
        Assert.Equal(0m, vm.GoldValue);
        Assert.Equal(0m, vm.MakingAmount);
        Assert.Equal(0m, vm.LineTotal);
        Assert.Equal(0m, vm.Cgst);
        Assert.Equal(0m, vm.Sgst);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// BillingViewModel Core Tests
// ═══════════════════════════════════════════════════════════════════════════════
public class BillingViewModelTests
{
    private static BillingViewModel CreateVm(
        IBillingEngine? engine = null,
        IBillPdfService? pdf = null,
        BillingScreenService? screenService = null)
    {
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Customers.GetAllAsync(default)).ReturnsAsync([]);
        mockUow.Setup(u => u.GoldRates.GetLatestRateAsync(It.IsAny<int>(), default))
               .ReturnsAsync((GoldRate?)null);

        var ss = screenService ?? new BillingScreenService(mockUow.Object);
        var nav = new Mock<INavigationService>().Object;
        var appState = new AppState { CurrentBranchId = 1 };
        appState.UpdateRates(75000m, 68750m, 56250m, "Test");

        var billingEngineMock = engine ?? new Mock<IBillingEngine>().Object;
        var pdfMock = pdf ?? new Mock<IBillPdfService>().Object;
        var calc = new GoldPriceCalculator();
        var logger = NullLogger<BillingViewModel>.Instance;

        return new BillingViewModel(nav, appState, billingEngineMock, calc, ss, pdfMock, logger);
    }

    [Fact]
    public void InitialState_IsCorrect()
    {
        var vm = CreateVm();
        Assert.Equal(string.Empty, vm.CustomerSearchText);
        Assert.Null(vm.SelectedCustomer);
        Assert.Empty(vm.LineItems);
        Assert.Equal(0m, vm.GrandTotal);
        Assert.Equal("Pending", vm.PaymentStatus);
        Assert.False(vm.IsBillSaved);
        Assert.Equal("Cash", vm.SelectedPaymentMode);
    }

    [Fact]
    public void PaymentModes_ContainsSixModes()
    {
        Assert.Equal(6, BillingViewModel.PaymentModes.Count);
        Assert.Contains("Cash", BillingViewModel.PaymentModes);
        Assert.Contains("Card", BillingViewModel.PaymentModes);
        Assert.Contains("UPI", BillingViewModel.PaymentModes);
        Assert.Contains("NEFT", BillingViewModel.PaymentModes);
        Assert.Contains("Split", BillingViewModel.PaymentModes);
        Assert.Contains("OldGoldExchange", BillingViewModel.PaymentModes);
    }

    [Fact]
    public void CurrentRateDisplay_ShowsRate_WhenRateAvailable()
    {
        var vm = CreateVm();
        Assert.Contains("75,000", vm.CurrentRateDisplay);
        Assert.Contains("24K", vm.CurrentRateDisplay);
    }

    [Fact]
    public void RecalculateTotals_WithNoItems_GrandTotalIsZero()
    {
        var vm = CreateVm();
        vm.RecalculateTotals();
        Assert.Equal(0m, vm.GrandTotal);
        Assert.Equal("Pending", vm.PaymentStatus);
    }

    [Fact]
    public void RecalculateTotals_WithDiscount_ReducesTaxableAmount()
    {
        var vm = CreateVm();
        var item = new BillLineItemViewModel
        {
            ItemId = 1, TagNo = "T1", Purity = "22K",
            NetWeight = 10m, WastageWeight = 0.2m,
            GoldValue = 68750m, MakingAmount = 8250m,
            TaxableAmount = 77000m,
            Cgst = 1500m, Sgst = 1500m,
            LineTotal = 80000m, RateUsed24K = 75000m,
            WastagePercent = 2m
        };
        vm.LineItems.Add(item);
        vm.DiscountAmount = 5000m;
        vm.RecalculateTotals();

        Assert.True(vm.TaxableAmount < vm.SubTotal);
    }

    [Fact]
    public void RecalculateTotals_PaidStatus_WhenAmountPaidCoversTotal()
    {
        var vm = CreateVm();
        var item = new BillLineItemViewModel
        {
            ItemId = 1, TagNo = "T1", Purity = "22K",
            NetWeight = 5m, WastageWeight = 0.1m,
            GoldValue = 34375m, MakingAmount = 4125m,
            TaxableAmount = 38500m,
            Cgst = 750m, Sgst = 750m,
            LineTotal = 40000m, RateUsed24K = 75000m,
            WastagePercent = 2m
        };
        vm.LineItems.Add(item);
        vm.RecalculateTotals();
        vm.AmountPaid = vm.GrandTotal;
        vm.RecalculateTotals();

        Assert.Equal("Paid", vm.PaymentStatus);
        Assert.True(vm.BalanceDue <= 0m);
    }

    [Fact]
    public void RemoveLineItem_RemovesFromCollection()
    {
        var vm = CreateVm();
        var item = new BillLineItemViewModel { ItemId = 1, TagNo = "T1", ItemName = "Ring" };
        vm.LineItems.Add(item);
        Assert.Single(vm.LineItems);

        vm.RemoveLineItemCommand.Execute(item);
        Assert.Empty(vm.LineItems);
    }

    [Fact]
    public void ClearBill_ResetsAllFields()
    {
        var vm = CreateVm();
        vm.LineItems.Add(new BillLineItemViewModel { ItemId = 1, TagNo = "T1" });
        vm.DiscountAmount = 500m;
        vm.AmountPaid = 10000m;

        vm.ClearBillCommand.Execute(null);

        Assert.Empty(vm.LineItems);
        Assert.Equal(0m, vm.DiscountAmount);
        Assert.Equal(0m, vm.AmountPaid);
        Assert.Equal(0m, vm.GrandTotal);
        Assert.Null(vm.SelectedCustomer);
    }

    [Fact]
    public void SaveBillCommand_CanExecute_ReturnsFalse_WhenNoCustomer()
    {
        var vm = CreateVm();
        vm.LineItems.Add(new BillLineItemViewModel { ItemId = 1, TagNo = "T1" });
        // No customer selected
        Assert.False(vm.SaveBillCommand.CanExecute(null));
    }

    [Fact]
    public void SaveBillCommand_CanExecute_ReturnsFalse_WhenNoItems()
    {
        var vm = CreateVm();
        vm.SelectCustomerCommand.Execute(new CustomerDto(1, "Test Customer", "9999999999"));
        // No items
        Assert.False(vm.SaveBillCommand.CanExecute(null));
    }

    [Fact]
    public void SaveBillCommand_CanExecute_ReturnsTrue_WhenCustomerAndItemsPresent()
    {
        var vm = CreateVm();
        vm.SelectCustomerCommand.Execute(new CustomerDto(1, "Test Customer", "9999999999"));
        vm.LineItems.Add(new BillLineItemViewModel { ItemId = 1, TagNo = "T1" });
        Assert.True(vm.SaveBillCommand.CanExecute(null));
    }

    [Fact]
    public void PrintBillCommand_CanExecute_ReturnsFalse_WhenBillNotSaved()
    {
        var vm = CreateVm();
        Assert.False(vm.PrintBillCommand.CanExecute(null));
    }

    [Fact]
    public void SelectCustomer_SetsSelectedCustomer()
    {
        var vm = CreateVm();
        var customer = new CustomerDto(5, "Ravi Shah", "9876543210");
        vm.SelectCustomerCommand.Execute(customer);
        Assert.Equal(5, vm.SelectedCustomer!.CustomerId);
        Assert.Equal("Ravi Shah", vm.SelectedCustomer.Name);
    }

    [Fact]
    public void SelectCustomer_ClosesSearchDropdown()
    {
        var vm = CreateVm();
        vm.IsCustomerSearchOpen = true;
        vm.SelectCustomerCommand.Execute(new CustomerDto(1, "Test", "111"));
        Assert.False(vm.IsCustomerSearchOpen);
    }

    [Fact]
    public async Task SaveBillAsync_SetsHasError_WhenNoCustomerSelected()
    {
        var vm = CreateVm();
        vm.LineItems.Add(new BillLineItemViewModel { ItemId = 1, TagNo = "T1" });
        // Force execution even though CanExecute is false
        await vm.SaveBillCommand.ExecuteAsync(null);
        Assert.True(vm.HasError);
        Assert.Contains("customer", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveBillAsync_CallsBillingEngine_WhenValid()
    {
        var engineMock = new Mock<IBillingEngine>();
        var returnedBill = new BillDto(
            BillId: 1, BillNo: "BILL-001",
            BillDate: DateOnly.FromDateTime(DateTime.Today),
            Customer: new CustomerDto(1, "Test", "111"),
            Items: [],
            GoldValue: 1000m, MakingAmount: 100m, WastageAmount: 20m,
            StoneCharge: 0, DiscountAmount: 0,
            CGST: 15m, SGST: 15m, IGST: 0,
            RoundOff: 0, GrandTotal: 1150m,
            ExchangeValue: 0, AmountPaid: 1150m,
            BalanceDue: 0, Status: "Paid",
            PaymentMode: "Cash", IsLocked: false,
            CreatedAt: DateTime.UtcNow);

        engineMock.Setup(e => e.CreateBillAsync(It.IsAny<CreateBillRequest>()))
                  .ReturnsAsync(returnedBill);

        var vm = CreateVm(engine: engineMock.Object);
        vm.SelectCustomerCommand.Execute(new CustomerDto(1, "Test", "111"));
        vm.LineItems.Add(new BillLineItemViewModel { ItemId = 1, TagNo = "T1" });

        await vm.SaveBillCommand.ExecuteAsync(null);

        engineMock.Verify(e => e.CreateBillAsync(It.IsAny<CreateBillRequest>()), Times.Once);
        Assert.True(vm.IsBillSaved);
        Assert.NotNull(vm.SavedBill);
        Assert.Equal("BILL-001", vm.SavedBill!.BillNo);
    }

    [Fact]
    public async Task SaveBillAsync_SetsHasError_WhenEngineThrows()
    {
        var engineMock = new Mock<IBillingEngine>();
        engineMock.Setup(e => e.CreateBillAsync(It.IsAny<CreateBillRequest>()))
                  .ThrowsAsync(new InvalidOperationException("Gold rate not available"));

        var vm = CreateVm(engine: engineMock.Object);
        vm.SelectCustomerCommand.Execute(new CustomerDto(1, "Test", "111"));
        vm.LineItems.Add(new BillLineItemViewModel { ItemId = 1, TagNo = "T1" });

        await vm.SaveBillCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
        Assert.Contains("Gold rate not available", vm.StatusMessage);
    }

    [Fact]
    public void NewBill_AfterSave_ResetsIsBillSaved()
    {
        var vm = CreateVm();
        vm.IsBillSaved = true;
        vm.NewBillCommand.Execute(null);
        Assert.False(vm.IsBillSaved);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// BillingScreenService Tests
// ═══════════════════════════════════════════════════════════════════════════════
public class BillingScreenServiceTests
{
    private static (BillingScreenService Service, Mock<IUnitOfWork> Uow) Create()
    {
        var uow = new Mock<IUnitOfWork>();
        return (new BillingScreenService(uow.Object), uow);
    }

    [Fact]
    public async Task LookupItemAsync_ReturnsNull_WhenTagIsEmpty()
    {
        var (svc, _) = Create();
        var result = await svc.LookupItemAsync(string.Empty, branchId: 1);
        Assert.Null(result);
    }

    [Fact]
    public async Task LookupItemAsync_ReturnsNull_WhenItemNotFound()
    {
        var (svc, uow) = Create();
        uow.Setup(u => u.Items.GetByTagAsync(It.IsAny<string>(), default)).ReturnsAsync((Item?)null);
        uow.Setup(u => u.Items.GetByHUIDAsync(It.IsAny<string>(), default)).ReturnsAsync((Item?)null);

        var result = await svc.LookupItemAsync("NONEXISTENT", branchId: 1);
        Assert.Null(result);
    }

    [Fact]
    public async Task LookupItemAsync_ReturnsNull_WhenItemNotInStock()
    {
        var (svc, uow) = Create();
        var item = new Item { ItemId = 1, TagNo = "T1", Status = "Sold", BranchId = 1 };
        uow.Setup(u => u.Items.GetByTagAsync("T1", default)).ReturnsAsync(item);

        var result = await svc.LookupItemAsync("T1", branchId: 1);
        Assert.Null(result);
    }

    [Fact]
    public async Task LookupItemAsync_ReturnsNull_WhenItemFromDifferentBranch()
    {
        var (svc, uow) = Create();
        var item = new Item { ItemId = 1, TagNo = "T1", Status = "InStock", BranchId = 2 };
        uow.Setup(u => u.Items.GetByTagAsync("T1", default)).ReturnsAsync(item);

        var result = await svc.LookupItemAsync("T1", branchId: 1);
        Assert.Null(result);
    }

    [Fact]
    public async Task LookupItemAsync_ReturnsItem_WhenValidAndInStock()
    {
        var (svc, uow) = Create();
        var item = new Item
        {
            ItemId = 42, TagNo = "TAG-042", Name = "Gold Ring",
            Purity = "22K", GrossWeight = 8.5m, StoneWeight = 0.3m,
            Status = "InStock", BranchId = 1, CreatedAt = DateTime.UtcNow
        };
        uow.Setup(u => u.Items.GetByTagAsync("TAG-042", default)).ReturnsAsync(item);

        var result = await svc.LookupItemAsync("TAG-042", branchId: 1);

        Assert.NotNull(result);
        Assert.Equal(42, result!.ItemId);
        Assert.Equal("Gold Ring", result.Name);
        Assert.Equal("22K", result.Purity);
    }

    [Fact]
    public async Task LookupItemAsync_TriesHUID_WhenTagNotFound()
    {
        var (svc, uow) = Create();
        var item = new Item
        {
            ItemId = 10, TagNo = "TAG-010", HUID = "HUID-ABC",
            Name = "Bangle", Purity = "18K",
            Status = "InStock", BranchId = 1, CreatedAt = DateTime.UtcNow
        };
        uow.Setup(u => u.Items.GetByTagAsync("HUID-ABC", default)).ReturnsAsync((Item?)null);
        uow.Setup(u => u.Items.GetByHUIDAsync("HUID-ABC", default)).ReturnsAsync(item);

        var result = await svc.LookupItemAsync("HUID-ABC", branchId: 1);
        Assert.NotNull(result);
        Assert.Equal(10, result!.ItemId);
    }

    [Fact]
    public async Task SearchCustomersAsync_ReturnsEmpty_WhenQueryTooShort()
    {
        var (svc, _) = Create();
        var result = await svc.SearchCustomersAsync("A", branchId: 1);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCustomersAsync_FiltersBy_NameAndBranch()
    {
        var (svc, uow) = Create();
        var customers = new List<Customer>
        {
            new() { CustomerId = 1, Name = "Ravi Kumar", Phone = "9000000001", BranchId = 1 },
            new() { CustomerId = 2, Name = "Priya Shah",  Phone = "9000000002", BranchId = 1 },
            new() { CustomerId = 3, Name = "Ravi Patel",  Phone = "9000000003", BranchId = 2 }
        };
        uow.Setup(u => u.Customers.GetAllAsync(default)).ReturnsAsync(customers);

        var result = (await svc.SearchCustomersAsync("Ravi", branchId: 1)).ToList();

        // Only branch-1 matches
        Assert.Single(result);
        Assert.Equal("Ravi Kumar", result[0].Name);
    }

    [Fact]
    public async Task GetCurrentRate24KAsync_ReturnsNull_WhenNoRate()
    {
        var (svc, uow) = Create();
        uow.Setup(u => u.GoldRates.GetLatestRateAsync(1, default)).ReturnsAsync((GoldRate?)null);

        var result = await svc.GetCurrentRate24KAsync(branchId: 1);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentRate24KAsync_ReturnsRate_WhenRateExists()
    {
        var (svc, uow) = Create();
        uow.Setup(u => u.GoldRates.GetLatestRateAsync(1, default))
           .ReturnsAsync(new GoldRate { Rate24K = 75000m, Rate22K = 68750m });

        var result = await svc.GetCurrentRate24KAsync(branchId: 1);
        Assert.Equal(75000m, result);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// BillPdfService Tests
// ═══════════════════════════════════════════════════════════════════════════════
public class BillPdfServiceTests
{
    private static BillDto SampleBill() => new(
        BillId: 1, BillNo: "BILL-2024-0001",
        BillDate: new DateOnly(2024, 11, 15),
        Customer: new CustomerDto(1, "Anita Sharma", "9876543210"),
        Items:
        [
            new BillItemDto(
                BillItemId: 1,
                Item: new ItemDto(1, "HUID-001", "TAG-001", "Gold Ring", "22K", 8.5m, 0.3m, "Sold", DateTime.Now),
                GrossWeight: 8.5m, StoneWeight: 0.3m, NetWeight: 8.2m,
                WastagePercent: 2m, WastageWeight: 0.164m, BillableWeight: 8.364m,
                PureGoldWeight: 7.517m, RateUsed24K: 75000m,
                GoldValue: 56375m, MakingAmount: 6765m,
                StoneCharge: 0m, CGST_Amount: 942m, SGST_Amount: 942m,
                LineTotal: 65024m)
        ],
        GoldValue: 56375m, MakingAmount: 6765m, WastageAmount: 615m,
        StoneCharge: 0m, DiscountAmount: 0m,
        CGST: 942m, SGST: 942m, IGST: 0m,
        RoundOff: 0.05m, GrandTotal: 65024m,
        ExchangeValue: 0m, AmountPaid: 65024m, BalanceDue: 0m,
        Status: "Paid", PaymentMode: "Cash", IsLocked: false,
        CreatedAt: DateTime.UtcNow);

    [Fact]
    public void GenerateBillPdf_ReturnsNonEmptyBytes()
    {
        var svc = new BillPdfService();
        var bytes = svc.GenerateBillPdf(SampleBill(), "Test Jewellers", "Mumbai", "1234567890");

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000, "PDF should be more than 1KB");
    }

    [Fact]
    public void GenerateBillPdf_ReturnsPdfHeader()
    {
        var svc = new BillPdfService();
        var bytes = svc.GenerateBillPdf(SampleBill(), "Test Jewellers", "Mumbai", "1234567890");

        // PDF files start with %PDF
        var header = System.Text.Encoding.ASCII.GetString(bytes, 0, 4);
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public void SaveBillPdf_WritesFile()
    {
        var svc = new BillPdfService();
        var path = Path.Combine(Path.GetTempPath(), $"test_bill_{Guid.NewGuid()}.pdf");
        try
        {
            svc.SaveBillPdf(SampleBill(), path, "Test Jewellers", "Mumbai", "1234567890");
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 1000);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void GenerateBillPdf_WithMultipleItems_Succeeds()
    {
        var bill = SampleBill();
        var items = Enumerable.Range(1, 5).Select(i => new BillItemDto(
            BillItemId: i,
            Item: new ItemDto(i, $"HUID-{i:D3}", $"TAG-{i:D3}", $"Gold Item {i}", "22K",
                              10m, 0m, "Sold", DateTime.Now),
            GrossWeight: 10m, StoneWeight: 0m, NetWeight: 10m,
            WastagePercent: 2m, WastageWeight: 0.2m, BillableWeight: 10.2m,
            PureGoldWeight: 9.167m, RateUsed24K: 75000m,
            GoldValue: 68750m, MakingAmount: 8250m,
            StoneCharge: 0m, CGST_Amount: 1500m, SGST_Amount: 1500m,
            LineTotal: 80000m)).ToList();

        var multiBill = bill with { Items = items, GrandTotal = 400000m };
        var svc = new BillPdfService();
        var bytes = svc.GenerateBillPdf(multiBill, "Gold Palace", "Chennai", "9999999999");
        Assert.True(bytes.Length > 1000);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// GoldPriceCalculator Integration Tests (Phase 10 billing calculations)
// ═══════════════════════════════════════════════════════════════════════════════
public class BillingCalculationIntegrationTests
{
    private readonly GoldPriceCalculator _calc = new();

    [Fact]
    public void Calculate_22K_Ring_GivesCorrectGoldValue()
    {
        var input = new GoldPriceCalculator.BillLineInput(
            GrossWeight: 10m, StoneWeight: 0m,
            Purity: "22K", MakingType: "PERCENT", MakingValue: 12m,
            WastagePercent: 2m, StoneCharge: 0m, Rate24KPer10g: 75000m);

        var result = _calc.Calculate(input);

        // Net = 10g, PurityFactor = 22/24, PureGold = 10 × (22/24) = 9.1667g
        // RatePerGram = 75000/10 = 7500
        // GoldValue = 9.1667 × 7500 ≈ 68750
        Assert.Equal(10m, result.NetWeight);
        Assert.True(Math.Abs(result.GoldValue - 68750m) < 1m);
    }

    [Fact]
    public void Calculate_WastageWeight_IsCorrect()
    {
        var input = new GoldPriceCalculator.BillLineInput(
            GrossWeight: 10m, StoneWeight: 0m,
            Purity: "22K", MakingType: "PERCENT", MakingValue: 12m,
            WastagePercent: 2m, StoneCharge: 0m, Rate24KPer10g: 75000m);

        var result = _calc.Calculate(input);

        // Wastage = NetWeight × WastagePercent / 100 = 10 × 2 / 100 = 0.2g
        Assert.Equal(0.2m, result.WastageWeight);
    }

    [Fact]
    public void Calculate_GST_GoldIs3Percent()
    {
        var input = new GoldPriceCalculator.BillLineInput(
            GrossWeight: 10m, StoneWeight: 0m,
            Purity: "24K", MakingType: "FIXED", MakingValue: 0m,
            WastagePercent: 0m, StoneCharge: 0m, Rate24KPer10g: 75000m);

        var result = _calc.Calculate(input);

        // GoldGST = GoldValue × 3% = 75000 × 3% = 2250
        Assert.Equal(75000m * 0.03m, result.GoldGST);
    }

    [Fact]
    public void CalculateTotal_WithDiscount_ReducesTaxable()
    {
        var lineResult = _calc.Calculate(new GoldPriceCalculator.BillLineInput(
            GrossWeight: 10m, StoneWeight: 0m,
            Purity: "22K", MakingType: "PERCENT", MakingValue: 12m,
            WastagePercent: 2m, StoneCharge: 0m, Rate24KPer10g: 75000m));

        var total = _calc.CalculateTotal(
            new GoldPriceCalculator.BillTotalInput([lineResult], 5000m, 0m, false), 0m);

        Assert.Equal(lineResult.TaxableAmount - 5000m, total.TaxableAmount);
    }

    [Fact]
    public void CalculateTotal_BalanceDue_IsZeroWhenFullyPaid()
    {
        var lineResult = _calc.Calculate(new GoldPriceCalculator.BillLineInput(
            GrossWeight: 5m, StoneWeight: 0m,
            Purity: "22K", MakingType: "PERCENT", MakingValue: 10m,
            WastagePercent: 2m, StoneCharge: 0m, Rate24KPer10g: 75000m));

        var total = _calc.CalculateTotal(
            new GoldPriceCalculator.BillTotalInput([lineResult], 0m, 0m, false),
            lineResult.LineTotal);

        // Round-off may cause minor difference; allow ±1
        Assert.True(Math.Abs(total.BalanceDue) <= 1m,
            $"Expected balance ~0 but got {total.BalanceDue}");
    }
}
