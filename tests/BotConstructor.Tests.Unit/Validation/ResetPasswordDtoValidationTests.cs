using System.ComponentModel.DataAnnotations;
using BotConstructor.Core.DTOs;
using FluentAssertions;
using Xunit;

namespace BotConstructor.Tests.Unit.Validation;

/// <summary>
/// Unit-тесты для валидации ResetPasswordDto
/// </summary>
public class ResetPasswordDtoValidationTests
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
    public void ResetPasswordDto_WithValidData_ShouldPassValidation()
    {
        // Arrange - подготовка валидных данных
        var dto = new ResetPasswordDto
        {
            Token = "valid-token-12345",
            Password = "NewPass@123",
            ConfirmPassword = "NewPass@123"
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка успешной валидации
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ResetPasswordDto_WithEmptyToken_ShouldFailValidation(string token)
    {
        // Arrange - подготовка данных с пустым токеном
        var dto = new ResetPasswordDto
        {
            Token = token!,
            Password = "NewPass@123",
            ConfirmPassword = "NewPass@123"
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка наличия ошибки валидации токена
        results.Should().ContainSingle(r => r.MemberNames.Contains("Token"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ResetPasswordDto_WithEmptyPassword_ShouldFailValidation(string password)
    {
        // Arrange - подготовка данных с пустым паролем
        var dto = new ResetPasswordDto
        {
            Token = "valid-token",
            Password = password!,
            ConfirmPassword = password!
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка наличия ошибки валидации пароля
        results.Should().ContainSingle(r => r.MemberNames.Contains("Password"));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public void ResetPasswordDto_WithShortPassword_ShouldFailValidation(string password)
    {
        // Arrange - подготовка данных с коротким паролем
        var dto = new ResetPasswordDto
        {
            Token = "valid-token",
            Password = password,
            ConfirmPassword = password
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка наличия ошибки о минимальной длине пароля
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
    public void ResetPasswordDto_WithWeakPassword_ShouldFailValidation(string password)
    {
        // Arrange - подготовка данных со слабым паролем
        var dto = new ResetPasswordDto
        {
            Token = "valid-token",
            Password = password,
            ConfirmPassword = password
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка наличия ошибки о сложности пароля
        results.Should().Contain(r =>
            r.MemberNames.Contains("Password") &&
            (r.ErrorMessage!.Contains("заглавную") ||
             r.ErrorMessage!.Contains("строчную") ||
             r.ErrorMessage!.Contains("цифру") ||
             r.ErrorMessage!.Contains("специальный")));
    }

    [Fact]
    public void ResetPasswordDto_WithMismatchedPasswords_ShouldFailValidation()
    {
        // Arrange - подготовка данных с несовпадающими паролями
        var dto = new ResetPasswordDto
        {
            Token = "valid-token",
            Password = "NewPass@123",
            ConfirmPassword = "Different@456"
        };

        // Act - выполнение валидации
        var results = ValidateDto(dto);

        // Assert - проверка наличия ошибки о несовпадении паролей
        results.Should().Contain(r =>
            r.MemberNames.Contains("ConfirmPassword") &&
            r.ErrorMessage!.Contains("не совпадают"));
    }
}
