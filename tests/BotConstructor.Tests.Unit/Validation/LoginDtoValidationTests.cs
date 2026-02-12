using System.ComponentModel.DataAnnotations;
using BotConstructor.Core.DTOs;
using FluentAssertions;
using Xunit;

namespace BotConstructor.Tests.Unit.Validation;

/// <summary>
/// Unit-тесты для валидации LoginDto
/// </summary>
public class LoginDtoValidationTests
{
    /// <summary>
    /// Выполняет валидацию объекта и возвращает список ошибок
    /// </summary>
    private static List<ValidationResult> ValidateDto(object dto)
    {
        var context = new ValidationContext(dto, null, null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, context, results, true);
        return results;
    }

    [Fact]
    public void LoginDto_WithValidData_ShouldPassValidation()
    {
        // Arrange - подготовка валидных данных для входа
        var dto = new LoginDto
        {
            Email = "test@example.com",
            Password = "anypassword",
            RememberMe = true
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка, что валидация прошла успешно
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void LoginDto_WithEmptyEmail_ShouldFailValidation(string email)
    {
        // Arrange - подготовка данных с пустым email
        var dto = new LoginDto
        {
            Email = email!,
            Password = "password"
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка наличия ошибки валидации email
        results.Should().ContainSingle(r => r.MemberNames.Contains("Email"));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test")]
    public void LoginDto_WithInvalidEmailFormat_ShouldFailValidation(string email)
    {
        // Arrange - подготовка данных с невалидным форматом email
        var dto = new LoginDto
        {
            Email = email,
            Password = "password"
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка наличия ошибки о неверном формате email
        results.Should().Contain(r =>
            r.MemberNames.Contains("Email") &&
            r.ErrorMessage!.Contains("формат"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void LoginDto_WithEmptyPassword_ShouldFailValidation(string password)
    {
        // Arrange - подготовка данных с пустым паролем
        var dto = new LoginDto
        {
            Email = "test@example.com",
            Password = password!
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка наличия ошибки валидации пароля
        results.Should().ContainSingle(r => r.MemberNames.Contains("Password"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LoginDto_WithRememberMe_ShouldPassValidation(bool rememberMe)
    {
        // Arrange - подготовка данных с разными значениями RememberMe
        var dto = new LoginDto
        {
            Email = "test@example.com",
            Password = "password",
            RememberMe = rememberMe
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка, что валидация прошла независимо от значения RememberMe
        results.Should().BeEmpty();
    }
}
