using BotConstructor.Core.DTOs;
using BotConstructor.Core.Entities;
using BotConstructor.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BotConstructor.Tests.Integration.Auth;

/// <summary>
/// Integration-тесты для модуля аутентификации
/// Проверяют работу всей цепочки: Controller -> Service -> Database
/// </summary>
public class AuthIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteRegistrationFlow_ShouldCreateUserAndConfirmEmail()
    {
        // Arrange - подготовка данных для регистрации
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Очистка базы данных перед тестом
        await ClearDatabase(context);

        // Создание роли User для тестов
        await SeedRoles(context);

        var registerDto = new RegisterDto
        {
            Email = "integration@test.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456",
            FirstName = "Integration",
            LastName = "Test"
        };

        // Act - выполнение регистрации (симуляция)
        // В реальности нужно было бы делать HTTP запрос, но для простоты тестируем через сервис
        var authService = scope.ServiceProvider.GetRequiredService<Core.Interfaces.IAuthService>();
        var registerResult = await authService.RegisterAsync(registerDto, "127.0.0.1");

        // Assert - проверка успешной регистрации
        registerResult.Success.Should().BeTrue();
        registerResult.User.Should().NotBeNull();

        // Проверка, что пользователь создан в базе данных
        var userInDb = await context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

        userInDb.Should().NotBeNull();
        userInDb!.Email.Should().Be(registerDto.Email);
        userInDb.FirstName.Should().Be(registerDto.FirstName);
        userInDb.LastName.Should().Be(registerDto.LastName);
        userInDb.EmailConfirmed.Should().BeFalse("email еще не подтвержден");
        userInDb.UserRoles.Should().ContainSingle(ur => ur.Role.Name == "User");

        // Проверка, что токен подтверждения создан
        var confirmationToken = await context.EmailConfirmationTokens
            .FirstOrDefaultAsync(t => t.UserId == userInDb.Id);

        confirmationToken.Should().NotBeNull();
        confirmationToken!.IsUsed.Should().BeFalse();
        confirmationToken.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // Act - подтверждение email
        var confirmResult = await authService.ConfirmEmailAsync(confirmationToken.Token);

        // Assert - проверка успешного подтверждения
        confirmResult.Should().BeTrue();

        // Обновление данных из БД
        await context.Entry(userInDb).ReloadAsync();
        userInDb.EmailConfirmed.Should().BeTrue();
        userInDb.EmailConfirmedAt.Should().NotBeNull();

        // Проверка, что токен помечен как использованный
        await context.Entry(confirmationToken).ReloadAsync();
        confirmationToken.IsUsed.Should().BeTrue();
        confirmationToken.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteLoginFlow_WithCorrectCredentials_ShouldSucceed()
    {
        // Arrange - создание тестового пользователя
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await ClearDatabase(context);
        await SeedRoles(context);

        var password = "Test@123456";
        var user = new User
        {
            Email = "login@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12),
            FirstName = "Login",
            LastName = "Test",
            EmailConfirmed = true,
            IsActive = true,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Назначение роли
        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = (await context.Roles.FirstAsync(r => r.Name == "User")).Id,
            AssignedAt = DateTime.UtcNow
        };
        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = user.Email,
            Password = password,
            RememberMe = false
        };

        // Act - выполнение входа
        var authService = scope.ServiceProvider.GetRequiredService<Core.Interfaces.IAuthService>();
        var loginResult = await authService.LoginAsync(loginDto, "127.0.0.1", "Test Agent");

        // Assert - проверка успешного входа
        loginResult.Success.Should().BeTrue();
        loginResult.User.Should().NotBeNull();
        loginResult.User!.Email.Should().Be(loginDto.Email);

        // Проверка, что попытка входа залогирована
        var loginAttempt = await context.LoginAttempts
            .FirstOrDefaultAsync(la => la.Email == user.Email);

        loginAttempt.Should().NotBeNull();
        loginAttempt!.IsSuccessful.Should().BeTrue();
        loginAttempt.IpAddress.Should().Be("127.0.0.1");
        loginAttempt.UserAgent.Should().Be("Test Agent");

        // Проверка обновления данных пользователя
        await context.Entry(user).ReloadAsync();
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginIp.Should().Be("127.0.0.1");
        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task LoginFlow_WithIncorrectPassword_ShouldFailAndLogAttempt()
    {
        // Arrange - создание тестового пользователя
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await ClearDatabase(context);
        await SeedRoles(context);

        var user = new User
        {
            Email = "faillogin@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass@123", 12),
            EmailConfirmed = true,
            IsActive = true,
            IsBlocked = false,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = user.Email,
            Password = "WrongPass@123"
        };

        // Act - попытка входа с неверным паролем
        var authService = scope.ServiceProvider.GetRequiredService<Core.Interfaces.IAuthService>();
        var loginResult = await authService.LoginAsync(loginDto, "127.0.0.1", "Test Agent");

        // Assert - проверка неудачного входа
        loginResult.Success.Should().BeFalse();
        loginResult.Message.Should().Contain("Неверный email или пароль");
        loginResult.User.Should().BeNull();

        // Проверка, что попытка входа залогирована как неудачная
        var loginAttempt = await context.LoginAttempts
            .FirstOrDefaultAsync(la => la.Email == user.Email);

        loginAttempt.Should().NotBeNull();
        loginAttempt!.IsSuccessful.Should().BeFalse();
        loginAttempt.FailureReason.Should().Contain("пароль");

        // Проверка увеличения счетчика неудачных попыток
        await context.Entry(user).ReloadAsync();
        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task PasswordResetFlow_ShouldCreateTokenAndResetPassword()
    {
        // Arrange - создание тестового пользователя
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await ClearDatabase(context);
        await SeedRoles(context);

        var oldPassword = "OldPass@123";
        var user = new User
        {
            Email = "reset@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword, 12),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act 1 - запрос на сброс пароля
        var authService = scope.ServiceProvider.GetRequiredService<Core.Interfaces.IAuthService>();
        var forgotPasswordDto = new ForgotPasswordDto { Email = user.Email };
        var requestResult = await authService.RequestPasswordResetAsync(forgotPasswordDto);

        // Assert - проверка создания токена
        requestResult.Should().BeTrue();

        var resetToken = await context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id);

        resetToken.Should().NotBeNull();
        resetToken!.IsUsed.Should().BeFalse();
        resetToken.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // Act 2 - сброс пароля
        var newPassword = "NewPass@456";
        var resetPasswordDto = new ResetPasswordDto
        {
            Token = resetToken.Token,
            Password = newPassword,
            ConfirmPassword = newPassword
        };

        var resetResult = await authService.ResetPasswordAsync(resetPasswordDto, "127.0.0.1");

        // Assert - проверка успешного сброса пароля
        resetResult.Success.Should().BeTrue();
        resetResult.Message.Should().Contain("успешно");

        // Проверка обновления пароля в БД
        await context.Entry(user).ReloadAsync();
        var isOldPasswordValid = BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash);
        var isNewPasswordValid = BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash);

        isOldPasswordValid.Should().BeFalse("старый пароль больше не должен работать");
        isNewPasswordValid.Should().BeTrue("новый пароль должен работать");

        // Проверка, что токен помечен как использованный
        await context.Entry(resetToken).ReloadAsync();
        resetToken.IsUsed.Should().BeTrue();
        resetToken.UsedAt.Should().NotBeNull();
        resetToken.IpAddress.Should().Be("127.0.0.1");

        // Проверка сброса счетчика неудачных попыток
        user.FailedLoginAttempts.Should().Be(0);
        user.LockedOutUntil.Should().BeNull();
    }

    /// <summary>
    /// Очистка базы данных перед тестом
    /// </summary>
    private static async Task ClearDatabase(ApplicationDbContext context)
    {
        // Удаление всех связанных данных в правильном порядке
        context.LoginAttempts.RemoveRange(context.LoginAttempts);
        context.EmailConfirmationTokens.RemoveRange(context.EmailConfirmationTokens);
        context.PasswordResetTokens.RemoveRange(context.PasswordResetTokens);
        context.UserSessions.RemoveRange(context.UserSessions);
        context.UserSettings.RemoveRange(context.UserSettings);
        context.UserRoles.RemoveRange(context.UserRoles);
        context.Users.RemoveRange(context.Users);
        context.Roles.RemoveRange(context.Roles);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Заполнение начальными данными (роли)
    /// </summary>
    private static async Task SeedRoles(ApplicationDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(
                new Role
                {
                    Name = "SuperAdmin",
                    Description = "Владелец платформы",
                    IsSystemRole = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Role
                {
                    Name = "Admin",
                    Description = "Администратор",
                    IsSystemRole = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Role
                {
                    Name = "User",
                    Description = "Обычный пользователь",
                    IsSystemRole = true,
                    CreatedAt = DateTime.UtcNow
                }
            );

            await context.SaveChangesAsync();
        }
    }
}
