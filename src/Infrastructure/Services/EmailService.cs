using BotConstructor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BotConstructor.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendEmailConfirmationAsync(string email, string token, string confirmationLink)
    {
        var subject = "Подтверждение email - BotConstructor";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f4f4f4; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>BotConstructor Platform</h1>
        </div>
        <div class='content'>
            <h2>Подтверждение email</h2>
            <p>Спасибо за регистрацию на платформе BotConstructor!</p>
            <p>Для завершения регистрации и подтверждения вашего email адреса, пожалуйста, нажмите на кнопку ниже:</p>
            <a href='{confirmationLink}' class='button'>Подтвердить Email</a>
            <p>Или скопируйте и вставьте следующую ссылку в браузер:</p>
            <p><a href='{confirmationLink}'>{confirmationLink}</a></p>
            <p>Ссылка действительна в течение 24 часов.</p>
            <p>Если вы не регистрировались на нашей платформе, просто проигнорируйте это письмо.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2026 BotConstructor Platform. Все права защищены.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetAsync(string email, string token, string resetLink)
    {
        var subject = "Восстановление пароля - BotConstructor";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f4f4f4; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
        .warning {{ background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>BotConstructor Platform</h1>
        </div>
        <div class='content'>
            <h2>Восстановление пароля</h2>
            <p>Мы получили запрос на восстановление пароля для вашего аккаунта.</p>
            <p>Для сброса пароля нажмите на кнопку ниже:</p>
            <a href='{resetLink}' class='button'>Сбросить пароль</a>
            <p>Или скопируйте и вставьте следующую ссылку в браузер:</p>
            <p><a href='{resetLink}'>{resetLink}</a></p>
            <div class='warning'>
                <strong>Важно!</strong> Ссылка действительна в течение 1 часа.
            </div>
            <p>Если вы не запрашивали восстановление пароля, просто проигнорируйте это письмо. Ваш пароль останется без изменений.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2026 BotConstructor Platform. Все права защищены.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName)
    {
        var subject = "Добро пожаловать в BotConstructor!";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f4f4f4; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
        .feature {{ background-color: white; padding: 15px; margin: 10px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Добро пожаловать!</h1>
        </div>
        <div class='content'>
            <h2>Здравствуйте, {firstName}!</h2>
            <p>Спасибо за регистрацию на платформе BotConstructor! Ваш email успешно подтвержден.</p>
            <p>Теперь вы можете:</p>
            <div class='feature'>
                <strong>✅ Создавать ботов</strong><br>
                Используйте визуальный конструктор для создания ботов без программирования
            </div>
            <div class='feature'>
                <strong>🤖 Подключать мессенджеры</strong><br>
                Telegram, Max и другие популярные платформы
            </div>
            <div class='feature'>
                <strong>📊 Просматривать аналитику</strong><br>
                Отслеживайте эффективность ваших ботов
            </div>
            <a href='https://yourdomain.com/dashboard' class='button'>Перейти в личный кабинет</a>
            <p>Если у вас есть вопросы, обратитесь в нашу службу поддержки.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2026 BotConstructor Platform. Все права защищены.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, body);
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        // TODO: Implement actual SMTP sending in production
        // For now, just log to console for development

        _logger.LogInformation("=== EMAIL SENT ===");
        _logger.LogInformation($"To: {to}");
        _logger.LogInformation($"Subject: {subject}");
        _logger.LogInformation($"Body: {body}");
        _logger.LogInformation("==================");

        await Task.CompletedTask;
    }
}
