namespace BotConstructor.Core.Entities;

/// <summary>
/// Настройки пользователя (уведомления и прочие параметры)
/// </summary>
public class UserSettings : BaseEntity
{
    /// <summary>
    /// ID пользователя
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Получать email уведомления
    /// </summary>
    public bool EmailNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Уведомления о событиях ботов
    /// </summary>
    public bool BotEventsNotifications { get; set; } = true;

    /// <summary>
    /// Уведомления о платежах
    /// </summary>
    public bool PaymentNotifications { get; set; } = true;

    /// <summary>
    /// Уведомления о новостях платформы
    /// </summary>
    public bool NewsletterNotifications { get; set; } = true;

    /// <summary>
    /// Уведомления о безопасности (вход, смена пароля)
    /// </summary>
    public bool SecurityNotifications { get; set; } = true;

    /// <summary>
    /// Язык интерфейса
    /// </summary>
    public string Language { get; set; } = "ru";

    /// <summary>
    /// Часовой пояс
    /// </summary>
    public string TimeZone { get; set; } = "Europe/Moscow";

    // Navigation properties
    /// <summary>
    /// Навигационное свойство к пользователю
    /// </summary>
    public User User { get; set; } = null!;
}
