using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Sync.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for <see cref="SyncBackgroundService"/>.
/// </summary>
public class SyncBackgroundServiceTests : IDisposable
{
    private readonly GoldDbContext _context;
    private readonly IUnitOfWork _uow;
    private readonly Mock<ISyncPushService> _pushMock;

    public SyncBackgroundServiceTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase($"SyncBgTest_{Guid.NewGuid()}")
            .Options;
        _context = new GoldDbContext(options);
        _context.Database.EnsureCreated();
        _uow = new UnitOfWork(_context);
        _pushMock = new Mock<ISyncPushService>();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("BRANCH_ID", null);
        _context.Dispose();
    }

    private static IConfiguration BuildConfig(int intervalMinutes = 0) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SyncConfiguration:IntervalMinutes"] = intervalMinutes.ToString()
            })
            .Build();

    private SyncBackgroundService CreateService(IConfiguration? config = null) =>
        new(_uow, _pushMock.Object,
            NullLogger<SyncBackgroundService>.Instance,
            config ?? BuildConfig());

    // -- Service disabled on owner PC ------------------------------------------

    [Fact]
    public async Task ExecuteAsync_OwnerBranch_DoesNotCallPushService()
    {
        // Use high IDs to avoid collision with seeded data (BranchId = 1).
        _context.Branches.Add(new Branch
        {
            BranchId = 101,
            Code = "HQ_TEST", Name = "Head Office Test",
            IsOwnerBranch = true,
            SqlConnectionString = "Server=localhost",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        Environment.SetEnvironmentVariable("BRANCH_ID", "101");

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None);

        _pushMock.Verify(
            p => p.PushPendingSyncsAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    // -- Service runs for a branch PC ------------------------------------------

    [Fact]
    public async Task ExecuteAsync_BranchPc_CallsPushServiceAtLeastOnce()
    {
        _context.Branches.Add(new Branch
        {
            BranchId = 102,
            Code = "B1_TEST", Name = "Branch 1 Test",
            IsOwnerBranch = false,
            SqlConnectionString = "Server=192.168.1.2",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        Environment.SetEnvironmentVariable("BRANCH_ID", "102");

        _pushMock
            .Setup(p => p.PushPendingSyncsAsync(102, It.IsAny<int>()))
            .ReturnsAsync(new Sync.Models.SyncAck(0, 0, new List<Sync.Models.SyncError>(), DateTime.UtcNow));

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);
        // Give the first iteration time to run, then stop.
        await Task.Delay(300);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        _pushMock.Verify(
            p => p.PushPendingSyncsAsync(102, It.IsAny<int>()),
            Times.AtLeastOnce);
    }

    // -- Handles exceptions gracefully -----------------------------------------

    [Fact]
    public async Task ExecuteAsync_PushThrows_ServiceContinuesWithoutCrash()
    {
        _context.Branches.Add(new Branch
        {
            BranchId = 103,
            Code = "B2_TEST", Name = "Branch 2 Test",
            IsOwnerBranch = false,
            SqlConnectionString = "Server=192.168.1.3",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        Environment.SetEnvironmentVariable("BRANCH_ID", "103");

        _pushMock
            .Setup(p => p.PushPendingSyncsAsync(103, It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Network error"));

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Should not throw despite the push service throwing.
        await service.StartAsync(cts.Token);
        await Task.Delay(300);
        cts.Cancel();

        // StopAsync should complete cleanly.
        var ex = await Record.ExceptionAsync(() => service.StopAsync(CancellationToken.None));
        Assert.Null(ex);
    }

    // -- Graceful shutdown -----------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_StopsCleanly()
    {
        _context.Branches.Add(new Branch
        {
            BranchId = 104,
            Code = "B3_TEST", Name = "Branch 3 Test",
            IsOwnerBranch = false,
            SqlConnectionString = "Server=192.168.1.4",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        Environment.SetEnvironmentVariable("BRANCH_ID", "104");

        _pushMock
            .Setup(p => p.PushPendingSyncsAsync(104, It.IsAny<int>()))
            .ReturnsAsync(new Sync.Models.SyncAck(0, 0, new List<Sync.Models.SyncError>(), DateTime.UtcNow));

        // Use a long interval so the service blocks on Task.Delay after first push.
        var service = CreateService(BuildConfig(intervalMinutes: 60));
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);
        await Task.Delay(200); // let the first push run
        cts.Cancel();

        // StopAsync should complete without hanging.
        var stopTask = service.StopAsync(CancellationToken.None);
        var winner = await Task.WhenAny(stopTask, Task.Delay(3000));

        Assert.Same(stopTask, winner);
    }
}
