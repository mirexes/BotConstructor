using BotConstructor.Core.DTOs;
using BotConstructor.Core.Entities;
using BotConstructor.Core.Models;

namespace BotConstructor.Core.Interfaces;

public interface IAdminService
{
    Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        bool? isBlocked = null,
        bool? emailConfirmed = null,
        DateTime? registeredAfter = null,
        DateTime? registeredBefore = null);

    Task<User?> GetUserByIdAsync(int id);
    Task<IEnumerable<LoginAttempt>> GetUserLoginHistoryAsync(int userId);
    Task<ServiceResult> BlockUserAsync(int userId, string reason);
    Task<ServiceResult> UnblockUserAsync(int userId);
    Task<ServiceResult> DeleteUserAsync(int userId);
    Task<ServiceResult> ConfirmUserEmailAsync(int userId);
    Task<ServiceResult> ResetUserPasswordAsync(int userId, string newPassword);
    Task<IEnumerable<Role>> GetAllRolesAsync();
    Task<ServiceResult> AssignRoleAsync(int userId, int roleId);
    Task<ServiceResult> RemoveRoleAsync(int userId, int roleId);
}
