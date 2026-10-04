namespace AeroLink.Web.Models;

/// <summary>
/// A single expected bag on a flight's baggage manifest. Backs user story I1
/// (list bags for a flight) and I2 (verify a bag tag and confirm loading).
/// </summary>
public class Bag
{
    /// <summary>Primary key, matches BagId in the source manifest dataset.</summary>
    public int BagId { get; set; }

    /// <summary>Foreign key to the flight this bag is expected on.</summary>
    public int FlightId { get; set; }

    /// <summary>Navigation property to the owning flight.</summary>
    public Flight? Flight { get; set; }

    /// <summary>Unique bag tag identifier, e.g. "DEMO-101-001".</summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Handling category: Standard, Fragile, Oversized, Wheelchair or
    /// SportingEquipment. Anything other than Standard is special baggage (I3).
    /// </summary>
    public string HandlingType { get; set; } = "Standard";

    /// <summary>Supplied handling instruction text shown to the employee.</summary>
    public string HandlingInstruction { get; set; } = string.Empty;

    /// <summary>
    /// Current loading outcome for the bag: Pending, Loaded, or
    /// ApprovedNotToLoad. Set by I2 (Loaded) or I5 (ApprovedNotToLoad).
    /// </summary>
    public string Outcome { get; set; } = "Pending";

    /// <summary>UTC timestamp the bag was confirmed as loaded, if it has been.</summary>
    public DateTime? LoadedAtUtc { get; set; }

    /// <summary>True when the bag has already been confirmed as loaded.</summary>
    public bool IsLoaded => Outcome == "Loaded";

    /// <summary>True when the bag's handling type requires acknowledgement before loading (I3).</summary>
    public bool IsSpecialHandling => HandlingType != "Standard";
}
