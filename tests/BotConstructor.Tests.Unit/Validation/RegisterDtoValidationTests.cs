using System.ComponentModel.DataAnnotations;
using BotConstructor.Core.DTOs;
using FluentAssertions;
using Xunit;

namespace BotConstructor.Tests.Unit.Validation;

/// <summary>
/// Unit-тесты для валидации RegisterDto
/// </summary>
public class RegisterDtoValidationTests
{
    /// <summary>
    /// Создает контекст валидации для DTO
    /// </summary>
    private static ValidationContext CreateValidationContext(object dto)
    {
        return new ValidationContext(dto, null, null);
    }

    /// <summary>
    /// Выполняет валидацию объекта и возвращает список ошибок
    /// </summary>
    private static List<ValidationResult> ValidateDto(object dto)
    {
        var context = CreateValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, context, results, true);
        return results;
    }

    [Fact]
    public void RegisterDto_WithValidData_ShouldPassValidation()
    {
        // Arrange - подготовка валидных данных
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456",
            FirstName = "Иван",
            LastName = "Иванов"
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка результата
        results.Should().BeEmpty("все поля заполнены корректно");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RegisterDto_WithEmptyEmail_ShouldFailValidation(string email)
    {
        // Arrange - подготовка данных с пустым email
        var dto = new RegisterDto
        {
            Email = email!,
            Password = "Test@123456",
            ConfirmPassword = "Test@123456"
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка, что есть ошибка валидации
        results.Should().ContainSingle(r => r.MemberNames.Contains("Email"));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test")]
    public void RegisterDto_WithInvalidEmailFormat_ShouldFailValidation(string email)
    {
        // Arrange - подготовка данных с невалидным форматом email
        var dto = new RegisterDto
        {
            Email = email,
            Password = "Test@123456",
            ConfirmPassword = "Test@123456"
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка, что есть ошибка валидации email
        results.Should().Contain(r =>
            r.MemberNames.Contains("Email") &&
            r.ErrorMessage!.Contains("формат"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RegisterDto_WithEmptyPassword_ShouldFailValidation(string password)
    {
        // Arrange - подготовка данных с пустым паролем
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = password!,
            ConfirmPassword = password!
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка, что есть ошибка валидации пароля
        results.Should().ContainSingle(r => r.MemberNames.Contains("Password"));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public void RegisterDto_WithShortPassword_ShouldFailValidation(string password)
    {
        // Arrange - подготовка данных с коротким паролем (менее 8 символов)
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = password,
            ConfirmPassword = password
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка, что есть ошибка о минимальной длине пароля
        results.Should().Contain(r =>
            r.MemberNames.Contains("Password") &&
            r.ErrorMessage!.Contains("8 символов"));
    }

    [Theory]
    [InlineData("password")]
    [InlineData("PASSWORD")]
    [InlineData("12345678")]
    [InlineData("Password")]
    [InlineData("Password123")]
    public void RegisterDto_WithWeakPassword_ShouldFailValidation(string password)
    {
        // Arrange - подготовка данных со слабым паролем (нет спецсимволов, заглавных, строчных или цифр)
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = password,
            ConfirmPassword = password
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка, что есть ошибка о сложности пароля
        results.Should().Contain(r =>
            r.MemberNames.Contains("Password") &&
            (r.ErrorMessage!.Contains("заглавную") ||
             r.ErrorMessage!.Contains("строчную") ||
             r.ErrorMessage!.Contains("цифру") ||
             r.ErrorMessage!.Contains("специальный")));
    }

    [Fact]
    public void RegisterDto_WithMismatchedPasswords_ShouldFailValidation()
    {
        // Arrange - подготовка данных с несовпадающими паролями
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Different@123"
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка, что есть ошибка о несовпадении паролей
        results.Should().Contain(r =>
            r.MemberNames.Contains("ConfirmPassword") &&
            r.ErrorMessage!.Contains("не совпадают"));
    }

    [Fact]
    public void RegisterDto_WithEmptyConfirmPassword_ShouldFailValidation()
    {
        // Arrange - подготовка данных с пустым подтверждением пароля
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123456",
            ConfirmPassword = ""
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка, что есть ошибка валидации
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void RegisterDto_WithOptionalFields_ShouldPassValidation()
    {
        // Arrange - подготовка данных без опциональных полей (FirstName, LastName)
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456"
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка, что валидация прошла (опциональные поля не обязательны)
        results.Should().BeEmpty();
    }
}
