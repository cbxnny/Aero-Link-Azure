using System.Security.Claims;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.AuditLog;

/// <summary>
/// Supervisor-only page that displays the complete audit trail for the
/// AeroLink system. Supports filtering by flight, action type, user
/// and date range to help supervisors review operational history.
/// </summary>
[Authorize(Roles = "BG_Supervisor")]
public class IndexModel : PageModel
{
    private readonly IAuditLogService _auditLogService;
    private readonly IManifestService _manifestService;

    /// <summary>Creates the page model with injected audit log and manifest services.</summary>
    public IndexModel(IAuditLogService auditLogService, IManifestService manifestService)
    {
        _auditLogService = auditLogService;
        _manifestService = manifestService;
    }

    /// <summary>Audit log entries matching the current filter criteria.</summary>
    public List<AuditLogEntry> Entries { get; set; } = new();

    /// <summary>Flights available for the flight filter dropdown.</summary>
    public List<Flight> Flights { get; set; } = new();

    /// <summary>Optional flight ID filter.</summary>
    [BindProperty(SupportsGet = true)]
    public int? FlightId { get; set; }

    /// <summary>Optional action category filter.</summary>
    [BindProperty(SupportsGet = true)]
    public string? ActionFilter { get; set; }

    /// <summary>Optional user name filter (partial match).</summary>
    [BindProperty(SupportsGet = true)]
    public string? UserFilter { get; set; }

    /// <summary>Total number of entries returned by the query.</summary>
    public int TotalEntries => Entries.Count;

    /// <summary>
    /// All distinct action categories available for filtering.
    /// </summary>
    public static IReadOnlyList<string> ActionCategories { get; } = new[]
    {
        "Login",
        "Logout",
        "BagLoaded",
        "BagRejected",
        "ExceptionRaised",
        "ExceptionResolved",
        "ApprovedNotToLoad",
        "LoadingClosed",
        "SpecialHandlingAcknowledged"
    };

    /// <summary>Loads audit entries using optional query-string filters.</summary>
    public async Task OnGetAsync()
    {
        Flights = await _manifestService.GetFlightsAsync();

        Entries = await _auditLogService.GetEntriesAsync(
            flightId: FlightId,
            action: ActionFilter,
            performedBy: UserFilter);
    }
}
