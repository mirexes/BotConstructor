using System.ComponentModel.DataAnnotations;

namespace BotConstructor.Web.Models;

public class AdminResetPasswordViewModel
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Введите новый пароль")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен содержать минимум 8 символов")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль")]
    [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
