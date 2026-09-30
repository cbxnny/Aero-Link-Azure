namespace AeroLink.Web.Models;

/// <summary>
/// Outcome codes returned when a bag tag is checked against a flight's
/// manifest, used by user story I2.
/// </summary>
public enum BagVerificationOutcome
{
    /// <summary>The tag matches the flight's manifest and is not yet loaded.</summary>
    Verified,

    /// <summary>The tag does not exist anywhere in the manifest data.</summary>
    InvalidTag,

    /// <summary>The tag exists but belongs to a different flight.</summary>
    WrongFlight,

    /// <summary>The bag has already been confirmed as loaded.</summary>
    AlreadyLoaded,

    /// <summary>The bag is special-handling and requires acknowledgement first (I3 dependency).</summary>
    NeedsHandlingAcknowledgement
}

/// <summary>Result of verifying a bag tag against a flight's manifest.</summary>
public class BagVerificationResult
{
    /// <summary>What the verification found.</summary>
    public BagVerificationOutcome Outcome { get; set; }

    /// <summary>The matching bag, when one was found on the correct flight.</summary>
    public Bag? Bag { get; set; }

    /// <summary>Human-readable message for display on the verification page.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>True when the outcome allows the bag to be confirmed as loaded.</summary>
    public bool CanConfirmLoad => Outcome == BagVerificationOutcome.Verified;
}
