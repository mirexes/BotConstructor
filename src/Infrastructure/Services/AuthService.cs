using BotConstructor.Core.DTOs;
using BotConstructor.Core.Entities;
using BotConstructor.Core.Interfaces;
using BotConstructor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace BotConstructor.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private const int MaxFailedAttempts = 5;
    private const int LockoutDurationMinutes = 15;
    private const int BcryptWorkFactor = 12;

    public AuthService(
        ApplicationDbContext context,
        IUserRepository userRepository,
        IEmailService emailService)
    {
        _context = context;
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterDto dto, string ipAddress)
    {
        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            return (false, "Пользователь с таким email уже существует", null);
        }

        // Hash password with bcrypt (cost factor 12)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BcryptWorkFactor);

        // Create user
        var user = new User
        {
            Email = dto.Email,
            PasswordHash = passwordHash,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            EmailConfirmed = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Add user to database
        await _userRepository.AddAsync(user);

        // Assign default "User" role
        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (userRole != null)
        {
            await _context.UserRoles.AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = userRole.Id,
                AssignedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        // Generate email confirmation token
        var confirmationToken = GenerateToken();
        await _context.EmailConfirmationTokens.AddAsync(new EmailConfirmationToken
        {
            UserId = user.Id,
            Token = confirmationToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Send confirmation email
        var confirmationLink = $"https://yourdomain.com/auth/confirm-email?token={confirmationToken}";
        await _emailService.SendEmailConfirmationAsync(user.Email, confirmationToken, confirmationLink);

        return (true, "Регистрация успешна. Проверьте вашу почту для подтверждения email", user);
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(
        LoginDto dto,
        string ipAddress,
        string? userAgent)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        // Log the attempt
        var loginAttempt = new LoginAttempt
        {
            Email = dto.Email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            UserId = user?.Id,
            CreatedAt = DateTime.UtcNow
        };

        if (user == null)
        {
            loginAttempt.IsSuccessful = false;
            loginAttempt.FailureReason = "Пользователь не найден";
            await _context.LoginAttempts.AddAsync(loginAttempt);
            await _context.SaveChangesAsync();

            return (false, "Неверный email или пароль", null);
        }

        // Check if user is blocked
        if (user.IsBlocked)
        {
            loginAttempt.IsSuccessful = false;
            loginAttempt.FailureReason = "Аккаунт заблокирован";
            await _context.LoginAttempts.AddAsync(loginAttempt);
            await _context.SaveChangesAsync();

            return (false, $"Ваш аккаунт заблокирован. Причина: {user.BlockedReason}", null);
        }

        // Check if user is locked out
        if (user.LockedOutUntil.HasValue && user.LockedOutUntil.Value > DateTime.UtcNow)
        {
            loginAttempt.IsSuccessful = false;
            loginAttempt.FailureReason = "Аккаунт временно заблокирован";
            await _context.LoginAttempts.AddAsync(loginAttempt);
            await _context.SaveChangesAsync();

            var remainingMinutes = (user.LockedOutUntil.Value - DateTime.UtcNow).Minutes;
            return (false, $"Слишком много неудачных попыток входа. Попробуйте через {remainingMinutes} минут", null);
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockedOutUntil = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                loginAttempt.FailureReason = $"Превышено количество попыток входа. Аккаунт заблокирован на {LockoutDurationMinutes} минут";
            }
            else
            {
                loginAttempt.FailureReason = "Неверный пароль";
            }

            loginAttempt.IsSuccessful = false;
            await _context.LoginAttempts.AddAsync(loginAttempt);
            await _userRepository.UpdateAsync(user);

            return (false, "Неверный email или пароль", null);
        }

        // Check if email is confirmed
        if (!user.EmailConfirmed)
        {
            loginAttempt.IsSuccessful = false;
            loginAttempt.FailureReason = "Email не подтвержден";
            await _context.LoginAttempts.AddAsync(loginAttempt);
            await _context.SaveChangesAsync();

            return (false, "Пожалуйста, подтвердите ваш email перед входом в систему", null);
        }

        // Successful login
        user.FailedLoginAttempts = 0;
        user.LockedOutUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ipAddress;

        loginAttempt.IsSuccessful = true;

        await _context.LoginAttempts.AddAsync(loginAttempt);
        await _userRepository.UpdateAsync(user);

        return (true, "Вход выполнен успешно", user);
    }

    public async Task<bool> ConfirmEmailAsync(string token)
    {
        var confirmationToken = await _context.EmailConfirmationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed);

        if (confirmationToken == null)
        {
            return false;
        }

        if (confirmationToken.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        var user = confirmationToken.User;
        user.EmailConfirmed = true;
        user.EmailConfirmedAt = DateTime.UtcNow;

        confirmationToken.IsUsed = true;
        confirmationToken.UsedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        await _context.SaveChangesAsync();

        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName ?? "пользователь");

        return true;
    }

    public async Task<bool> RequestPasswordResetAsync(ForgotPasswordDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        // Always return true to prevent email enumeration
        if (user == null)
        {
            return true;
        }

        // Generate reset token
        var resetToken = GenerateToken();
        await _context.PasswordResetTokens.AddAsync(new PasswordResetToken
        {
            UserId = user.Id,
            Token = resetToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1), // Token valid for 1 hour
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Send reset email
        var resetLink = $"https://yourdomain.com/auth/reset-password?token={resetToken}";
        await _emailService.SendPasswordResetAsync(user.Email, resetToken, resetLink);

        return true;
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto, string ipAddress)
    {
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == dto.Token && !t.IsUsed);

        if (resetToken == null)
        {
            return (false, "Неверный или истекший токен");
        }

        if (resetToken.ExpiresAt < DateTime.UtcNow)
        {
            return (false, "Токен истек. Пожалуйста, запросите новый");
        }

        var user = resetToken.User;

        // Hash new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BcryptWorkFactor);
        user.FailedLoginAttempts = 0;
        user.LockedOutUntil = null;

        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;
        resetToken.IpAddress = ipAddress;

        await _userRepository.UpdateAsync(user);
        await _context.SaveChangesAsync();

        return (true, "Пароль успешно изменен");
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userRepository.GetByEmailAsync(email);
    }

    public async Task<bool> IsEmailConfirmedAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        return user?.EmailConfirmed ?? false;
    }

    private static string GenerateToken()
    {
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }
}
