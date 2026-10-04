using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Manifest;

/// <summary>
/// Implements user story I1: a Baggage Handler selects a departure flight
/// and views the list of bags expected on it, with tag, handling type and
/// current loading status.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IManifestService _manifestService;

    /// <summary>Creates the page model with an injected manifest service.</summary>
    public IndexModel(IManifestService manifestService)
    {
        _manifestService = manifestService;
    }

    /// <summary>All flights available for selection in the dropdown.</summary>
    public List<Flight> Flights { get; set; } = new();

    /// <summary>The currently selected flight, with its bags loaded, if any.</summary>
    public Flight? SelectedFlight { get; set; }

    /// <summary>Flight id chosen from the query string or the selection form.</summary>
    [BindProperty(SupportsGet = true)]
    public int? FlightId { get; set; }

    /// <summary>Loads the flight list, and the selected flight's manifest if one was chosen.</summary>
    public async Task OnGetAsync()
    {
        Flights = await _manifestService.GetFlightsAsync();

        if (FlightId.HasValue)
        {
            SelectedFlight = await _manifestService.GetFlightWithBagsAsync(FlightId.Value);
        }
    }
}
