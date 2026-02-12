using System.ComponentModel.DataAnnotations;

namespace BotConstructor.Core.DTOs;

/// <summary>
/// DTO для смены пароля
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// Текущий пароль
    /// </summary>
    [Required(ErrorMessage = "Текущий пароль обязателен")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Новый пароль
    /// </summary>
    [Required(ErrorMessage = "Новый пароль обязателен")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен быть от 8 до 100 символов")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Подтверждение нового пароля
    /// </summary>
    [Required(ErrorMessage = "Подтверждение пароля обязательно")]
    [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
