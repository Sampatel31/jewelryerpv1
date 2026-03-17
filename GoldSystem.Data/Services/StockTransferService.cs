using GoldSystem.Core.Models;
using GoldSystem.Core.Services;
using GoldSystem.Data;
using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Services;

/// <summary>
/// Phase 11 – handles atomic stock transfers between branches with full validation.
/// </summary>
public class StockTransferService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogger _auditLogger;

    public StockTransferService(IUnitOfWork uow, IAuditLogger auditLogger)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    }

    /// <summary>
    /// Transfers an item from one branch to another atomically.
    /// Validates that the item exists, is InStock, and belongs to the source branch.
    /// </summary>
    public async Task<StockTransferDto> TransferItemAsync(
        StockTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.FromBranchId == request.ToBranchId)
            throw new InvalidOperationException("Source and destination branches must be different.");

        var item = await _uow.Items.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Item {request.ItemId} not found.");

        if (item.Status != "InStock")
            throw new InvalidOperationException($"Item '{item.TagNo}' is not in stock (status: {item.Status}).");

        if (item.BranchId != request.FromBranchId)
            throw new InvalidOperationException(
                $"Item '{item.TagNo}' does not belong to branch {request.FromBranchId}.");

        var toBranch = await _uow.Branches.GetByIdAsync(request.ToBranchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Destination branch {request.ToBranchId} not found.");

        var fromBranch = await _uow.Branches.GetByIdAsync(request.FromBranchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Source branch {request.FromBranchId} not found.");

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            item.BranchId = request.ToBranchId;
            await _uow.Items.UpdateAsync(item, cancellationToken);

            await _auditLogger.LogAsync(
                userId: request.UserId,
                action: "StockTransfer",
                tableName: "Items",
                recordId: item.ItemId,
                oldValueJson: $"{{\"BranchId\":{request.FromBranchId}}}",
                newValueJson: $"{{\"BranchId\":{request.ToBranchId},\"Remarks\":\"{request.Remarks}\"}}",
                branchId: request.FromBranchId);

            await _uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }

        return new StockTransferDto(
            TransferId: 0,
            ItemId: item.ItemId,
            ItemName: item.Name,
            TagNo: item.TagNo,
            HUID: item.HUID ?? string.Empty,
            FromBranchId: request.FromBranchId,
            FromBranchName: fromBranch.Name,
            ToBranchId: request.ToBranchId,
            ToBranchName: toBranch.Name,
            UserId: request.UserId,
            TransferredAt: DateTime.UtcNow,
            Remarks: request.Remarks);
    }

    /// <summary>
    /// Returns all items for a given branch, mapped to InventoryItemDto.
    /// </summary>
    public async Task<IReadOnlyList<InventoryItemDto>> GetInventoryAsync(
        int branchId,
        CancellationToken cancellationToken = default)
    {
        var items = await _uow.Items.GetInventoryByBranchAsync(branchId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);

        return items.Select(i => new InventoryItemDto(
            ItemId: i.ItemId,
            HUID: i.HUID,
            TagNo: i.TagNo,
            Name: i.Name,
            Purity: i.Purity,
            GrossWeight: i.GrossWeight,
            NetWeight: i.NetWeight,
            Status: i.Status,
            CategoryName: i.Category?.Name ?? string.Empty,
            BranchId: i.BranchId,
            PurchaseDate: i.PurchaseDate,
            CostPrice: i.CostPrice,
            DaysInStock: today.DayNumber - i.PurchaseDate.DayNumber))
        .ToList();
    }

    /// <summary>
    /// Calculates loyalty tier based on total purchased amount.
    /// Silver: 0–99,999 | Gold: 1,00,000–4,99,999 | Platinum: 5,00,000+
    /// </summary>
    public static LoyaltyTier GetTier(decimal totalPurchased) => totalPurchased switch
    {
        >= 500_000m => LoyaltyTier.Platinum,
        >= 100_000m => LoyaltyTier.Gold,
        _ => LoyaltyTier.Silver
    };

    /// <summary>
    /// Accrues loyalty points for a customer based on purchase amount.
    /// 1 point per ₹1,000 spent.
    /// </summary>
    public static int CalculatePointsEarned(decimal purchaseAmount)
        => (int)(purchaseAmount / 1000m);

    /// <summary>
    /// Redeems loyalty points for a discount. 100 points = ₹50 discount.
    /// </summary>
    public async Task<LoyaltyRedemptionResult> RedeemPointsAsync(
        LoyaltyRedemptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PointsToRedeem <= 0)
            return new LoyaltyRedemptionResult(false, 0, 0, 0, "Points to redeem must be positive.");

        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Customer {request.CustomerId} not found.");

        if (customer.LoyaltyPoints < request.PointsToRedeem)
            return new LoyaltyRedemptionResult(
                false, 0, 0, customer.LoyaltyPoints,
                $"Insufficient points. Available: {customer.LoyaltyPoints}, Requested: {request.PointsToRedeem}");

        var discountAmount = (request.PointsToRedeem / 100m) * 50m;
        var newPoints = customer.LoyaltyPoints - request.PointsToRedeem;

        await _uow.Customers.UpdateLoyaltyPointsAsync(request.CustomerId, newPoints, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new LoyaltyRedemptionResult(
            true, request.PointsToRedeem, discountAmount, newPoints,
            $"Redeemed {request.PointsToRedeem} points for ₹{discountAmount:N0} discount.");
    }
}
