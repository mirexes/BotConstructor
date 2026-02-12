namespace BotConstructor.Core.Entities;

/// <summary>
/// Сущность для хранения активных сессий пользователей
/// </summary>
public class UserSession : BaseEntity
{
    /// <summary>
    /// ID пользователя
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Уникальный идентификатор сессии
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// IP адрес клиента
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User Agent браузера
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Дата и время последней активности
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Дата и время истечения сессии
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Активна ли сессия
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    /// <summary>
    /// Навигационное свойство к пользователю
    /// </summary>
    public User User { get; set; } = null!;
}
