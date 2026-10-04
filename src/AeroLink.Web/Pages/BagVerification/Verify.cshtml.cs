using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.BagVerification;

/// <summary>
/// Implements user story I2: a Baggage Employee types a bag tag, the system
/// checks it against the selected flight's manifest, and a verified bag can
/// be confirmed as loaded. Invalid, wrong-flight and already-loaded tags are
/// rejected without changing the loaded-bag count.
/// </summary>
public class VerifyModel : PageModel
{
    private readonly IManifestService _manifestService;
    private readonly IBagVerificationService _verificationService;

    /// <summary>Creates the page model with injected manifest and verification services.</summary>
    public VerifyModel(IManifestService manifestService, IBagVerificationService verificationService)
    {
        _manifestService = manifestService;
        _verificationService = verificationService;
    }

    /// <summary>Flights available in the flight-selection dropdown.</summary>
    public List<Flight> Flights { get; set; } = new();

    /// <summary>The flight the employee is currently loading bags for.</summary>
    [BindProperty(SupportsGet = true)]
    public int? FlightId { get; set; }

    /// <summary>The bag tag entered by the employee.</summary>
    [BindProperty]
    public string? Tag { get; set; }

    /// <summary>Result of the most recent verify or confirm action, for display.</summary>
    public BagVerificationResult? Result { get; set; }

    /// <summary>Loads the flight list for the page's selection dropdown.</summary>
    public async Task OnGetAsync()
    {
        Flights = await _manifestService.GetFlightsAsync();
    }

    /// <summary>Checks the entered tag against the manifest without confirming loading.</summary>
    public async Task<IActionResult> OnPostVerifyAsync()
    {
        Flights = await _manifestService.GetFlightsAsync();

        if (!FlightId.HasValue || string.IsNullOrWhiteSpace(Tag))
        {
            ModelState.AddModelError(string.Empty, "Select a flight and enter a bag tag.");
            return Page();
        }

        Result = await _verificationService.VerifyAsync(FlightId.Value, Tag);
        return Page();
    }

    /// <summary>Confirms a verified bag as loaded, re-checking the tag first.</summary>
    public async Task<IActionResult> OnPostConfirmAsync()
    {
        Flights = await _manifestService.GetFlightsAsync();

        if (!FlightId.HasValue || string.IsNullOrWhiteSpace(Tag))
        {
            ModelState.AddModelError(string.Empty, "Select a flight and enter a bag tag.");
            return Page();
        }

        Result = await _verificationService.ConfirmLoadedAsync(FlightId.Value, Tag);
        return Page();
    }
}
