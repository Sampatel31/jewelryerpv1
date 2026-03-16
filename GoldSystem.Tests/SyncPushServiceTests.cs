using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Sync.Models;
using GoldSystem.Sync.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for <see cref="SyncPushService"/>.
/// Owner DB interaction is skipped via a mock connectivity service that returns
/// false, covering the "network unreachable" and "no pending records" paths.
/// </summary>
public class SyncPushServiceTests : IDisposable
{
    private readonly GoldDbContext _context;
    private readonly IUnitOfWork _uow;
    private readonly Mock<INetworkConnectivityService> _connectivityMock;

    public SyncPushServiceTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase($"SyncPushTest_{Guid.NewGuid()}")
            .Options;
        _context = new GoldDbContext(options);
        _context.Database.EnsureCreated();
        _uow = new UnitOfWork(_context);
        _connectivityMock = new Mock<INetworkConnectivityService>();
    }

    public void Dispose() => _context.Dispose();

    private SyncPushService CreateService() =>
        new(_uow, _connectivityMock.Object, NullLogger<SyncPushService>.Instance);

    // ── Branch not found ───────────────────────────────────────────────────────

    [Fact]
    public async Task PushPendingSyncsAsync_BranchNotFound_ThrowsInvalidOperationException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PushPendingSyncsAsync(9999));
    }

    // ── Network unreachable ────────────────────────────────────────────────────

    [Fact]
    public async Task PushPendingSyncsAsync_OwnerUnreachable_ReturnsDeferredAck()
    {
        // Seed a non-owner branch and an owner branch.
        _context.Branches.Add(new Branch
        {
            BranchId = 10,
            Code = "B1", Name = "Branch 1",
            IsOwnerBranch = false,
            SqlConnectionString = "Server=branch1",
            IsActive = true
        });
        _context.Branches.Add(new Branch
        {
            BranchId = 11,
            Code = "HQ", Name = "Head Office",
            IsOwnerBranch = true,
            SqlConnectionString = "Server=192.168.1.100",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        _connectivityMock
            .Setup(c => c.CanReachOwnerDbAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var service = CreateService();
        var ack = await service.PushPendingSyncsAsync(10);

        Assert.Equal(0, ack.SyncedCount);
        Assert.Equal(0, ack.FailedCount);
        Assert.Empty(ack.Errors);
    }

    // ── No pending records ─────────────────────────────────────────────────────

    [Fact]
    public async Task PushPendingSyncsAsync_NoPendingRecords_ReturnsEmptyAck()
    {
        _context.Branches.Add(new Branch
        {
            BranchId = 20,
            Code = "B2", Name = "Branch 2",
            IsOwnerBranch = false,
            SqlConnectionString = "Server=branch2",
            IsActive = true
        });
        _context.Branches.Add(new Branch
        {
            BranchId = 21,
            Code = "HQ2", Name = "HQ 2",
            IsOwnerBranch = true,
            SqlConnectionString = "Server=192.168.1.200",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        _connectivityMock
            .Setup(c => c.CanReachOwnerDbAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        var service = CreateService();
        var ack = await service.PushPendingSyncsAsync(20);

        Assert.Equal(0, ack.SyncedCount);
        Assert.Empty(ack.Errors);
    }

    // ── Owner PC skips push ────────────────────────────────────────────────────

    [Fact]
    public async Task PushPendingSyncsAsync_OwnerBranch_ReturnsSilentAck()
    {
        _context.Branches.Add(new Branch
        {
            BranchId = 30,
            Code = "HQ3", Name = "HQ 3",
            IsOwnerBranch = true,
            SqlConnectionString = "Server=localhost",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var service = CreateService();
        var ack = await service.PushPendingSyncsAsync(30);

        // No connectivity check and no push should happen.
        _connectivityMock.Verify(
            c => c.CanReachOwnerDbAsync(It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);

        Assert.Equal(0, ack.SyncedCount);
    }

    // ── ExtractIpFromConnectionString helper ───────────────────────────────────

    [Fact]
    public void ExtractIpFromConnectionString_ValidConnectionString_ReturnsIp()
    {
        var ip = SyncPushService.ExtractIpFromConnectionString(
            "Server=192.168.1.100,1433;Database=GoldDB;Trusted_Connection=True;");

        Assert.Equal("192.168.1.100", ip);
    }

    [Fact]
    public void ExtractIpFromConnectionString_EmptyString_ReturnsLocalhost()
    {
        var ip = SyncPushService.ExtractIpFromConnectionString(string.Empty);
        Assert.Equal("localhost", ip);
    }
}
