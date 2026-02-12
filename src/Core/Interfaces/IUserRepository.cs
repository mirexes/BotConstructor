using BotConstructor.Core.Entities;

namespace BotConstructor.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> EmailExistsAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();

    // Admin methods
    Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        bool? isBlocked = null,
        bool? emailConfirmed = null,
        DateTime? registeredAfter = null,
        DateTime? registeredBefore = null);

    Task<IEnumerable<LoginAttempt>> GetUserLoginHistoryAsync(int userId, int limit = 50);
    Task DeleteAsync(User user);
    Task<IEnumerable<Role>> GetAllRolesAsync();
    Task AddUserRoleAsync(int userId, int roleId);
    Task RemoveUserRoleAsync(int userId, int roleId);
}
