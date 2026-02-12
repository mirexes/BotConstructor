using System.ComponentModel.DataAnnotations;

namespace BotConstructor.Web.Models;

public class BlockUserViewModel
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Укажите причину блокировки")]
    [StringLength(500, ErrorMessage = "Причина не должна превышать 500 символов")]
    public string Reason { get; set; } = string.Empty;
}
