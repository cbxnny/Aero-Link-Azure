using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Exceptions;

/// <summary>
/// Implements user story I4: a Baggage Handler reports a problem (missing,
/// damaged, or another issue) against a bag so a Supervisor can investigate
/// it. The bag is flagged with an open exception, which blocks it from
/// being loaded (see ExceptionService) until a Supervisor decides it (I5).
/// </summary>
public class ReportModel : PageModel
{
    private readonly IManifestService _manifestService;
    private readonly IExceptionService _exceptionService;

    /// <summary>Creates the page model with injected manifest and exception services.</summary>
    public ReportModel(IManifestService manifestService, IExceptionService exceptionService)
    {
        _manifestService = manifestService;
        _exceptionService = exceptionService;
    }

    /// <summary>Flights available in the flight-selection dropdown.</summary>
    public List<Flight> Flights { get; set; } = new();

    /// <summary>The flight the reported bag belongs to.</summary>
    [BindProperty(SupportsGet = true)]
    public int? FlightId { get; set; }

    /// <summary>The bag tag being reported.</summary>
    [BindProperty]
    public string? Tag { get; set; }

    /// <summary>The problem category: MissingBag, DamagedBag, or Other.</summary>
    [BindProperty]
    public string Category { get; set; } = "MissingBag";

    /// <summary>Free-text description of the problem.</summary>
    [BindProperty]
    public string? Description { get; set; }

    /// <summary>Result of the most recent report attempt, for display.</summary>
    public ExceptionReportResult? Result { get; set; }

    /// <summary>Loads the flight list for the page's selection dropdown.</summary>
    public async Task OnGetAsync()
    {
        Flights = await _manifestService.GetFlightsAsync();
    }

    /// <summary>Records the reported problem against the entered bag tag.</summary>
    public async Task<IActionResult> OnPostReportAsync()
    {
        Flights = await _manifestService.GetFlightsAsync();

        if (!FlightId.HasValue || string.IsNullOrWhiteSpace(Tag))
        {
            ModelState.AddModelError(string.Empty, "Select a flight and enter a bag tag.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ModelState.AddModelError(string.Empty, "A description of the problem is required.");
            return Page();
        }

        // TODO: once T2 (login) exists, replace this placeholder with the
        // signed-in employee's name from session.
        const string reportedBy = "Baggage Handler";

        Result = await _exceptionService.ReportAsync(FlightId.Value, Tag, Category, Description, reportedBy);
        return Page();
    }
}
