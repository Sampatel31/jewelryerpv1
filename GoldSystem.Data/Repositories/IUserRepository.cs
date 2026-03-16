using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersByBranchAsync(int branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(int userId, CancellationToken cancellationToken = default);
}
