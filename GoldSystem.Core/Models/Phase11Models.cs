namespace GoldSystem.Core.Models;

// ─── Stock Transfer Models ───────────────────────────────────────────────────

public record StockTransferRequest(
    int ItemId,
    int FromBranchId,
    int ToBranchId,
    int UserId,
    string Remarks = "");

public record StockTransferDto(
    int TransferId,
    int ItemId,
    string ItemName,
    string TagNo,
    string HUID,
    int FromBranchId,
    string FromBranchName,
    int ToBranchId,
    string ToBranchName,
    int UserId,
    DateTime TransferredAt,
    string Remarks);

// ─── Customer Ledger Models ──────────────────────────────────────────────────

public record CustomerLedgerEntry(
    int BillId,
    string BillNo,
    DateOnly BillDate,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal BalanceDue,
    string Status,
    string PaymentMode,
    int ItemCount);

// ─── Loyalty Models ──────────────────────────────────────────────────────────

public enum LoyaltyTier { Silver, Gold, Platinum }

public record LoyaltyInfo(
    int CustomerId,
    string CustomerName,
    int TotalPoints,
    LoyaltyTier Tier,
    decimal TotalPurchased,
    decimal PointsValueInRupees,
    int PointsToNextTier,
    string NextTierName);

public record LoyaltyRedemptionRequest(
    int CustomerId,
    int PointsToRedeem,
    int UserId);

public record LoyaltyRedemptionResult(
    bool Success,
    int PointsRedeemed,
    decimal DiscountAmount,
    int RemainingPoints,
    string Message);

// ─── Inventory Item Display Model ────────────────────────────────────────────

public record InventoryItemDto(
    int ItemId,
    string? HUID,
    string TagNo,
    string Name,
    string Purity,
    decimal GrossWeight,
    decimal NetWeight,
    string Status,
    string CategoryName,
    int BranchId,
    DateOnly PurchaseDate,
    decimal CostPrice,
    int DaysInStock);
