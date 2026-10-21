namespace AeroLink.Web.Models;

/// <summary>
/// Records a single auditable action within the AeroLink system. Every
/// significant operation (login, bag loaded, rejection, exception raised,
/// supervisor decision, closure) is captured here so that Supervisors can
/// review the complete operational history for a flight.
/// </summary>
public class AuditLogEntry
{
    /// <summary>Primary key.</summary>
    public int AuditLogEntryId { get; set; }

    /// <summary>UTC timestamp the action was recorded.</summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Category of action, e.g. "Login", "Logout", "BagLoaded",
    /// "BagRejected", "ExceptionRaised", "ExceptionResolved",
    /// "ApprovedNotToLoad", "LoadingClosed", "SpecialHandlingAcknowledged".
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Display name or employee ID of the user who performed the action.</summary>
    public string PerformedBy { get; set; } = string.Empty;

    /// <summary>Role of the user at the time the action was performed (e.g. "BG_Handler", "BG_Supervisor").</summary>
    public string? UserRole { get; set; }

    /// <summary>Optional flight ID, when the action relates to a specific flight.</summary>
    public int? FlightId { get; set; }

    /// <summary>Optional flight number for display convenience.</summary>
    public string? FlightNumber { get; set; }

    /// <summary>Optional bag tag involved in the action.</summary>
    public string? BagTag { get; set; }

    /// <summary>Human-readable description of what happened.</summary>
    public string Details { get; set; } = string.Empty;
}
