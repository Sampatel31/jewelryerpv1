using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using GoldSystem.WPF.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldSystem.WPF.Tests;

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 15 – Error Handling Service Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class ErrorHandlingServiceTests
{
    private static readonly ErrorHandlingService _svc =
        new(NullLogger<ErrorHandlingService>.Instance);

    [Fact]
    public async Task HandleExceptionAsync_ValidationException_DoesNotThrow()
    {
        var ex = new ValidationException("Field is required");
        await _svc.HandleExceptionAsync(ex);   // must complete without throwing
    }

    [Fact]
    public async Task HandleExceptionAsync_BusinessLogicException_DoesNotThrow()
    {
        var ex = new BusinessLogicException("Cannot delete active record");
        await _svc.HandleExceptionAsync(ex);
    }

    [Fact]
    public async Task HandleExceptionAsync_GenericException_DoesNotThrow()
    {
        var ex = new Exception("Something went wrong");
        await _svc.HandleExceptionAsync(ex);
    }

    [Fact]
    public async Task HandleExceptionAsync_TimeoutException_DoesNotThrow()
    {
        await _svc.HandleExceptionAsync(new TimeoutException());
    }

    [Fact]
    public async Task HandleExceptionAsync_InvalidOperationException_DoesNotThrow()
    {
        await _svc.HandleExceptionAsync(new InvalidOperationException("bad state"));
    }

    [Fact]
    public void ShowErrorDialog_DoesNotThrow()
    {
        _svc.ShowErrorDialog("Something failed", "Error");
    }

    [Fact]
    public void ShowSuccessToast_DoesNotThrow()
    {
        _svc.ShowSuccessToast("Saved successfully");
    }

    [Fact]
    public void ShowWarningToast_DoesNotThrow()
    {
        _svc.ShowWarningToast("Low stock warning");
    }

    [Fact]
    public void ShowInfoToast_DoesNotThrow()
    {
        _svc.ShowInfoToast("Loading data…");
    }

    [Fact]
    public void ValidateInput_PassingRule_DoesNotThrow()
    {
        _svc.ValidateInput("Name", "Gold Ring", () => true, "Name is required");
    }

    [Fact]
    public void ValidateInput_FailingRule_ThrowsValidationException()
    {
        Assert.Throws<ValidationException>(() =>
            _svc.ValidateInput("Name", "", () => false, "Name is required"));
    }

    [Fact]
    public void ValidateInput_FailingRule_MessageContainsFieldName()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            _svc.ValidateInput("BillNo", "", () => false, "Cannot be empty"));
        Assert.Contains("BillNo", ex.Message);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 15 – Custom Exception Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class Phase15ExceptionTests
{
    [Fact]
    public void ValidationException_StoresMessage()
    {
        const string msg = "Weight must be positive";
        var ex = new ValidationException(msg);
        Assert.Equal(msg, ex.Message);
    }

    [Fact]
    public void BusinessLogicException_StoresMessage()
    {
        const string msg = "Insufficient stock";
        var ex = new BusinessLogicException(msg);
        Assert.Equal(msg, ex.Message);
    }

    [Fact]
    public void DataException_StoresMessageAndInnerException()
    {
        var inner = new InvalidOperationException("DB error");
        var ex    = new DataException("Database error", inner);
        Assert.Equal("Database error", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void ValidationException_IsSubclassOfException()
    {
        Assert.IsAssignableFrom<Exception>(new ValidationException("x"));
    }

    [Fact]
    public void BusinessLogicException_IsSubclassOfException()
    {
        Assert.IsAssignableFrom<Exception>(new BusinessLogicException("x"));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 15 – Logging Service Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class LoggingServiceTests
{
    private static readonly LoggingService _svc =
        new(NullLogger<LoggingService>.Instance);

    [Fact]
    public void LogDebug_DoesNotThrow()   => _svc.LogDebug("Debug: {Value}", 42);

    [Fact]
    public void LogInfo_DoesNotThrow()    => _svc.LogInfo("Started module {Module}", "Billing");

    [Fact]
    public void LogWarning_DoesNotThrow() => _svc.LogWarning("Low stock for {Item}", "Ring");

    [Fact]
    public void LogError_DoesNotThrow()
    {
        var ex = new Exception("test error");
        _svc.LogError(ex, "Error in {Operation}", "Save");
    }

    [Fact]
    public void LogFatal_DoesNotThrow()
    {
        var ex = new Exception("fatal");
        _svc.LogFatal(ex, "Fatal in {Module}", "Auth");
    }

    [Fact]
    public void LogPerformance_FastOperation_DoesNotThrow()
        => _svc.LogPerformance("LoadCustomers", 150);

    [Fact]
    public void LogPerformance_SlowOperation_DoesNotThrow()
        => _svc.LogPerformance("GenerateReport", 2500);

    [Fact]
    public void LogUserAction_DoesNotThrow()
        => _svc.LogUserAction("user1", "Create", "Billing", "Bill");

    [Fact]
    public void LogApiCall_SuccessStatus_DoesNotThrow()
        => _svc.LogApiCall("GET", "/api/rates", 200, 80);

    [Fact]
    public void LogApiCall_ErrorStatus_DoesNotThrow()
        => _svc.LogApiCall("POST", "/api/bills", 500, 300);
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 15 – Caching Service Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class CachingServiceTests
{
    private static CachingService CreateService() =>
        new(new MemoryCache(new MemoryCacheOptions()),
            NullLogger<CachingService>.Instance);

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsValue()
    {
        var svc = CreateService();
        await svc.SetAsync("key1", "hello");
        var result = await svc.GetAsync<string>("key1");
        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task GetAsync_MissingKey_ReturnsDefault()
    {
        var svc    = CreateService();
        var result = await svc.GetAsync<string>("missing");
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_ThenGetAsync_ReturnsNull()
    {
        var svc = CreateService();
        await svc.SetAsync("key2", 42);
        await svc.RemoveAsync("key2");
        var result = await svc.GetAsync<int?>("key2");
        Assert.Null(result);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllTrackedKeys()
    {
        var svc = CreateService();
        await svc.SetAsync("a", 1);
        await svc.SetAsync("b", 2);
        await svc.ClearAsync();
        Assert.Null(await svc.GetAsync<int?>("a"));
        Assert.Null(await svc.GetAsync<int?>("b"));
    }

    [Fact]
    public async Task GetOrCreateAsync_MissingKey_InvokesFactory()
    {
        var svc     = CreateService();
        var invoked = false;
        var result  = await svc.GetOrCreateAsync("key3", () =>
        {
            invoked = true;
            return Task.FromResult("created");
        });
        Assert.True(invoked);
        Assert.Equal("created", result);
    }

    [Fact]
    public async Task GetOrCreateAsync_ExistingKey_DoesNotInvokeFactory()
    {
        var svc = CreateService();
        await svc.SetAsync("key4", "existing");
        var invoked = false;
        var result  = await svc.GetOrCreateAsync("key4", () =>
        {
            invoked = true;
            return Task.FromResult("new");
        });
        Assert.False(invoked);
        Assert.Equal("existing", result);
    }

    [Fact]
    public async Task SetAsync_WithExplicitTtl_StoresValue()
    {
        var svc = CreateService();
        await svc.SetAsync("ttlKey", 99, TimeSpan.FromMinutes(5));
        var result = await svc.GetAsync<int?>("ttlKey");
        Assert.Equal(99, result);
    }

    [Fact]
    public async Task SetAsync_ComplexObject_RoundTrips()
    {
        var svc = CreateService();
        var obj = new OperationMetric
        {
            OperationName    = "Test",
            AverageDurationMs = 123.4,
            MaxDurationMs    = 500,
            MinDurationMs    = 50,
            ExecutionCount   = 10
        };
        await svc.SetAsync("metric", obj);
        var result = await svc.GetAsync<OperationMetric>("metric");
        Assert.NotNull(result);
        Assert.Equal("Test", result!.OperationName);
        Assert.Equal(10, result.ExecutionCount);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 15 – Performance Service Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class PerformanceServiceTests
{
    private static PerformanceService CreateService() =>
        new(new LoggingService(NullLogger<LoggingService>.Instance));

    [Fact]
    public async Task MeasureAsync_ReturnsActionResult()
    {
        var svc    = CreateService();
        var result = await svc.MeasureAsync("op1", () => Task.FromResult(42));
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task MeasureAsync_Void_CompletesSuccessfully()
    {
        var svc     = CreateService();
        var executed = false;
        await svc.MeasureAsync("op2", () => { executed = true; return Task.CompletedTask; });
        Assert.True(executed);
    }

    [Fact]
    public async Task GetMetrics_AfterOneMeasurement_ReturnsOneEntry()
    {
        var svc = CreateService();
        await svc.MeasureAsync("opA", () => Task.FromResult(1));
        var metrics = svc.GetMetrics();
        Assert.Single(metrics.Operations);
        Assert.Equal("opA", metrics.Operations[0].OperationName);
    }

    [Fact]
    public async Task GetMetrics_MultipleMeasurements_AccumulatesStats()
    {
        var svc = CreateService();
        await svc.MeasureAsync("opB", () => Task.FromResult(1));
        await svc.MeasureAsync("opB", () => Task.FromResult(2));
        await svc.MeasureAsync("opB", () => Task.FromResult(3));
        var metrics = svc.GetMetrics();
        var op = Assert.Single(metrics.Operations);
        Assert.Equal(3, op.ExecutionCount);
        Assert.True(op.MaxDurationMs >= op.MinDurationMs);
    }

    [Fact]
    public async Task GetMetrics_DifferentOperations_AreTrackedSeparately()
    {
        var svc = CreateService();
        await svc.MeasureAsync("opX", () => Task.FromResult(0));
        await svc.MeasureAsync("opY", () => Task.FromResult(0));
        var metrics = svc.GetMetrics();
        Assert.Equal(2, metrics.Operations.Count);
        Assert.Contains(metrics.Operations, o => o.OperationName == "opX");
        Assert.Contains(metrics.Operations, o => o.OperationName == "opY");
    }

    [Fact]
    public async Task MeasureAsync_PropagatesExceptions()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await svc.MeasureAsync<int>("failOp",
                () => throw new InvalidOperationException("boom")));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 15 – PerformanceMetrics Model Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class PerformanceMetricsModelTests
{
    [Fact]
    public void PerformanceMetrics_DefaultsToEmptyList()
    {
        var m = new PerformanceMetrics();
        Assert.NotNull(m.Operations);
        Assert.Empty(m.Operations);
    }

    [Fact]
    public void OperationMetric_DefaultValues_AreZeroOrEmpty()
    {
        var op = new OperationMetric();
        Assert.Equal(string.Empty, op.OperationName);
        Assert.Equal(0.0, op.AverageDurationMs);
        Assert.Equal(0L, op.MaxDurationMs);
        Assert.Equal(0L, op.MinDurationMs);
        Assert.Equal(0, op.ExecutionCount);
    }

    [Fact]
    public void OperationMetric_SetProperties_Persist()
    {
        var op = new OperationMetric
        {
            OperationName     = "LoadBills",
            AverageDurationMs = 250.5,
            MaxDurationMs     = 500,
            MinDurationMs     = 100,
            ExecutionCount    = 20
        };
        Assert.Equal("LoadBills", op.OperationName);
        Assert.Equal(250.5,       op.AverageDurationMs);
        Assert.Equal(500L,        op.MaxDurationMs);
        Assert.Equal(100L,        op.MinDurationMs);
        Assert.Equal(20,          op.ExecutionCount);
    }
}
