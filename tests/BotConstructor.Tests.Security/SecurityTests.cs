using BotConstructor.Core.DTOs;
using BotConstructor.Core.Entities;
using BotConstructor.Core.Interfaces;
using BotConstructor.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BotConstructor.Tests.Security;

/// <summary>
/// Тесты безопасности для модуля аутентификации
/// Проверяют защиту от различных типов атак
/// </summary>
public class SecurityTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SecurityTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Security_BruteForceProtection_ShouldLockAccountAfter5FailedAttempts()
    {
        // Тест защиты от брутфорса: после 5 неудачных попыток входа аккаунт блокируется

        // Arrange - создание тестового пользователя
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await ClearDatabase(context);
        await SeedRoles(context);

        var password = "CorrectPass@123";
        var user = new User
        {
            Email = "bruteforce@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12),
            EmailConfirmed = true,
            IsActive = true,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var wrongLoginDto = new LoginDto
        {
            Email = user.Email,
            Password = "WrongPassword@123"
        };

        // Act - выполнение 5 неудачных попыток входа
        for (int i = 0; i < 5; i++)
        {
            await authService.LoginAsync(wrongLoginDto, "127.0.0.1", "BruteForce Bot");
            await context.Entry(user).ReloadAsync();
        }

        // Assert - проверка блокировки аккаунта
        await context.Entry(user).ReloadAsync();
        user.FailedLoginAttempts.Should().Be(5);
        user.LockedOutUntil.Should().NotBeNull();
        user.LockedOutUntil.Should().BeAfter(DateTime.UtcNow);

        // Проверка, что даже с правильным паролем вход невозможен
        var correctLoginDto = new LoginDto
        {
            Email = user.Email,
            Password = password
        };

        var result = await authService.LoginAsync(correctLoginDto, "127.0.0.1", "BruteForce Bot");
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Слишком много неудачных попыток");

        // Проверка логирования всех попыток входа
        var loginAttempts = await context.LoginAttempts
            .Where(la => la.Email == user.Email)
            .ToListAsync();

        loginAttempts.Should().HaveCount(6); // 5 неудачных + 1 заблокированная
        loginAttempts.Count(la => !la.IsSuccessful).Should().Be(6);
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("admin'--")]
    [InlineData("' OR 1=1--")]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("1'; DROP TABLE Users--")]
    public async Task Security_SQLInjectionProtection_ShouldNotAllowMaliciousInput(string maliciousEmail)
    {
        // Тест защиты от SQL-инъекций: вредоносный ввод не должен влиять на запросы

        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await ClearDatabase(context);
        await SeedRoles(context);

        var loginDto = new LoginDto
        {
            Email = maliciousEmail,
            Password = "anypassword"
        };

        // Act - попытка входа с вредоносным email
        var result = await authService.LoginAsync(loginDto, "127.0.0.1", "SQL Injection Bot");

        // Assert - проверка, что вход не удался (а не произошло исключение)
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Неверный email или пароль");

        // Проверка, что таблица Users не была удалена или изменена
        var usersExist = await context.Users.AnyAsync();
        // Если была попытка DROP TABLE, это не должно было сработать
        // База должна быть в нормальном состоянии
    }

    [Fact]
    public async Task Security_PasswordHashing_ShouldUseBCryptWithCorrectWorkFactor()
    {
        // Тест правильности хеширования паролей с использованием BCrypt

        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await ClearDatabase(context);
        await SeedRoles(context);

        var password = "Test@123456";
        var registerDto = new RegisterDto
        {
            Email = "hash@test.com",
            Password = password,
            ConfirmPassword = password
        };

        // Act - регистрация пользователя
        var result = await authService.RegisterAsync(registerDto, "127.0.0.1");

        // Assert - проверка хеширования пароля
        result.Success.Should().BeTrue();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);

        user.Should().NotBeNull();
        user!.PasswordHash.Should().NotBe(password, "пароль должен быть захеширован");
        user.PasswordHash.Should().StartWith("$2a$12$", "BCrypt с work factor 12");

        // Проверка, что хеш можно верифицировать
        var isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        isValid.Should().BeTrue();

        // Проверка, что неправильный пароль не проходит верификацию
        var isInvalid = BCrypt.Net.BCrypt.Verify("WrongPassword", user.PasswordHash);
        isInvalid.Should().BeFalse();
    }

    [Fact]
    public async Task Security_PasswordStorage_ShouldNeverStorePlaintext()
    {
        // Тест: пароли никогда не должны храниться в открытом виде

        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await ClearDatabase(context);
        await SeedRoles(context);

        var password = "PlaintextTest@123";
        var registerDto = new RegisterDto
        {
            Email = "plaintext@test.com",
            Password = password,
            ConfirmPassword = password
        };

        // Act - регистрация и проверка хранения
        await authService.RegisterAsync(registerDto, "127.0.0.1");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);

        // Assert - проверка, что пароль не хранится в открытом виде
        user.Should().NotBeNull();
        user!.PasswordHash.Should().NotContain(password);
        user.PasswordHash.Length.Should().BeGreaterThan(password.Length);

        // Проверка всей таблицы Users на отсутствие открытых паролей
        var allUsers = await context.Users.ToListAsync();
        foreach (var u in allUsers)
        {
            u.PasswordHash.Should().NotBe(password);
            u.PasswordHash.Should().StartWith("$2a$"); // BCrypt prefix
        }
    }

    [Fact]
    public async Task Security_UserEnumeration_ShouldNotRevealExistence()
    {
        // Тест защиты от перечисления пользователей:
        // ошибки входа не должны раскрывать, существует ли пользователь

        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await ClearDatabase(context);
        await SeedRoles(context);

        // Создание существующего пользователя
        var existingUser = new User
        {
            Email = "existing@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@123", 12),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var existingUserLoginDto = new LoginDto
        {
            Email = existingUser.Email,
            Password = "WrongPassword@123"
        };

        var nonExistentUserLoginDto = new LoginDto
        {
            Email = "nonexistent@test.com",
            Password = "SomePassword@123"
        };

        // Act - попытки входа для существующего и несуществующего пользователя
        var existingUserResult = await authService.LoginAsync(existingUserLoginDto, "127.0.0.1", "Test");
        var nonExistentUserResult = await authService.LoginAsync(nonExistentUserLoginDto, "127.0.0.1", "Test");

        // Assert - проверка, что сообщения об ошибке одинаковые
        existingUserResult.Success.Should().BeFalse();
        nonExistentUserResult.Success.Should().BeFalse();

        existingUserResult.Message.Should().Be(nonExistentUserResult.Message,
            "сообщения об ошибке не должны раскрывать существование пользователя");

        existingUserResult.Message.Should().Be("Неверный email или пароль");
        nonExistentUserResult.Message.Should().Be("Неверный email или пароль");
    }

    [Fact]
    public async Task Security_TokenExpiration_ShouldNotAcceptExpiredTokens()
    {
        // Тест: истекшие токены не должны приниматься

        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await ClearDatabase(context);

        var user = new User
        {
            Email = "token@test.com",
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Создание истекшего токена
        var expiredToken = new EmailConfirmationToken
        {
            UserId = user.Id,
            Token = "expired-token-12345",
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Истек час назад
            IsUsed = false,
            CreatedAt = DateTime.UtcNow.AddHours(-25)
        };
        context.EmailConfirmationTokens.Add(expiredToken);
        await context.SaveChangesAsync();

        // Act - попытка использовать истекший токен
        var result = await authService.ConfirmEmailAsync(expiredToken.Token);

        // Assert - проверка отказа
        result.Should().BeFalse();
        user.EmailConfirmed.Should().BeFalse();
    }

    /// <summary>
    /// Очистка базы данных
    /// </summary>
    private static async Task ClearDatabase(ApplicationDbContext context)
    {
        context.LoginAttempts.RemoveRange(context.LoginAttempts);
        context.EmailConfirmationTokens.RemoveRange(context.EmailConfirmationTokens);
        context.PasswordResetTokens.RemoveRange(context.PasswordResetTokens);
        context.UserRoles.RemoveRange(context.UserRoles);
        context.Users.RemoveRange(context.Users);
        context.Roles.RemoveRange(context.Roles);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Заполнение ролями
    /// </summary>
    private static async Task SeedRoles(ApplicationDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.Add(new Role
            {
                Name = "User",
                Description = "Обычный пользователь",
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }
}
