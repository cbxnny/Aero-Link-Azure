namespace AeroLink.Web.Models;

/// <summary>Current baggage reconciliation for a selected departure flight.</summary>
public class LoadingReadiness
{
    /// <summary>The flight and its expected baggage manifest.</summary>
    public Flight Flight { get; init; } = new();

    /// <summary>Exceptions recorded against this flight or its expected bags.</summary>
    public List<BaggageException> Exceptions { get; init; } = new();

    /// <summary>Total number of expected bags.</summary>
    public int ExpectedCount => Flight.Bags.Count;

    /// <summary>Number of bags successfully loaded.</summary>
    public int LoadedCount => Flight.Bags.Count(b => b.IsLoaded);

    /// <summary>Number of bags recorded as approved not to load by I5.</summary>
    public int ApprovedNotToLoadCount => Flight.Bags.Count(b => b.Outcome == "ApprovedNotToLoad");

    /// <summary>Bags which are neither loaded nor approved not to load.</summary>
    public List<Bag> OutstandingBags => Flight.Bags
        .Where(b => !b.IsLoaded && b.Outcome != "ApprovedNotToLoad").OrderBy(b => b.Tag).ToList();

    /// <summary>Open exceptions which independently prevent closure.</summary>
    public List<BaggageException> OpenExceptions => Exceptions.Where(e => e.Status == "Open").ToList();

    /// <summary>True after loading has already been finalised.</summary>
    public bool IsCompleted => Flight.LoadingStatus == "Completed";

    /// <summary>
    /// True only for an open flight with a non-empty manifest, all bags accounted
    /// for and no open exceptions. An empty manifest requires investigation.
    /// </summary>
    public bool CanClose => !IsCompleted && ExpectedCount > 0 && OutstandingBags.Count == 0 && OpenExceptions.Count == 0;
}

/// <summary>Read-only data captured for the final loading summary.</summary>
public record LoadingSummary(
    int FlightId,
    string FlightNumber,
    string DepartureAirport,
    string ArrivalAirport,
    DateTime ScheduledDepartureUtc,
    string AircraftId,
    int ExpectedCount,
    int LoadedCount,
    int ApprovedNotToLoadCount,
    List<LoadingBagSummary> Bags,
    List<LoadingExceptionSummary> Exceptions);

/// <summary>A bag's final outcome and handling category at closure.</summary>
public record LoadingBagSummary(string Tag, string HandlingType, string Outcome, DateTime? LoadedAtUtc);

/// <summary>An exception and its Supervisor decision at closure.</summary>
public record LoadingExceptionSummary(int ExceptionId, string Tag, string Category, string Status,
    string Description, string? DecisionReason, string? DecidedBy, DateTime? DecidedAtUtc);

/// <summary>Outcome of a request to close a flight's loading operation.</summary>
public enum LoadingClosureOutcome
{
    /// <summary>Loading was closed and the final summary saved.</summary>
    Completed,
    /// <summary>The flight was already closed; its existing record was preserved.</summary>
    AlreadyCompleted,
    /// <summary>The selected flight does not exist.</summary>
    FlightNotFound,
    /// <summary>Missing manifest, outstanding bags or open exceptions prevent closure.</summary>
    Blocked,
    /// <summary>The caller is not an authenticated Supervisor with an employee ID.</summary>
    Forbidden
}

/// <summary>Closure outcome, explanation, and any existing or newly saved completion record.</summary>
public record LoadingClosureResult(LoadingClosureOutcome Outcome, string Message, LoadingCompletion? Completion = null);
