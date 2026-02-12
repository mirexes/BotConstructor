using BotConstructor.Core.DTOs;
using BotConstructor.Core.Entities;

namespace BotConstructor.Core.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterDto dto, string ipAddress);
    Task<(bool Success, string Message, User? User)> LoginAsync(LoginDto dto, string ipAddress, string? userAgent);
    Task<bool> ConfirmEmailAsync(string token);
    Task<bool> RequestPasswordResetAsync(ForgotPasswordDto dto);
    Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto, string ipAddress);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> IsEmailConfirmedAsync(string email);
}
