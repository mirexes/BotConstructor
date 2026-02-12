using BotConstructor.Core.DTOs;
using BotConstructor.Core.Entities;
using BotConstructor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BotConstructor.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AdminService> _logger;

    public AdminService(IUserRepository userRepository, ILogger<AdminService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        bool? isBlocked = null,
        bool? emailConfirmed = null,
        DateTime? registeredAfter = null,
        DateTime? registeredBefore = null)
    {
        return await _userRepository.GetUsersPagedAsync(
            page,
            pageSize,
            searchTerm,
            isBlocked,
            emailConfirmed,
            registeredAfter,
            registeredBefore);
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<LoginAttempt>> GetUserLoginHistoryAsync(int userId)
    {
        return await _userRepository.GetUserLoginHistoryAsync(userId);
    }

    public async Task<ServiceResult> BlockUserAsync(int userId, string reason)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new ServiceResult { Success = false, Message = "Пользователь не найден" };
        }

        if (user.IsBlocked)
        {
            return new ServiceResult { Success = false, Message = "Пользователь уже заблокирован" };
        }

        user.IsBlocked = true;
        user.BlockedAt = DateTime.UtcNow;
        user.BlockedReason = reason;

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation($"User {user.Email} (ID: {userId}) has been blocked. Reason: {reason}");

        return new ServiceResult { Success = true, Message = "Пользователь успешно заблокирован" };
    }

    public async Task<ServiceResult> UnblockUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new ServiceResult { Success = false, Message = "Пользователь не найден" };
        }

        if (!user.IsBlocked)
        {
            return new ServiceResult { Success = false, Message = "Пользователь не заблокирован" };
        }

        user.IsBlocked = false;
        user.BlockedAt = null;
        user.BlockedReason = null;

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation($"User {user.Email} (ID: {userId}) has been unblocked");

        return new ServiceResult { Success = true, Message = "Пользователь успешно разблокирован" };
    }

    public async Task<ServiceResult> DeleteUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new ServiceResult { Success = false, Message = "Пользователь не найден" };
        }

        await _userRepository.DeleteAsync(user);
        _logger.LogWarning($"User {user.Email} (ID: {userId}) has been deleted");

        return new ServiceResult { Success = true, Message = "Пользователь успешно удален" };
    }

    public async Task<ServiceResult> ConfirmUserEmailAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new ServiceResult { Success = false, Message = "Пользователь не найден" };
        }

        if (user.EmailConfirmed)
        {
            return new ServiceResult { Success = false, Message = "Email уже подтвержден" };
        }

        user.EmailConfirmed = true;
        user.EmailConfirmedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation($"Email for user {user.Email} (ID: {userId}) has been manually confirmed");

        return new ServiceResult { Success = true, Message = "Email успешно подтвержден" };
    }

    public async Task<ServiceResult> ResetUserPasswordAsync(int userId, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new ServiceResult { Success = false, Message = "Пользователь не найден" };
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            return new ServiceResult { Success = false, Message = "Пароль должен содержать минимум 8 символов" };
        }

        // Hash the new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 12);

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation($"Password for user {user.Email} (ID: {userId}) has been reset by admin");

        return new ServiceResult { Success = true, Message = "Пароль успешно изменен" };
    }

    public async Task<IEnumerable<Role>> GetAllRolesAsync()
    {
        return await _userRepository.GetAllRolesAsync();
    }

    public async Task<ServiceResult> AssignRoleAsync(int userId, int roleId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new ServiceResult { Success = false, Message = "Пользователь не найден" };
        }

        // Check if user already has this role
        if (user.UserRoles.Any(ur => ur.RoleId == roleId))
        {
            return new ServiceResult { Success = false, Message = "У пользователя уже есть эта роль" };
        }

        await _userRepository.AddUserRoleAsync(userId, roleId);
        _logger.LogInformation($"Role {roleId} assigned to user {user.Email} (ID: {userId})");

        return new ServiceResult { Success = true, Message = "Роль успешно назначена" };
    }

    public async Task<ServiceResult> RemoveRoleAsync(int userId, int roleId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new ServiceResult { Success = false, Message = "Пользователь не найден" };
        }

        // Check if user has this role
        if (!user.UserRoles.Any(ur => ur.RoleId == roleId))
        {
            return new ServiceResult { Success = false, Message = "У пользователя нет этой роли" };
        }

        await _userRepository.RemoveUserRoleAsync(userId, roleId);
        _logger.LogInformation($"Role {roleId} removed from user {user.Email} (ID: {userId})");

        return new ServiceResult { Success = true, Message = "Роль успешно удалена" };
    }
}
