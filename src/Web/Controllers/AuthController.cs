using BotConstructor.Core.DTOs;
using BotConstructor.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace BotConstructor.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var result = await _authService.RegisterAsync(dto, ipAddress);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        var result = await _authService.LoginAsync(dto, ipAddress, userAgent);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(dto);
        }

        // Create claims for authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.User!.Id.ToString()),
            new Claim(ClaimTypes.Email, result.User.Email),
            new Claim(ClaimTypes.Name, result.User.FirstName ?? result.User.Email)
        };

        // Add roles
        foreach (var userRole in result.User.UserRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = dto.RememberMe,
            ExpiresUtc = dto.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(12)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation($"User {result.User.Email} logged in successfully");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl });
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl,
            Items = { { "provider", provider } }
        };
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        var result = await HttpContext.AuthenticateAsync("Cookies.External");
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Ошибка входа через внешний сервис. Попробуйте ещё раз.";
            return RedirectToAction(nameof(Login));
        }

        var principal = result.Principal;
        var provider = result.Properties?.Items.GetValueOrDefault("provider") ?? "Unknown";
        var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = principal.FindFirstValue(ClaimTypes.Surname);

        await HttpContext.SignOutAsync("Cookies.External");

        if (string.IsNullOrEmpty(providerKey))
        {
            TempData["ErrorMessage"] = "Не удалось получить данные аккаунта от внешнего сервиса.";
            return RedirectToAction(nameof(Login));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var serviceResult = await _authService.ExternalLoginAsync(provider, providerKey, email, firstName, lastName, ipAddress);

        if (!serviceResult.Success)
        {
            TempData["ErrorMessage"] = serviceResult.Message;
            return RedirectToAction(nameof(Login));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, serviceResult.User!.Id.ToString()),
            new Claim(ClaimTypes.Email, serviceResult.User.Email),
            new Claim(ClaimTypes.Name, serviceResult.User.FirstName ?? serviceResult.User.Email)
        };

        foreach (var userRole in serviceResult.User.UserRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation($"User {serviceResult.User.Email} logged in via {provider}");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User logged out");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            ViewBag.Message = "Неверная ссылка подтверждения";
            ViewBag.Success = false;
            return View();
        }

        var result = await _authService.ConfirmEmailAsync(token);

        if (result)
        {
            ViewBag.Message = "Ваш email успешно подтвержден! Теперь вы можете войти в систему.";
            ViewBag.Success = true;
        }
        else
        {
            ViewBag.Message = "Ссылка подтверждения недействительна или истекла. Пожалуйста, запросите новую.";
            ViewBag.Success = false;
        }

        return View();
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        await _authService.RequestPasswordResetAsync(dto);

        TempData["SuccessMessage"] = "Если указанный email существует, на него будет отправлено письмо с инструкциями по восстановлению пароля.";
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        var dto = new ResetPasswordDto { Token = token };
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var result = await _authService.ResetPasswordAsync(dto, ipAddress);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Login));
    }
}
