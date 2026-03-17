using GoldSystem.Core.Models;
using GoldSystem.Core.Services;
using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Repositories;
using GoldSystem.Data.Services;
using GoldSystem.WPF.Services;
using GoldSystem.WPF.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GoldSystem.WPF.Tests;

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 11 – StockTransferService Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class StockTransferServiceTests
{
    private static (StockTransferService svc, Mock<IUnitOfWork> uow, Mock<IAuditLogger> audit) Create()
    {
        var uow = new Mock<IUnitOfWork>();
        var audit = new Mock<IAuditLogger>();
        var svc = new StockTransferService(uow.Object, audit.Object);
        return (svc, uow, audit);
    }

    private static Item InStockItem(int branchId = 1) => new()
    {
        ItemId = 1,
        TagNo = "T001",
        HUID = "H001",
        Name = "Gold Chain",
        Purity = "22K",
        GrossWeight = 10m,
        NetWeight = 10m,
        Status = "InStock",
        BranchId = branchId,
        PurchaseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
        CostPrice = 75000m
    };

    [Fact]
    public async Task TransferItemAsync_SameBranch_Throws()
    {
        var (svc, _, _) = Create();
        var req = new StockTransferRequest(1, 1, 1, 99);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.TransferItemAsync(req));
    }

    [Fact]
    public async Task TransferItemAsync_ItemNotFound_Throws()
    {
        var (svc, uow, _) = Create();
        uow.Setup(u => u.Items.GetByIdAsync(1, default)).ReturnsAsync((Item?)null);
        var req = new StockTransferRequest(1, 1, 2, 99);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.TransferItemAsync(req));
    }

    [Fact]
    public async Task TransferItemAsync_ItemNotInStock_Throws()
    {
        var (svc, uow, _) = Create();
        var item = InStockItem();
        item.Status = "Sold";
        uow.Setup(u => u.Items.GetByIdAsync(1, default)).ReturnsAsync(item);
        var req = new StockTransferRequest(1, 1, 2, 99);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.TransferItemAsync(req));
    }

    [Fact]
    public async Task TransferItemAsync_WrongSourceBranch_Throws()
    {
        var (svc, uow, _) = Create();
        var item = InStockItem(branchId: 3); // item is in branch 3, request says from branch 1
        uow.Setup(u => u.Items.GetByIdAsync(1, default)).ReturnsAsync(item);
        var req = new StockTransferRequest(1, 1, 2, 99);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.TransferItemAsync(req));
    }

    [Fact]
    public async Task TransferItemAsync_DestinationBranchNotFound_Throws()
    {
        var (svc, uow, _) = Create();
        var item = InStockItem(branchId: 1);
        uow.Setup(u => u.Items.GetByIdAsync(1, default)).ReturnsAsync(item);
        uow.Setup(u => u.Branches.GetByIdAsync(2, default)).ReturnsAsync((Branch?)null);
        uow.Setup(u => u.Branches.GetByIdAsync(1, default)).ReturnsAsync(new Branch { BranchId = 1, Name = "Main" });
        var req = new StockTransferRequest(1, 1, 2, 99);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.TransferItemAsync(req));
    }

    [Fact]
    public async Task TransferItemAsync_ValidRequest_ReturnsDto()
    {
        var (svc, uow, audit) = Create();
        var item = InStockItem(branchId: 1);
        var fromBranch = new Branch { BranchId = 1, Name = "Main", Code = "MUM" };
        var toBranch = new Branch { BranchId = 2, Name = "Branch2", Code = "DEL" };

        uow.Setup(u => u.Items.GetByIdAsync(1, default)).ReturnsAsync(item);
        uow.Setup(u => u.Branches.GetByIdAsync(1, default)).ReturnsAsync(fromBranch);
        uow.Setup(u => u.Branches.GetByIdAsync(2, default)).ReturnsAsync(toBranch);
        uow.Setup(u => u.Items.UpdateAsync(item, default)).Returns(Task.CompletedTask);
        uow.Setup(u => u.BeginTransactionAsync(default)).Returns(Task.CompletedTask);
        uow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);
        audit.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                                    It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>()))
             .Returns(Task.CompletedTask);

        var req = new StockTransferRequest(1, 1, 2, 99, "Test transfer");
        var result = await svc.TransferItemAsync(req);

        Assert.Equal("T001", result.TagNo);
        Assert.Equal(1, result.FromBranchId);
        Assert.Equal(2, result.ToBranchId);
        Assert.Equal("Test transfer", result.Remarks);
    }

    [Fact]
    public async Task TransferItemAsync_ValidRequest_UpdatesItemBranchId()
    {
        var (svc, uow, audit) = Create();
        var item = InStockItem(branchId: 1);
        uow.Setup(u => u.Items.GetByIdAsync(1, default)).ReturnsAsync(item);
        uow.Setup(u => u.Branches.GetByIdAsync(1, default))
           .ReturnsAsync(new Branch { BranchId = 1, Name = "Main", Code = "MUM" });
        uow.Setup(u => u.Branches.GetByIdAsync(2, default))
           .ReturnsAsync(new Branch { BranchId = 2, Name = "Branch2", Code = "DEL" });
        uow.Setup(u => u.Items.UpdateAsync(item, default)).Returns(Task.CompletedTask);
        uow.Setup(u => u.BeginTransactionAsync(default)).Returns(Task.CompletedTask);
        uow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);
        audit.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                                    It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>()))
             .Returns(Task.CompletedTask);

        await svc.TransferItemAsync(new StockTransferRequest(1, 1, 2, 99));

        Assert.Equal(2, item.BranchId);
    }

    [Fact]
    public void GetTier_Below100K_ReturnsSilver()
    {
        Assert.Equal(LoyaltyTier.Silver, StockTransferService.GetTier(99_999m));
        Assert.Equal(LoyaltyTier.Silver, StockTransferService.GetTier(0m));
    }

    [Fact]
    public void GetTier_100KTo500K_ReturnsGold()
    {
        Assert.Equal(LoyaltyTier.Gold, StockTransferService.GetTier(100_000m));
        Assert.Equal(LoyaltyTier.Gold, StockTransferService.GetTier(499_999m));
    }

    [Fact]
    public void GetTier_500KAndAbove_ReturnsPlatinum()
    {
        Assert.Equal(LoyaltyTier.Platinum, StockTransferService.GetTier(500_000m));
        Assert.Equal(LoyaltyTier.Platinum, StockTransferService.GetTier(1_000_000m));
    }

    [Fact]
    public void CalculatePointsEarned_PerThousandRule()
    {
        Assert.Equal(75, StockTransferService.CalculatePointsEarned(75_000m));
        Assert.Equal(0, StockTransferService.CalculatePointsEarned(999m));
        Assert.Equal(1, StockTransferService.CalculatePointsEarned(1_000m));
    }

    [Fact]
    public async Task RedeemPointsAsync_NegativePoints_ReturnsFailure()
    {
        var (svc, _, _) = Create();
        var req = new LoyaltyRedemptionRequest(1, -10, 99);
        var result = await svc.RedeemPointsAsync(req);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task RedeemPointsAsync_InsufficientPoints_ReturnsFailure()
    {
        var (svc, uow, _) = Create();
        uow.Setup(u => u.Customers.GetByIdAsync(1, default))
           .ReturnsAsync(new Customer { CustomerId = 1, LoyaltyPoints = 50 });
        var req = new LoyaltyRedemptionRequest(1, 200, 99);
        var result = await svc.RedeemPointsAsync(req);
        Assert.False(result.Success);
        Assert.Contains("Insufficient", result.Message);
    }

    [Fact]
    public async Task RedeemPointsAsync_ValidRequest_ReturnsCorrectDiscount()
    {
        var (svc, uow, _) = Create();
        uow.Setup(u => u.Customers.GetByIdAsync(1, default))
           .ReturnsAsync(new Customer { CustomerId = 1, LoyaltyPoints = 500 });
        uow.Setup(u => u.Customers.UpdateLoyaltyPointsAsync(1, 300, default)).Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var req = new LoyaltyRedemptionRequest(1, 200, 99);
        var result = await svc.RedeemPointsAsync(req);
        Assert.True(result.Success);
        Assert.Equal(100m, result.DiscountAmount); // 200/100 * 50 = 100
        Assert.Equal(300, result.RemainingPoints);
    }

    [Fact]
    public async Task GetInventoryAsync_MapsAllFields()
    {
        var (svc, uow, _) = Create();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var items = new List<Item>
        {
            new()
            {
                ItemId = 1, TagNo = "T001", HUID = "H001", Name = "Ring", Purity = "22K",
                GrossWeight = 8m, NetWeight = 7.5m, Status = "InStock", BranchId = 1,
                CostPrice = 60000m, PurchaseDate = today.AddDays(-45),
                Category = new Category { Name = "Rings" }
            }
        };
        uow.Setup(u => u.Items.GetInventoryByBranchAsync(1, default)).ReturnsAsync(items);
        var result = await svc.GetInventoryAsync(1);
        Assert.Single(result);
        Assert.Equal("T001", result[0].TagNo);
        Assert.Equal(45, result[0].DaysInStock);
        Assert.Equal("Rings", result[0].CategoryName);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 11 – InventoryViewModel Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class InventoryViewModelTests
{
    private static (InventoryViewModel vm, Mock<IUnitOfWork> uow) Create()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Items.GetInventoryByBranchAsync(It.IsAny<int>(), default))
           .ReturnsAsync([]);
        uow.Setup(u => u.Branches.GetActiveBranchesAsync(default)).ReturnsAsync([]);
        var audit = new Mock<IAuditLogger>();
        var svc = new StockTransferService(uow.Object, audit.Object);
        var nav = new Mock<NavigationService>(new Mock<IServiceProvider>().Object).Object;
        var state = new AppState { CurrentBranchId = 1 };
        var vm = new InventoryViewModel(nav, state, uow.Object, svc, NullLogger<InventoryViewModel>.Instance);
        return (vm, uow);
    }

    [Fact]
    public void InitialState_IsCorrect()
    {
        var (vm, _) = Create();
        Assert.False(vm.IsLoading);
        Assert.Empty(vm.AllItems);
        Assert.Equal("All", vm.SelectedStatusFilter);
        Assert.Equal(0, vm.InStockCount);
    }

    [Fact]
    public async Task LoadCommand_SetsIsLoadingFalse()
    {
        var (vm, _) = Create();
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadCommand_PopulatesAllItems()
    {
        var (vm, uow) = Create();
        var today = DateOnly.FromDateTime(DateTime.Today);
        uow.Setup(u => u.Items.GetInventoryByBranchAsync(1, default))
           .ReturnsAsync([new Item
            {
                ItemId = 1, TagNo = "T001", Name = "Ring", Purity = "22K",
                GrossWeight = 8m, NetWeight = 8m, Status = "InStock", BranchId = 1,
                PurchaseDate = today.AddDays(-10), CostPrice = 60000m,
                Category = new Category { Name = "Rings" }
            }]);

        await vm.LoadCommand.ExecuteAsync(null);
        Assert.Single(vm.AllItems);
    }

    [Fact]
    public async Task ApplyFilter_BySearchText_FiltersItems()
    {
        var (vm, uow) = Create();
        var today = DateOnly.FromDateTime(DateTime.Today);
        uow.Setup(u => u.Items.GetInventoryByBranchAsync(1, default))
           .ReturnsAsync([
               new Item { ItemId = 1, TagNo = "T001", Name = "Gold Ring", Purity = "22K",
                          GrossWeight = 8m, NetWeight = 8m, Status = "InStock", BranchId = 1,
                          PurchaseDate = today, CostPrice = 60000m, Category = new Category { Name = "Rings" }},
               new Item { ItemId = 2, TagNo = "T002", Name = "Gold Chain", Purity = "22K",
                          GrossWeight = 12m, NetWeight = 12m, Status = "InStock", BranchId = 1,
                          PurchaseDate = today, CostPrice = 90000m, Category = new Category { Name = "Chains" }}
           ]);

        await vm.LoadCommand.ExecuteAsync(null);
        vm.SearchText = "Ring";

        Assert.Single(vm.FilteredItems);
        Assert.Equal("T001", vm.FilteredItems[0].TagNo);
    }

    [Fact]
    public async Task ApplyFilter_ByStatus_FiltersItems()
    {
        var (vm, uow) = Create();
        var today = DateOnly.FromDateTime(DateTime.Today);
        uow.Setup(u => u.Items.GetInventoryByBranchAsync(1, default))
           .ReturnsAsync([
               new Item { ItemId = 1, TagNo = "T001", Name = "Ring", Purity = "22K",
                          GrossWeight = 8m, NetWeight = 8m, Status = "InStock", BranchId = 1,
                          PurchaseDate = today, CostPrice = 60000m, Category = new Category { Name = "Rings" }},
               new Item { ItemId = 2, TagNo = "T002", Name = "Chain", Purity = "22K",
                          GrossWeight = 12m, NetWeight = 12m, Status = "Sold", BranchId = 1,
                          PurchaseDate = today, CostPrice = 90000m, Category = new Category { Name = "Chains" }}
           ]);

        await vm.LoadCommand.ExecuteAsync(null);
        vm.SelectedStatusFilter = "InStock";

        Assert.Single(vm.FilteredItems);
        Assert.Equal("InStock", vm.FilteredItems[0].Status);
    }

    [Fact]
    public async Task Kpis_InStockCount_IsCorrect()
    {
        var (vm, uow) = Create();
        var today = DateOnly.FromDateTime(DateTime.Today);
        uow.Setup(u => u.Items.GetInventoryByBranchAsync(1, default))
           .ReturnsAsync([
               new Item { ItemId = 1, TagNo = "T001", Name = "Ring", Purity = "22K",
                          GrossWeight = 8m, NetWeight = 8m, Status = "InStock", BranchId = 1,
                          PurchaseDate = today, CostPrice = 60000m, Category = new Category { Name = "Rings" }},
               new Item { ItemId = 2, TagNo = "T002", Name = "Chain", Purity = "22K",
                          GrossWeight = 12m, NetWeight = 12m, Status = "Sold", BranchId = 1,
                          PurchaseDate = today, CostPrice = 90000m, Category = new Category { Name = "Chains" }}
           ]);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(1, vm.InStockCount);
        Assert.Equal(60000m, vm.InStockValue);
    }

    [Fact]
    public async Task Kpis_AgingItems_CountsOver90Days()
    {
        var (vm, uow) = Create();
        var today = DateOnly.FromDateTime(DateTime.Today);
        uow.Setup(u => u.Items.GetInventoryByBranchAsync(1, default))
           .ReturnsAsync([
               new Item { ItemId = 1, TagNo = "T001", Name = "Old Ring", Purity = "22K",
                          GrossWeight = 8m, NetWeight = 8m, Status = "InStock", BranchId = 1,
                          PurchaseDate = today.AddDays(-120), CostPrice = 60000m,
                          Category = new Category { Name = "Rings" }},
               new Item { ItemId = 2, TagNo = "T002", Name = "New Chain", Purity = "22K",
                          GrossWeight = 12m, NetWeight = 12m, Status = "InStock", BranchId = 1,
                          PurchaseDate = today.AddDays(-30), CostPrice = 90000m,
                          Category = new Category { Name = "Chains" }}
           ]);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(1, vm.AgingItemsCount);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 11 – CustomerViewModel Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class CustomerViewModelTests
{
    private static (CustomerViewModel vm, Mock<IUnitOfWork> uow) Create()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Customers.GetAllAsync(default)).ReturnsAsync([]);
        uow.Setup(u => u.Customers.GetTopCustomersByVolumeAsync(50, It.IsAny<int>(), default))
           .ReturnsAsync([]);
        var audit = new Mock<IAuditLogger>();
        var svc = new StockTransferService(uow.Object, audit.Object);
        var nav = new Mock<NavigationService>(new Mock<IServiceProvider>().Object).Object;
        var state = new AppState { CurrentBranchId = 1 };
        var vm = new CustomerViewModel(nav, state, uow.Object, svc, NullLogger<CustomerViewModel>.Instance);
        return (vm, uow);
    }

    [Fact]
    public void InitialState_IsCorrect()
    {
        var (vm, _) = Create();
        Assert.False(vm.IsLoading);
        Assert.Empty(vm.Customers);
        Assert.Equal("Name", vm.SelectedSortField);
        Assert.False(vm.IsEditMode);
    }

    [Fact]
    public async Task LoadCommand_SetsIsLoadingFalse()
    {
        var (vm, _) = Create();
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadCommand_PopulatesCustomers_FromCurrentBranch()
    {
        var (vm, uow) = Create();
        uow.Setup(u => u.Customers.GetAllAsync(default))
           .ReturnsAsync([
               new Customer { CustomerId = 1, Name = "Alice", Phone = "9999999999", BranchId = 1 },
               new Customer { CustomerId = 2, Name = "Bob", Phone = "8888888888", BranchId = 2 }
           ]);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Single(vm.Customers); // only branch 1
        Assert.Equal("Alice", vm.Customers[0].Name);
    }

    [Fact]
    public void NewCustomerCommand_SetsEditMode()
    {
        var (vm, _) = Create();
        vm.NewCustomerCommand.Execute(null);
        Assert.True(vm.IsEditMode);
        Assert.True(vm.IsNewCustomer);
    }

    [Fact]
    public void CancelEditCommand_ClearsEditMode()
    {
        var (vm, _) = Create();
        vm.NewCustomerCommand.Execute(null);
        vm.CancelEditCommand.Execute(null);
        Assert.False(vm.IsEditMode);
    }

    [Fact]
    public async Task SaveCustomerCommand_MissingName_SetsError()
    {
        var (vm, _) = Create();
        vm.NewCustomerCommand.Execute(null);
        vm.EditName = string.Empty;
        vm.EditPhone = "9999999999";
        await vm.SaveCustomerCommand.ExecuteAsync(null);
        Assert.True(vm.HasError);
        Assert.Contains("name", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveCustomerCommand_MissingPhone_SetsError()
    {
        var (vm, _) = Create();
        vm.NewCustomerCommand.Execute(null);
        vm.EditName = "Alice";
        vm.EditPhone = string.Empty;
        await vm.SaveCustomerCommand.ExecuteAsync(null);
        Assert.True(vm.HasError);
        Assert.Contains("phone", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveCustomerCommand_ShortPhone_SetsError()
    {
        var (vm, _) = Create();
        vm.NewCustomerCommand.Execute(null);
        vm.EditName = "Alice";
        vm.EditPhone = "12345";
        await vm.SaveCustomerCommand.ExecuteAsync(null);
        Assert.True(vm.HasError);
    }

    [Fact]
    public async Task SaveCustomerCommand_ValidInput_AddsCustomer()
    {
        var (vm, uow) = Create();
        uow.Setup(u => u.Customers.AddAsync(It.IsAny<Customer>(), default)).Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        uow.Setup(u => u.Customers.GetAllAsync(default)).ReturnsAsync([]);

        vm.NewCustomerCommand.Execute(null);
        vm.EditName = "New Customer";
        vm.EditPhone = "9000000001";
        await vm.SaveCustomerCommand.ExecuteAsync(null);

        uow.Verify(u => u.Customers.AddAsync(It.Is<Customer>(c => c.Name == "New Customer"), default), Times.Once);
    }

    [Fact]
    public void CustomerSearchText_FiltersCustomers()
    {
        var (vm, _) = Create();
        vm.Customers.Add(new Customer { Name = "Alice Kumar", Phone = "9999999999" });
        vm.Customers.Add(new Customer { Name = "Bob Patel", Phone = "8888888888" });
        vm.ApplyCustomerFilterCommand.Execute(null);
        vm.CustomerSearchText = "Alice";
        Assert.Single(vm.FilteredCustomers);
    }

    [Fact]
    public void LoyaltyInfo_TierNames_AreCorrect()
    {
        Assert.Equal("Silver", LoyaltyTier.Silver.ToString());
        Assert.Equal("Gold", LoyaltyTier.Gold.ToString());
        Assert.Equal("Platinum", LoyaltyTier.Platinum.ToString());
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 11 – LoyaltyInfo Model Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class LoyaltyModelTests
{
    [Fact]
    public void LoyaltyInfo_Record_CreatesCorrectly()
    {
        var info = new LoyaltyInfo(
            CustomerId: 1,
            CustomerName: "Alice",
            TotalPoints: 500,
            Tier: LoyaltyTier.Gold,
            TotalPurchased: 200_000m,
            PointsValueInRupees: 250m,
            PointsToNextTier: 300,
            NextTierName: "Platinum");

        Assert.Equal(1, info.CustomerId);
        Assert.Equal("Alice", info.CustomerName);
        Assert.Equal(LoyaltyTier.Gold, info.Tier);
        Assert.Equal(250m, info.PointsValueInRupees);
    }

    [Fact]
    public void LoyaltyRedemptionResult_SuccessCase()
    {
        var result = new LoyaltyRedemptionResult(true, 100, 50m, 400, "Redeemed successfully");
        Assert.True(result.Success);
        Assert.Equal(50m, result.DiscountAmount);
        Assert.Equal(400, result.RemainingPoints);
    }

    [Fact]
    public void StockTransferRequest_DefaultRemarks_IsEmpty()
    {
        var req = new StockTransferRequest(1, 1, 2, 99);
        Assert.Equal(string.Empty, req.Remarks);
    }

    [Fact]
    public void InventoryItemDto_DaysInStock_IsComputed()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = new InventoryItemDto(1, "H001", "T001", "Ring", "22K", 8m, 8m,
                                       "InStock", "Rings", 1, today.AddDays(-30), 60000m, 30);
        Assert.Equal(30, dto.DaysInStock);
    }

    [Fact]
    public void CustomerLedgerEntry_OutstandingBalance_IsCorrect()
    {
        var entry = new CustomerLedgerEntry(1, "BILL-001",
            DateOnly.FromDateTime(DateTime.Today),
            100000m, 80000m, 20000m, "Partial", "Cash", 3);
        Assert.Equal(20000m, entry.BalanceDue);
    }
}
