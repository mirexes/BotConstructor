using BotConstructor.Core.DTOs;
using BotConstructor.Core.Entities;

namespace BotConstructor.Core.Interfaces;

/// <summary>
/// Интерфейс сервиса для работы с профилем пользователя
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Получить профиль пользователя по ID
    /// </summary>
    Task<User?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Обновить профиль пользователя
    /// </summary>
    Task<(bool Success, string Message)> UpdateProfileAsync(int userId, UpdateProfileDto dto);

    /// <summary>
    /// Изменить пароль пользователя
    /// </summary>
    Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordDto dto);

    /// <summary>
    /// Получить настройки пользователя
    /// </summary>
    Task<UserSettings?> GetUserSettingsAsync(int userId);

    /// <summary>
    /// Обновить настройки пользователя
    /// </summary>
    Task<bool> UpdateSettingsAsync(int userId, UpdateSettingsDto dto);

    /// <summary>
    /// Получить список активных сессий пользователя
    /// </summary>
    Task<List<UserSessionDto>> GetActiveSessionsAsync(int userId, string? currentSessionId = null);

    /// <summary>
    /// Завершить все сессии пользователя кроме текущей
    /// </summary>
    Task<int> TerminateAllSessionsAsync(int userId, string? exceptSessionId = null);

    /// <summary>
    /// Удалить аккаунт пользователя
    /// </summary>
    Task<bool> DeleteAccountAsync(int userId);

    /// <summary>
    /// Создать или обновить сессию пользователя
    /// </summary>
    Task CreateOrUpdateSessionAsync(int userId, string sessionId, string ipAddress, string userAgent);

    /// <summary>
    /// Очистить устаревшие сессии
    /// </summary>
    Task CleanupExpiredSessionsAsync();
}
