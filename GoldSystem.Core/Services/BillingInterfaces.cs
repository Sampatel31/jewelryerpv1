using GoldSystem.Core.Models;

namespace GoldSystem.Core.Services;

public interface IBillNumberGenerator
{
    Task<string> GenerateBillNoAsync(int branchId);
}

public record BillValidationResult(bool IsValid, List<string> Errors);

public interface IBillingValidationService
{
    Task<BillValidationResult> ValidateCreateBillAsync(CreateBillRequest request);
}

public interface IAuditLogger
{
    Task LogAsync(int userId, string action, string tableName, int recordId,
        string? oldValueJson = null, string? newValueJson = null, int branchId = 0);
}

public interface IBillingEngine
{
    Task<BillDto> CreateBillAsync(CreateBillRequest request);
    Task<BillDto> GetBillAsync(int billId);
    Task<BillDto> GetBillByNoAsync(string billNo);
    Task<(bool IsLocked, string Reason)> CanEditBillAsync(int billId);
    Task LockBillAsync(int billId, int userId = 0);
    Task PrintBillAsync(int billId, int userId = 0);
}
