namespace AeroLink.Web.Models;

/// <summary>A permanent I6 completion record, saved atomically with the flight's Completed status.</summary>
public class LoadingCompletion
{
    /// <summary>The completed flight; at most one completion record can exist per flight.</summary>
    public int FlightId { get; set; }

    /// <summary>UTC time at which the Supervisor closed loading.</summary>
    public DateTime CompletedAtUtc { get; set; }

    /// <summary>Employee ID taken from the authenticated Supervisor's claims.</summary>
    public string CompletedByEmployeeId { get; set; } = string.Empty;

    /// <summary>Supervisor's display name at the time of completion.</summary>
    public string CompletedByName { get; set; } = string.Empty;

    /// <summary>JSON snapshot of the manifest outcomes and exception decisions at closure.</summary>
    public string SummaryJson { get; set; } = string.Empty;
}
