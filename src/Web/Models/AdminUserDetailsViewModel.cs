using BotConstructor.Core.Entities;

namespace BotConstructor.Web.Models;

public class AdminUserDetailsViewModel
{
    public User User { get; set; } = null!;
    public IEnumerable<LoginAttempt> LoginHistory { get; set; } = new List<LoginAttempt>();
    public IEnumerable<Role> AllRoles { get; set; } = new List<Role>();

    public IEnumerable<Role> UserRoles => User.UserRoles.Select(ur => ur.Role);
    public IEnumerable<Role> AvailableRoles => AllRoles.Where(r => !User.UserRoles.Any(ur => ur.RoleId == r.Id));
}
