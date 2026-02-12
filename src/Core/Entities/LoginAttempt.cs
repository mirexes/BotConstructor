namespace BotConstructor.Core.Entities;

public class LoginAttempt : BaseEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }

    public string Email { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? FailureReason { get; set; }
}
