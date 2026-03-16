namespace GoldSystem.Core.Models;

// ─── Request models ──────────────────────────────────────────────────────────

public record CreateBillRequest(
    int CustomerId,
    List<AddBillItemRequest> Items,
    decimal DiscountAmount,
    decimal ExchangeValue,
    string PaymentMode,
    decimal AmountPaid,
    int UserId,
    int BranchId);

public record AddBillItemRequest(
    int ItemId,
    decimal AdjustedGrossWeight = 0,
    decimal AdjustedStoneWeight = 0);

// ─── Response / DTO models ───────────────────────────────────────────────────

public record CustomerDto(
    int CustomerId,
    string Name,
    string Phone);

public record ItemDto(
    int ItemId,
    string HUID,
    string TagNo,
    string Name,
    string Purity,
    decimal GrossWeight,
    decimal StoneWeight,
    string Status,
    DateTime CreatedAt);

public record BillItemDto(
    int BillItemId,
    ItemDto Item,
    decimal GrossWeight,
    decimal StoneWeight,
    decimal NetWeight,
    decimal WastagePercent,
    decimal WastageWeight,
    decimal BillableWeight,
    decimal PureGoldWeight,
    decimal RateUsed24K,
    decimal GoldValue,
    decimal MakingAmount,
    decimal StoneCharge,
    decimal CGST_Amount,
    decimal SGST_Amount,
    decimal LineTotal);

public record BillDto(
    int BillId,
    string BillNo,
    DateOnly BillDate,
    CustomerDto Customer,
    List<BillItemDto> Items,
    decimal GoldValue,
    decimal MakingAmount,
    decimal WastageAmount,
    decimal StoneCharge,
    decimal DiscountAmount,
    decimal CGST,
    decimal SGST,
    decimal IGST,
    decimal RoundOff,
    decimal GrandTotal,
    decimal ExchangeValue,
    decimal AmountPaid,
    decimal BalanceDue,
    string Status,
    string PaymentMode,
    bool IsLocked,
    DateTime CreatedAt);
