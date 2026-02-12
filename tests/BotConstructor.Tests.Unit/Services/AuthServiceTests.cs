using BotConstructor.Core.DTOs;
using BotConstructor.Core.Entities;
using BotConstructor.Core.Interfaces;
using BotConstructor.Infrastructure.Data;
using BotConstructor.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BotConstructor.Tests.Unit.Services;

/// <summary>
/// Unit-тесты для сервиса аутентификации AuthService
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Настройка InMemory базы данных для тестов
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        // Создание mock-объектов для зависимостей
        _userRepositoryMock = new Mock<IUserRepository>();
        _emailServiceMock = new Mock<IEmailService>();

        // Создание экземпляра AuthService с mock-зависимостями
        _authService = new AuthService(
            _context,
            _userRepositoryMock.Object,
            _emailServiceMock.Object
        );

        // Инициализация тестовых данных (роль User)
        SeedTestData();
    }

    /// <summary>
    /// Заполнение тестовыми данными
    /// </summary>
    private void SeedTestData()
    {
        // Добавление роли "User" для тестов
        _context.Roles.Add(new Role
        {
            Id = 1,
            Name = "User",
            Description = "Обычный пользователь",
            IsSystemRole = true
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange - подготовка данных для регистрации
        var dto = new RegisterDto
        {
            Email = "newuser@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456",
            FirstName = "Иван",
            LastName = "Петров"
        };

        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(dto.Email))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _emailServiceMock
            .Setup(x => x.SendEmailConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act - выполнение регистрации
        var result = await _authService.RegisterAsync(dto, "127.0.0.1");

        // Assert - проверка успешности регистрации
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("успешна");
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(dto.Email);
        result.User.FirstName.Should().Be(dto.FirstName);
        result.User.LastName.Should().Be(dto.LastName);

        // Проверка, что метод AddAsync был вызван один раз
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);

        // Проверка, что email подтверждения был отправлен
        _emailServiceMock.Verify(
            x => x.SendEmailConfirmationAsync(
                dto.Email,
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnError()
    {
        // Arrange - подготовка данных с существующим email
        var dto = new RegisterDto
        {
            Email = "existing@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456"
        };

        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(dto.Email))
            .ReturnsAsync(true);

        // Act - попытка регистрации с существующим email
        var result = await _authService.RegisterAsync(dto, "127.0.0.1");

        // Assert - проверка, что регистрация не удалась
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("уже существует");
        result.User.Should().BeNull();

        // Проверка, что AddAsync не был вызван
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPassword()
    {
        // Arrange - подготовка данных
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456"
        };

        User? capturedUser = null;

        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(dto.Email))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => capturedUser = user)
            .Returns(Task.CompletedTask);

        // Act - регистрация пользователя
        await _authService.RegisterAsync(dto, "127.0.0.1");

        // Assert - проверка, что пароль был захеширован
        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().NotBe(dto.Password);
        capturedUser.PasswordHash.Should().NotBeNullOrEmpty();

        // Проверка, что хеш можно верифицировать
        var isValid = BCrypt.Net.BCrypt.Verify(dto.Password, capturedUser.PasswordHash);
        isValid.Should().BeTrue("пароль должен быть захеширован с помощью BCrypt");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange - подготовка существующего пользователя
        var password = "Test@123456";
        var user = new User
        {
            Id = 1,
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12),
            EmailConfirmed = true,
            IsActive = true,
            IsBlocked = false,
            UserRoles = new List<UserRole>()
        };

        var dto = new LoginDto
        {
            Email = user.Email,
            Password = password
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act - выполнение входа
        var result = await _authService.LoginAsync(dto, "127.0.0.1", "Mozilla/5.0");

        // Assert - проверка успешного входа
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("успешно");
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(dto.Email);

        // Проверка, что данные пользователя были обновлены
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldReturnError()
    {
        // Arrange - подготовка данных с несуществующим email
        var dto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "Test@123456"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync((User?)null);

        // Act - попытка входа с несуществующим email
        var result = await _authService.LoginAsync(dto, "127.0.0.1", "Mozilla/5.0");

        // Assert - проверка, что вход не удался
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Неверный email или пароль");
        result.User.Should().BeNull();

        // Проверка, что попытка входа была залогирована
        var loginAttempts = await _context.LoginAttempts.ToListAsync();
        loginAttempts.Should().ContainSingle();
        loginAttempts.First().IsSuccessful.Should().BeFalse();
        loginAttempts.First().FailureReason.Should().Contain("не найден");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldReturnError()
    {
        // Arrange - подготовка пользователя с неверным паролем
        var user = new User
        {
            Id = 1,
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@123", 12),
            EmailConfirmed = true,
            IsActive = true,
            IsBlocked = false,
            FailedLoginAttempts = 0
        };

        var dto = new LoginDto
        {
            Email = user.Email,
            Password = "WrongPassword@123"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act - попытка входа с неверным паролем
        var result = await _authService.LoginAsync(dto, "127.0.0.1", "Mozilla/5.0");

        // Assert - проверка, что вход не удался
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Неверный email или пароль");
        result.User.Should().BeNull();

        // Проверка, что счетчик неудачных попыток увеличился
        user.FailedLoginAttempts.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoginAsync_WithBlockedUser_ShouldReturnError()
    {
        // Arrange - подготовка заблокированного пользователя
        var user = new User
        {
            Id = 1,
            Email = "blocked@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123456", 12),
            EmailConfirmed = true,
            IsActive = true,
            IsBlocked = true,
            BlockedReason = "Нарушение правил"
        };

        var dto = new LoginDto
        {
            Email = user.Email,
            Password = "Test@123456"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(user);

        // Act - попытка входа заблокированного пользователя
        var result = await _authService.LoginAsync(dto, "127.0.0.1", "Mozilla/5.0");

        // Assert - проверка, что вход заблокирован
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("заблокирован");
        result.Message.Should().Contain(user.BlockedReason);
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithUnconfirmedEmail_ShouldReturnError()
    {
        // Arrange - подготовка пользователя с неподтвержденным email
        var password = "Test@123456";
        var user = new User
        {
            Id = 1,
            Email = "unconfirmed@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12),
            EmailConfirmed = false,
            IsActive = true,
            IsBlocked = false
        };

        var dto = new LoginDto
        {
            Email = user.Email,
            Password = password
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(user);

        // Act - попытка входа с неподтвержденным email
        var result = await _authService.LoginAsync(dto, "127.0.0.1", "Mozilla/5.0");

        // Assert - проверка, что вход не удался
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("подтвердите ваш email");
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithMultipleFailedAttempts_ShouldLockAccount()
    {
        // Arrange - подготовка пользователя с несколькими неудачными попытками
        var user = new User
        {
            Id = 1,
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@123", 12),
            EmailConfirmed = true,
            IsActive = true,
            IsBlocked = false,
            FailedLoginAttempts = 4 // Уже 4 неудачные попытки
        };

        var dto = new LoginDto
        {
            Email = user.Email,
            Password = "WrongPassword@123"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act - 5-я неудачная попытка входа (должна заблокировать аккаунт)
        var result = await _authService.LoginAsync(dto, "127.0.0.1", "Mozilla/5.0");

        // Assert - проверка, что аккаунт заблокирован
        result.Success.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(5);
        user.LockedOutUntil.Should().NotBeNull();
        user.LockedOutUntil.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithValidToken_ShouldConfirmEmail()
    {
        // Arrange - подготовка пользователя и токена подтверждения
        var user = new User
        {
            Id = 1,
            Email = "user@example.com",
            EmailConfirmed = false
        };

        var token = "valid-confirmation-token";
        var confirmationToken = new EmailConfirmationToken
        {
            Id = 1,
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            User = user
        };

        _context.EmailConfirmationTokens.Add(confirmationToken);
        await _context.SaveChangesAsync();

        _userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _emailServiceMock
            .Setup(x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act - подтверждение email
        var result = await _authService.ConfirmEmailAsync(token);

        // Assert - проверка успешного подтверждения
        result.Should().BeTrue();
        user.EmailConfirmed.Should().BeTrue();
        user.EmailConfirmedAt.Should().NotBeNull();
        confirmationToken.IsUsed.Should().BeTrue();

        // Проверка, что приветственное письмо было отправлено
        _emailServiceMock.Verify(
            x => x.SendWelcomeEmailAsync(user.Email, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange - подготовка несуществующего токена
        var token = "invalid-token";

        // Act - попытка подтверждения с невалидным токеном
        var result = await _authService.ConfirmEmailAsync(token);

        // Assert - проверка, что подтверждение не удалось
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange - подготовка истекшего токена
        var user = new User
        {
            Id = 1,
            Email = "user@example.com",
            EmailConfirmed = false
        };

        var token = "expired-token";
        var confirmationToken = new EmailConfirmationToken
        {
            Id = 1,
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Истекший токен
            IsUsed = false,
            User = user
        };

        _context.EmailConfirmationTokens.Add(confirmationToken);
        await _context.SaveChangesAsync();

        // Act - попытка подтверждения с истекшим токеном
        var result = await _authService.ConfirmEmailAsync(token);

        // Assert - проверка, что подтверждение не удалось
        result.Should().BeFalse();
        user.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithValidEmail_ShouldSendResetEmail()
    {
        // Arrange - подготовка существующего пользователя
        var user = new User
        {
            Id = 1,
            Email = "user@example.com",
            EmailConfirmed = true
        };

        var dto = new ForgotPasswordDto
        {
            Email = user.Email
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(user);

        _emailServiceMock
            .Setup(x => x.SendPasswordResetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act - запрос на сброс пароля
        var result = await _authService.RequestPasswordResetAsync(dto);

        // Assert - проверка успешности
        result.Should().BeTrue();

        // Проверка, что токен был создан
        var tokens = await _context.PasswordResetTokens.ToListAsync();
        tokens.Should().ContainSingle();
        tokens.First().UserId.Should().Be(user.Id);

        // Проверка, что email был отправлен
        _emailServiceMock.Verify(
            x => x.SendPasswordResetAsync(
                user.Email,
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithNonexistentEmail_ShouldReturnTrueToPreventEnumeration()
    {
        // Arrange - подготовка несуществующего email
        var dto = new ForgotPasswordDto
        {
            Email = "nonexistent@example.com"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync((User?)null);

        // Act - запрос на сброс пароля для несуществующего email
        var result = await _authService.RequestPasswordResetAsync(dto);

        // Assert - проверка, что всегда возвращается true (защита от перечисления пользователей)
        result.Should().BeTrue();

        // Проверка, что email НЕ был отправлен
        _emailServiceMock.Verify(
            x => x.SendPasswordResetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    public void Dispose()
    {
        // Освобождение ресурсов после каждого теста
        _context?.Dispose();
    }
}
