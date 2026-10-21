using System.Security.Claims;
using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>Reconciles and finalises departure baggage loading for I6.</summary>
public interface ILoadingClosureService
{
    /// <summary>Returns current readiness and blockers, or null for an unknown flight.</summary>
    Task<LoadingReadiness?> GetReadinessAsync(int flightId);

    /// <summary>Returns the saved completion record, or null if no summary has been recorded.</summary>
    Task<LoadingCompletion?> GetCompletionAsync(int flightId);

    /// <summary>
    /// Checks Supervisor identity and current readiness before atomically saving
    /// Completed status and a final summary. Failed and repeated requests do not
    /// modify baggage, exceptions, or an existing completion record.
    /// </summary>
    /// <param name="flightId">Departure flight to close.</param>
    /// <param name="supervisor">Authenticated principal supplied by the server, never by a form field.</param>
    /// <returns>The closure outcome, explanatory message and completion record where available.</returns>
    Task<LoadingClosureResult> CloseAsync(int flightId, ClaimsPrincipal supervisor);
}
