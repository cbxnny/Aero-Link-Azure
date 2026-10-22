using System.Text;
using System.Text.Json;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.LoadingClosure;

/// <summary>Displays loading readiness and saved summaries; only Supervisors may submit closure.</summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly IManifestService _manifestService;
    private readonly ILoadingClosureService _closureService;

    /// <summary>Creates the page with manifest and closure services.</summary>
    public IndexModel(IManifestService manifestService, ILoadingClosureService closureService)
    {
        _manifestService = manifestService;
        _closureService = closureService;
    }

    /// <summary>Flight selected for reconciliation or final-summary review.</summary>
    [BindProperty(SupportsGet = true)]
    public int? FlightId { get; set; }

    /// <summary>Available flights, including completed flights.</summary>
    public List<Flight> Flights { get; private set; } = new();

    /// <summary>Current readiness, including outstanding bags and open exceptions.</summary>
    public LoadingReadiness? Readiness { get; private set; }

    /// <summary>Saved closure metadata when the flight has been completed.</summary>
    public LoadingCompletion? Completion { get; private set; }

    /// <summary>The final snapshot used for reporting rather than recomputed live outcomes.</summary>
    public LoadingSummary? Summary { get; private set; }

    /// <summary>Reason a submitted closure could not proceed.</summary>
    public string? StatusMessage { get; private set; }

    /// <summary>Displays a selected flight's readiness or saved final summary.</summary>
    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return FlightId.HasValue && Readiness is null ? NotFound() : Page();
    }

    /// <summary>Returns the current counts as JSON so the readiness card can refresh without a reload.</summary>
    public async Task<IActionResult> OnGetStatusAsync()
    {
        if (!FlightId.HasValue) return BadRequest();
        var readiness = await _closureService.GetReadinessAsync(FlightId.Value);
        if (readiness is null) return NotFound();

        return new JsonResult(new
        {
            expected = readiness.ExpectedCount,
            loaded = readiness.LoadedCount,
            approvedNotToLoad = readiness.ApprovedNotToLoadCount,
            outstanding = readiness.OutstandingBags.Count,
            openExceptions = readiness.OpenExceptions.Count,
            canClose = readiness.CanClose,
            completed = readiness.IsCompleted
        });
    }

    /// <summary>Downloads the saved final summary of a completed flight as a CSV file.</summary>
    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        if (!FlightId.HasValue) return BadRequest();
        var completion = await _closureService.GetCompletionAsync(FlightId.Value);
        if (completion is null) return NotFound();

        var summary = JsonSerializer.Deserialize<LoadingSummary>(completion.SummaryJson);
        if (summary is null) return NotFound();

        var flightName = new string(summary.FlightNumber.Where(char.IsLetterOrDigit).ToArray());
        var bytes = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(LoadingSummaryCsv.Build(summary, completion)))
            .ToArray();
        return File(bytes, "text/csv", $"loading-summary-{flightName}.csv");
    }

    /// <summary>Rechecks identity and readiness on the server, then redirects to the saved summary.</summary>
    public async Task<IActionResult> OnPostCloseAsync()
    {
        if (!User.IsInRole("BG_Supervisor")) return Forbid();
        if (!ModelState.IsValid || !FlightId.HasValue)
        {
            StatusMessage = "Select a departure flight before closing loading.";
            await LoadAsync();
            return Page();
        }

        var result = await _closureService.CloseAsync(FlightId.Value, User);
        if (result.Outcome == LoadingClosureOutcome.Forbidden) return Forbid();
        if (result.Outcome == LoadingClosureOutcome.FlightNotFound) return NotFound();
        if (result.Outcome is LoadingClosureOutcome.Completed or LoadingClosureOutcome.AlreadyCompleted)
            return RedirectToPage(new { FlightId });

        StatusMessage = result.Message;
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Flights = await _manifestService.GetFlightsAsync();
        if (!FlightId.HasValue) return;
        Readiness = await _closureService.GetReadinessAsync(FlightId.Value);
        Completion = await _closureService.GetCompletionAsync(FlightId.Value);
        Summary = Completion is null ? null : JsonSerializer.Deserialize<LoadingSummary>(Completion.SummaryJson);
    }
}
