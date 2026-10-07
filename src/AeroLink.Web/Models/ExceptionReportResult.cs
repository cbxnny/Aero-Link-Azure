namespace AeroLink.Web.Models;

/// <summary>
/// Outcome codes returned when a problem is reported against a bag,
/// used by user story I4.
/// </summary>
public enum ExceptionReportOutcome
{
    /// <summary>The exception was recorded and the bag flagged as Open.</summary>
    Reported,

    /// <summary>The tag does not exist anywhere in the manifest data.</summary>
    InvalidTag,

    /// <summary>The tag exists but belongs to a different flight.</summary>
    WrongFlight,

    /// <summary>The bag has already been confirmed as loaded and cannot be reported.</summary>
    AlreadyLoaded,

    /// <summary>The bag already has an open exception awaiting a Supervisor decision.</summary>
    AlreadyHasOpenException
}

/// <summary>Result of reporting a problem against a bag (I4).</summary>
public class ExceptionReportResult
{
    /// <summary>What the report attempt found.</summary>
    public ExceptionReportOutcome Outcome { get; set; }

    /// <summary>The created or existing exception, when relevant to the outcome.</summary>
    public BaggageException? Exception { get; set; }

    /// <summary>Human-readable message for display on the report page.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>True when the report was successfully recorded.</summary>
    public bool Success => Outcome == ExceptionReportOutcome.Reported;
}
