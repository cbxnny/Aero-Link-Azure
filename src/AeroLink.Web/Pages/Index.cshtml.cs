using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Pages;

/// <summary>
/// Operational command center and home page for AeroLink Ground Services.
/// Aggregates real-time baggage stats, flight status, and directs handlers and supervisors
/// to their designated workflow modules (Stories T1, I1-I5).
/// </summary>
public class IndexModel : PageModel
{
    private readonly AeroLinkDbContext _db;

    public IndexModel(AeroLinkDbContext db)
    {
        _db = db;
    }

    public int TotalFlights { get; set; }
    public int TotalBags { get; set; }
    public int LoadedBags { get; set; }
    public int PendingBags { get; set; }
    public int OpenExceptions { get; set; }
    public double LoadPercentage { get; set; }

    public List<FlightSummary> Flights { get; set; } = new();

    public record FlightSummary(
        int FlightId,
        string FlightNumber,
        string Destination,
        DateTime ScheduledDepartureUtc,
        string AircraftId,
        string Status,
        int TotalBags,
        int LoadedBags);

    public async Task OnGetAsync()
    {
        TotalFlights = await _db.Flights.CountAsync();
        TotalBags = await _db.Bags.CountAsync();
        LoadedBags = await _db.Bags.CountAsync(b => b.Outcome == "Loaded");
        try
        {
            OpenExceptions = await _db.Exceptions.CountAsync(e => e.Status == "Open");
        }
        catch
        {
            OpenExceptions = 0;
        }

        LoadPercentage = TotalBags > 0
            ? Math.Round((double)LoadedBags / TotalBags * 100, 1)
            : 0;

        var flightEntities = await _db.Flights
            .Include(f => f.Bags)
            .OrderBy(f => f.ScheduledDepartureUtc)
            .Take(6)
            .ToListAsync();

        Flights = flightEntities.Select(f => new FlightSummary(
            f.FlightId,
            f.FlightNumber,
            f.ArrivalAirport,
            f.ScheduledDepartureUtc,
            f.AircraftId,
            f.LoadingStatus,
            f.Bags.Count,
            f.Bags.Count(b => b.Outcome == "Loaded")
        )).ToList();
    }
}
