namespace BotConstructor.Core.DTOs;

/// <summary>
/// DTO для отображения активной сессии пользователя
/// </summary>
public class UserSessionDto
{
    /// <summary>
    /// ID сессии
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Уникальный идентификатор сессии
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// IP адрес
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Браузер и устройство
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Последняя активность
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Дата истечения
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Текущая сессия?
    /// </summary>
    public bool IsCurrent { get; set; }
}
