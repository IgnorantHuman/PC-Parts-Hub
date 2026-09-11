using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ActionsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ActionsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<AdminAction> ActionList { get; set; }
        = new List<AdminAction>();

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string ActionFilter { get; set; } = "All";

    public async Task OnGetAsync()
    {
        IQueryable<AdminAction> actionQuery =
            _context.AdminActions
                .Include(adminAction => adminAction.Admin)
                .Include(adminAction => adminAction.Product)
                .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            SearchTerm = SearchTerm.Trim();

            actionQuery = actionQuery.Where(adminAction =>
                (
                    adminAction.Product != null &&
                    adminAction.Product.Name.Contains(SearchTerm)
                ) ||
                (
                    adminAction.Admin != null &&
                    (
                        adminAction.Admin.FullName.Contains(SearchTerm) ||
                        (
                            adminAction.Admin.Email != null &&
                            adminAction.Admin.Email.Contains(SearchTerm)
                        )
                    )
                ) ||
                (
                    adminAction.Reason != null &&
                    adminAction.Reason.Contains(SearchTerm)
                ));
        }

        if (!string.IsNullOrWhiteSpace(ActionFilter) &&
            !ActionFilter.Equals(
                "All",
                StringComparison.OrdinalIgnoreCase))
        {
            actionQuery = actionQuery.Where(adminAction =>
                adminAction.Action == ActionFilter);
        }

        ActionList = await actionQuery
            .OrderByDescending(adminAction =>
                adminAction.ActionDate)
            .ToListAsync();
    }
}