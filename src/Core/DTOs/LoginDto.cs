using System.ComponentModel.DataAnnotations;

namespace BotConstructor.Core.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Неверный формат email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}
