using BotConstructor.Core.Entities;

namespace BotConstructor.Web.Models;

public class AdminUsersViewModel
{
    public IEnumerable<User> Users { get; set; } = new List<User>();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // Filter properties
    public string? SearchTerm { get; set; }
    public bool? IsBlocked { get; set; }
    public bool? EmailConfirmed { get; set; }
    public DateTime? RegisteredAfter { get; set; }
    public DateTime? RegisteredBefore { get; set; }

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
