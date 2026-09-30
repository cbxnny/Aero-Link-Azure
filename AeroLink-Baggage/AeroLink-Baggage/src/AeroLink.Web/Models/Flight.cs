namespace AeroLink.Web.Models;

/// <summary>
/// A departure flight that carries a baggage manifest. Backs user story I1
/// (view expected bags for a flight) and the flight-selection step of I2.
/// </summary>
public class Flight
{
    /// <summary>Primary key, matches FlightId in the source manifest dataset.</summary>
    public int FlightId { get; set; }

    /// <summary>Short demo label shown in flight pickers, e.g. "A".</summary>
    public string FlightLabel { get; set; } = string.Empty;

    /// <summary>Airline flight number, e.g. "DEMO101".</summary>
    public string FlightNumber { get; set; } = string.Empty;

    /// <summary>IATA/ICAO-style code for the departure airport.</summary>
    public string DepartureAirport { get; set; } = string.Empty;

    /// <summary>IATA/ICAO-style code for the arrival airport.</summary>
    public string ArrivalAirport { get; set; } = string.Empty;

    /// <summary>Scheduled departure time in UTC.</summary>
    public DateTime ScheduledDepartureUtc { get; set; }

    /// <summary>Identifier of the aircraft assigned to this flight.</summary>
    public string AircraftId { get; set; } = string.Empty;

    /// <summary>Overall loading status for the flight (NotStarted, InProgress, Completed).</summary>
    public string LoadingStatus { get; set; } = "NotStarted";

    /// <summary>Bags expected to be loaded onto this flight.</summary>
    public List<Bag> Bags { get; set; } = new();
}
