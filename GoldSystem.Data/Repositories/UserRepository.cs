using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(GoldDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default)
        => await DbSet.Where(u => u.Role == role).OrderBy(u => u.Name).ToListAsync(cancellationToken);

    public async Task<IEnumerable<User>> GetUsersByBranchAsync(int branchId, CancellationToken cancellationToken = default)
        => await DbSet.Where(u => u.BranchId == branchId).OrderBy(u => u.Name).ToListAsync(cancellationToken);

    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
        => await DbSet.Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync(cancellationToken);

    public async Task UpdateLastLoginAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await DbSet.FindAsync(new object[] { userId }, cancellationToken);
        if (user is null) return;
        user.LastLoginAt = DateTime.UtcNow;
    }
}
