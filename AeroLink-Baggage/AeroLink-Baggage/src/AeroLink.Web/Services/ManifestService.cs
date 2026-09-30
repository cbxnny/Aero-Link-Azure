using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services;

/// <inheritdoc cref="IManifestService" />
public class ManifestService : IManifestService
{
    private readonly AeroLinkDbContext _db;

    /// <summary>Creates the service with an injected database context.</summary>
    public ManifestService(AeroLinkDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<List<Flight>> GetFlightsAsync()
    {
        return await _db.Flights
            .OrderBy(f => f.ScheduledDepartureUtc)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Flight?> GetFlightWithBagsAsync(int flightId)
    {
        return await _db.Flights
            .Include(f => f.Bags)
            .FirstOrDefaultAsync(f => f.FlightId == flightId);
    }
}
