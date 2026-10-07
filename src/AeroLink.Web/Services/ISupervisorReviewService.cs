using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>
/// Lets a Baggage Supervisor review open exceptions and record a decision.
/// Supports user story I5: resolve a reported problem, or explicitly
/// approve a bag not to load, so every expected bag has a justified outcome.
/// </summary>
public interface ISupervisorReviewService
{
    /// <summary>Open exceptions awaiting a decision, optionally filtered to one flight.</summary>
    Task<List<BaggageException>> GetOpenExceptionsAsync(int? flightId = null);

    /// <summary>
    /// Resolves the reported problem. The bag's outcome is left unchanged
    /// (Pending), so it can still be loaded normally through I2.
    /// </summary>
    Task<ExceptionDecisionResult> ResolveAsync(int exceptionId, string reason, string? decidedByEmployeeName);

    /// <summary>
    /// Explicitly approves the bag not to be loaded. Sets the bag's outcome
    /// to ApprovedNotToLoad, which I6 counts as accounted-for when checking
    /// whether loading can be closed.
    /// </summary>
    Task<ExceptionDecisionResult> ApproveNotToLoadAsync(int exceptionId, string reason, string? decidedByEmployeeName);
}
