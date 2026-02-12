using System.ComponentModel.DataAnnotations;

namespace BotConstructor.Core.DTOs;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Неверный формат email")]
    public string Email { get; set; } = string.Empty;
}
