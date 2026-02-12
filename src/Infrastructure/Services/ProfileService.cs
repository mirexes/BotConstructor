using BotConstructor.Core.DTOs;
using BotConstructor.Core.Entities;
using BotConstructor.Core.Interfaces;
using BotConstructor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BotConstructor.Infrastructure.Services;

/// <summary>
/// Сервис для работы с профилем пользователя
/// </summary>
public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(ApplicationDbContext context, ILogger<ProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Получить профиль пользователя по ID
    /// </summary>
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <summary>
    /// Обновить профиль пользователя
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "Пользователь не найден");
            }

            // Проверка, не занят ли новый email другим пользователем
            if (user.Email != dto.Email)
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == dto.Email && u.Id != userId);

                if (emailExists)
                {
                    return (false, "Этот email уже используется другим пользователем");
                }

                // Если email изменился, требуется повторное подтверждение
                user.Email = dto.Email;
                user.EmailConfirmed = false;
                _logger.LogInformation($"Email пользователя {userId} изменен на {dto.Email}. Требуется подтверждение.");
            }

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Профиль пользователя {userId} успешно обновлен");

            var message = user.EmailConfirmed
                ? "Профиль успешно обновлен"
                : "Профиль обновлен. На новый email отправлено письмо для подтверждения";

            return (true, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при обновлении профиля пользователя {userId}");
            return (false, "Произошла ошибка при обновлении профиля");
        }
    }

    /// <summary>
    /// Изменить пароль пользователя
    /// </summary>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "Пользователь не найден");
            }

            // Проверка текущего пароля
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning($"Неудачная попытка смены пароля для пользователя {userId}: неверный текущий пароль");
                return (false, "Текущий пароль указан неверно");
            }

            // Установка нового пароля
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, 12);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Пароль пользователя {userId} успешно изменен");

            return (true, "Пароль успешно изменен");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при изменении пароля пользователя {userId}");
            return (false, "Произошла ошибка при изменении пароля");
        }
    }

    /// <summary>
    /// Получить настройки пользователя
    /// </summary>
    public async Task<UserSettings?> GetUserSettingsAsync(int userId)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        // Если настроек нет, создаем с дефолтными значениями
        if (settings == null)
        {
            settings = new UserSettings
            {
                UserId = userId,
                EmailNotificationsEnabled = true,
                BotEventsNotifications = true,
                PaymentNotifications = true,
                NewsletterNotifications = true,
                SecurityNotifications = true,
                Language = "ru",
                TimeZone = "Europe/Moscow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Созданы настройки по умолчанию для пользователя {userId}");
        }

        return settings;
    }

    /// <summary>
    /// Обновить настройки пользователя
    /// </summary>
    public async Task<bool> UpdateSettingsAsync(int userId, UpdateSettingsDto dto)
    {
        try
        {
            var settings = await GetUserSettingsAsync(userId);
            if (settings == null)
            {
                return false;
            }

            settings.EmailNotificationsEnabled = dto.EmailNotificationsEnabled;
            settings.BotEventsNotifications = dto.BotEventsNotifications;
            settings.PaymentNotifications = dto.PaymentNotifications;
            settings.NewsletterNotifications = dto.NewsletterNotifications;
            settings.SecurityNotifications = dto.SecurityNotifications;
            settings.Language = dto.Language;
            settings.TimeZone = dto.TimeZone;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Настройки пользователя {userId} успешно обновлены");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при обновлении настроек пользователя {userId}");
            return false;
        }
    }

    /// <summary>
    /// Получить список активных сессий пользователя
    /// </summary>
    public async Task<List<UserSessionDto>> GetActiveSessionsAsync(int userId, string? currentSessionId = null)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();

        return sessions.Select(s => new UserSessionDto
        {
            Id = s.Id,
            SessionId = s.SessionId,
            IpAddress = s.IpAddress,
            UserAgent = s.UserAgent,
            LastActivityAt = s.LastActivityAt,
            ExpiresAt = s.ExpiresAt,
            IsCurrent = s.SessionId == currentSessionId
        }).ToList();
    }

    /// <summary>
    /// Завершить все сессии пользователя кроме текущей
    /// </summary>
    public async Task<int> TerminateAllSessionsAsync(int userId, string? exceptSessionId = null)
    {
        try
        {
            var sessionsToTerminate = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            if (!string.IsNullOrEmpty(exceptSessionId))
            {
                sessionsToTerminate = sessionsToTerminate
                    .Where(s => s.SessionId != exceptSessionId)
                    .ToList();
            }

            foreach (var session in sessionsToTerminate)
            {
                session.IsActive = false;
                session.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Завершено {sessionsToTerminate.Count} сессий для пользователя {userId}");

            return sessionsToTerminate.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при завершении сессий пользователя {userId}");
            return 0;
        }
    }

    /// <summary>
    /// Удалить аккаунт пользователя
    /// </summary>
    public async Task<bool> DeleteAccountAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Помечаем пользователя как неактивного вместо полного удаления
            // Это позволит сохранить историю и связанные данные
            user.IsActive = false;
            user.IsBlocked = true;
            user.BlockedAt = DateTime.UtcNow;
            user.BlockedReason = "Аккаунт удален по запросу пользователя";
            user.UpdatedAt = DateTime.UtcNow;

            // Завершаем все активные сессии
            await TerminateAllSessionsAsync(userId);

            await _context.SaveChangesAsync();

            _logger.LogWarning($"Аккаунт пользователя {userId} ({user.Email}) был удален по запросу пользователя");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при удалении аккаунта пользователя {userId}");
            return false;
        }
    }

    /// <summary>
    /// Создать или обновить сессию пользователя
    /// </summary>
    public async Task CreateOrUpdateSessionAsync(int userId, string sessionId, string ipAddress, string userAgent)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
            {
                // Создаем новую сессию
                session = new UserSession
                {
                    UserId = userId,
                    SessionId = sessionId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    LastActivityAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30), // Срок действия 30 дней
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserSessions.Add(session);
                _logger.LogInformation($"Создана новая сессия {sessionId} для пользователя {userId}");
            }
            else
            {
                // Обновляем существующую сессию
                session.LastActivityAt = DateTime.UtcNow;
                session.IpAddress = ipAddress;
                session.UserAgent = userAgent;
                session.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при создании/обновлении сессии для пользователя {userId}");
        }
    }

    /// <summary>
    /// Очистить устаревшие сессии
    /// </summary>
    public async Task CleanupExpiredSessionsAsync()
    {
        try
        {
            var expiredSessions = await _context.UserSessions
                .Where(s => s.ExpiresAt < DateTime.UtcNow && s.IsActive)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                session.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Очищено {expiredSessions.Count} устаревших сессий");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке устаревших сессий");
        }
    }
}
