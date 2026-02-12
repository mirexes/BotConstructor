using BotConstructor.Core.DTOs;
using BotConstructor.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BotConstructor.Web.Controllers;

/// <summary>
/// Контроллер личного кабинета пользователя
/// </summary>
[Authorize]
public class ProfileController : Controller
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IProfileService profileService, ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Получить ID текущего пользователя из claims
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    /// <summary>
    /// Получить ID текущей сессии
    /// </summary>
    private string? GetCurrentSessionId()
    {
        return HttpContext.Session.Id;
    }

    /// <summary>
    /// Главная страница профиля
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var user = await _profileService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        return View(user);
    }

    /// <summary>
    /// Редактирование профиля - GET
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = GetCurrentUserId();
        var user = await _profileService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var dto = new UpdateProfileDto
        {
            FirstName = user.FirstName ?? "",
            LastName = user.LastName,
            Email = user.Email
        };

        return View(dto);
    }

    /// <summary>
    /// Редактирование профиля - POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateProfileDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _profileService.UpdateProfileAsync(userId, dto);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.Message);
        return View(dto);
    }

    /// <summary>
    /// Смена пароля - GET
    /// </summary>
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    /// <summary>
    /// Смена пароля - POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _profileService.ChangePasswordAsync(userId, dto);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.Message);
        return View(dto);
    }

    /// <summary>
    /// Настройки - GET
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var userId = GetCurrentUserId();
        var settings = await _profileService.GetUserSettingsAsync(userId);

        if (settings == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var dto = new UpdateSettingsDto
        {
            EmailNotificationsEnabled = settings.EmailNotificationsEnabled,
            BotEventsNotifications = settings.BotEventsNotifications,
            PaymentNotifications = settings.PaymentNotifications,
            NewsletterNotifications = settings.NewsletterNotifications,
            SecurityNotifications = settings.SecurityNotifications,
            Language = settings.Language,
            TimeZone = settings.TimeZone
        };

        return View(dto);
    }

    /// <summary>
    /// Настройки - POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(UpdateSettingsDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _profileService.UpdateSettingsAsync(userId, dto);

        if (result)
        {
            TempData["SuccessMessage"] = "Настройки успешно сохранены";
            return RedirectToAction(nameof(Settings));
        }

        ModelState.AddModelError("", "Произошла ошибка при сохранении настроек");
        return View(dto);
    }

    /// <summary>
    /// Активные сессии - GET
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Sessions()
    {
        var userId = GetCurrentUserId();
        var currentSessionId = GetCurrentSessionId();
        var sessions = await _profileService.GetActiveSessionsAsync(userId, currentSessionId);

        return View(sessions);
    }

    /// <summary>
    /// Завершить все сессии кроме текущей - POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TerminateAllSessions()
    {
        var userId = GetCurrentUserId();
        var currentSessionId = GetCurrentSessionId();
        var count = await _profileService.TerminateAllSessionsAsync(userId, currentSessionId);

        TempData["SuccessMessage"] = $"Завершено сессий: {count}";
        return RedirectToAction(nameof(Sessions));
    }

    /// <summary>
    /// Удаление аккаунта - GET (страница подтверждения)
    /// </summary>
    [HttpGet]
    public IActionResult DeleteAccount()
    {
        return View();
    }

    /// <summary>
    /// Удаление аккаунта - POST (подтверждение)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccountConfirmed()
    {
        var userId = GetCurrentUserId();
        var result = await _profileService.DeleteAccountAsync(userId);

        if (result)
        {
            // Выход из системы
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation($"Пользователь {userId} удалил свой аккаунт");

            TempData["SuccessMessage"] = "Ваш аккаунт был успешно удален";
            return RedirectToAction("Index", "Home");
        }

        TempData["ErrorMessage"] = "Произошла ошибка при удалении аккаунта";
        return RedirectToAction(nameof(DeleteAccount));
    }
}
