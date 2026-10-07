namespace AeroLink.Web.Models;

/// <summary>Result of a Supervisor deciding an open exception (I5).</summary>
public class ExceptionDecisionResult
{
    /// <summary>True when the decision was recorded successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Human-readable message for display on the review page.</summary>
    public string Message { get; set; } = string.Empty;
}
