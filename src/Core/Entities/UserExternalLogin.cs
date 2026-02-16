namespace BotConstructor.Core.Entities;

public class UserExternalLogin : BaseEntity
{
    public int UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
    public string? ProviderDisplayName { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
