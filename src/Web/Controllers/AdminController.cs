using BotConstructor.Core.Interfaces;
using BotConstructor.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotConstructor.Web.Controllers;

[Authorize(Roles = "SuperAdmin,Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    // GET: /Admin/Users
    [HttpGet]
    public async Task<IActionResult> Users(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        bool? isBlocked = null,
        bool? emailConfirmed = null,
        DateTime? registeredAfter = null,
        DateTime? registeredBefore = null)
    {
        var (users, totalCount) = await _adminService.GetUsersPagedAsync(
            page,
            pageSize,
            searchTerm,
            isBlocked,
            emailConfirmed,
            registeredAfter,
            registeredBefore);

        var viewModel = new AdminUsersViewModel
        {
            Users = users,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            SearchTerm = searchTerm,
            IsBlocked = isBlocked,
            EmailConfirmed = emailConfirmed,
            RegisteredAfter = registeredAfter,
            RegisteredBefore = registeredBefore
        };

        return View(viewModel);
    }

    // GET: /Admin/UserDetails/5
    [HttpGet]
    public async Task<IActionResult> UserDetails(int id)
    {
        var user = await _adminService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var loginHistory = await _adminService.GetUserLoginHistoryAsync(id);
        var allRoles = await _adminService.GetAllRolesAsync();

        var viewModel = new AdminUserDetailsViewModel
        {
            User = user,
            LoginHistory = loginHistory,
            AllRoles = allRoles
        };

        return View(viewModel);
    }

    // POST: /Admin/BlockUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockUser(BlockUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Некорректные данные";
            return RedirectToAction(nameof(UserDetails), new { id = model.UserId });
        }

        var result = await _adminService.BlockUserAsync(model.UserId, model.Reason);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(UserDetails), new { id = model.UserId });
    }

    // POST: /Admin/UnblockUser/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnblockUser(int id)
    {
        var result = await _adminService.UnblockUserAsync(id);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(UserDetails), new { id });
    }

    // POST: /Admin/DeleteUser/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _adminService.DeleteUserAsync(id);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Users));
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(UserDetails), new { id });
        }
    }

    // POST: /Admin/ConfirmEmail/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmEmail(int id)
    {
        var result = await _adminService.ConfirmUserEmailAsync(id);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(UserDetails), new { id });
    }

    // POST: /Admin/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(AdminResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Некорректные данные";
            return RedirectToAction(nameof(UserDetails), new { id = model.UserId });
        }

        var result = await _adminService.ResetUserPasswordAsync(model.UserId, model.NewPassword);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(UserDetails), new { id = model.UserId });
    }

    // POST: /Admin/AssignRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(int userId, int roleId)
    {
        var result = await _adminService.AssignRoleAsync(userId, roleId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(UserDetails), new { id = userId });
    }

    // POST: /Admin/RemoveRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(int userId, int roleId)
    {
        var result = await _adminService.RemoveRoleAsync(userId, roleId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(UserDetails), new { id = userId });
    }
}
