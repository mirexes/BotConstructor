namespace BotConstructor.Core.Interfaces;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string email, string token, string confirmationLink);
    Task SendPasswordResetAsync(string email, string token, string resetLink);
    Task SendWelcomeEmailAsync(string email, string firstName);
}
