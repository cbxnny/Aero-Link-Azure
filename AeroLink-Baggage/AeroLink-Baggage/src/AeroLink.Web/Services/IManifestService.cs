using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>
/// Read-only access to flights and their baggage manifests.
/// Supports user story I1: view expected bags for a departure flight.
/// </summary>
public interface IManifestService
{
    /// <summary>Returns every flight, ordered by scheduled departure time.</summary>
    Task<List<Flight>> GetFlightsAsync();

    /// <summary>Returns a single flight with its bags loaded, or null if not found.</summary>
    Task<Flight?> GetFlightWithBagsAsync(int flightId);
}
