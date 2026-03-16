using GoldSystem.Core.Models;
using GoldSystem.Core.Services;

namespace GoldSystem.Data.Services;

/// <summary>
/// Validates all inputs for bill creation before any database writes occur.
/// </summary>
public class BillingValidationService : IBillingValidationService
{
    private static readonly string[] ValidPaymentModes =
        ["Cash", "Card", "UPI", "NEFT", "Split", "OldGoldExchange"];

    private readonly IUnitOfWork _uow;

    public BillingValidationService(IUnitOfWork uow)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<BillValidationResult> ValidateCreateBillAsync(CreateBillRequest request)
    {
        var errors = new List<string>();

        // Validate customer exists
        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId);
        if (customer == null)
            errors.Add("Customer not found");

        if (request.Items == null || request.Items.Count == 0)
        {
            errors.Add("Bill must contain at least one item");
        }
        else
        {
            // Validate all items exist and are in stock at the correct branch
            foreach (var itemReq in request.Items)
            {
                var dbItem = await _uow.Items.GetByIdAsync(itemReq.ItemId);
                if (dbItem == null)
                {
                    errors.Add($"Item {itemReq.ItemId} not found");
                }
                else if (dbItem.Status != "InStock")
                {
                    errors.Add($"Item {dbItem.HUID ?? dbItem.TagNo} is not available for sale");
                }
                else if (dbItem.BranchId != request.BranchId)
                {
                    errors.Add($"Item {dbItem.HUID ?? dbItem.TagNo} belongs to a different branch");
                }
            }
        }

        // Validate monetary amounts
        if (request.AmountPaid < 0)
            errors.Add("Amount paid cannot be negative");

        if (request.DiscountAmount < 0)
            errors.Add("Discount cannot be negative");

        if (request.ExchangeValue < 0)
            errors.Add("Exchange value cannot be negative");

        // Validate payment mode
        if (!ValidPaymentModes.Contains(request.PaymentMode))
            errors.Add($"Invalid payment mode '{request.PaymentMode}'");

        return new BillValidationResult(IsValid: errors.Count == 0, Errors: errors);
    }
}
