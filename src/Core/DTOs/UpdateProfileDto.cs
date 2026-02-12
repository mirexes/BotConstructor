using System.ComponentModel.DataAnnotations;

namespace BotConstructor.Core.DTOs;

/// <summary>
/// DTO для обновления профиля пользователя
/// </summary>
public class UpdateProfileDto
{
    /// <summary>
    /// Имя пользователя
    /// </summary>
    [Required(ErrorMessage = "Имя обязательно для заполнения")]
    [StringLength(100, ErrorMessage = "Имя не может быть длиннее 100 символов")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Фамилия пользователя
    /// </summary>
    [StringLength(100, ErrorMessage = "Фамилия не может быть длиннее 100 символов")]
    public string? LastName { get; set; }

    /// <summary>
    /// Email пользователя
    /// </summary>
    [Required(ErrorMessage = "Email обязателен для заполнения")]
    [EmailAddress(ErrorMessage = "Некорректный email адрес")]
    [StringLength(255, ErrorMessage = "Email не может быть длиннее 255 символов")]
    public string Email { get; set; } = string.Empty;
}
