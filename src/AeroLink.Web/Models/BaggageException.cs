namespace AeroLink.Web.Models;

/// <summary>
/// A reported problem against a bag (missing, damaged, or another loading
/// issue). Created by I4 and decided by a Supervisor via I5.
/// </summary>
public class BaggageException
{
    /// <summary>Primary key.</summary>
    public int ExceptionId { get; set; }

    /// <summary>Foreign key to the bag this problem was reported against.</summary>
    public int BagId { get; set; }

    /// <summary>Navigation property to the bag.</summary>
    public Bag? Bag { get; set; }

    /// <summary>Foreign key to the flight, kept alongside BagId for simpler filtering by flight.</summary>
    public int FlightId { get; set; }

    /// <summary>Category of the problem: MissingBag, DamagedBag, or Other.</summary>
    public string Category { get; set; } = "Other";

    /// <summary>Free-text description of the problem, supplied by the reporting employee.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Lifecycle status: Open, Resolved, or ApprovedNotToLoad.</summary>
    public string Status { get; set; } = "Open";

    /// <summary>UTC timestamp the problem was reported.</summary>
    public DateTime ReportedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Name of the employee who reported the problem.</summary>
    public string? ReportedByEmployeeName { get; set; }

    /// <summary>Reason recorded by the Supervisor when deciding the exception (I5).</summary>
    public string? DecisionReason { get; set; }

    /// <summary>UTC timestamp the Supervisor's decision was recorded.</summary>
    public DateTime? DecidedAtUtc { get; set; }

    /// <summary>Name of the Supervisor who made the decision.</summary>
    public string? DecidedByEmployeeName { get; set; }

    /// <summary>True while the exception is awaiting a Supervisor decision.</summary>
    public bool IsOpen => Status == "Open";
}
