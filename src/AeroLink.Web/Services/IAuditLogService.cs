using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>
/// Provides audit-trail recording and retrieval for the AeroLink system.
/// Every significant operation (login, bag loaded, rejection, exception,
/// supervisor decision, closure) is logged through this service.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Records a single audit log entry.
    /// </summary>
    /// <param name="action">Short action category, e.g. "BagLoaded".</param>
    /// <param name="performedBy">Display name or employee ID of the acting user.</param>
    /// <param name="details">Human-readable description of what happened.</param>
    /// <param name="userRole">Optional role code of the user.</param>
    /// <param name="flightId">Optional flight ID.</param>
    /// <param name="flightNumber">Optional flight number for display.</param>
    /// <param name="bagTag">Optional bag tag involved in the action.</param>
    Task LogAsync(
        string action,
        string performedBy,
        string details,
        string? userRole = null,
        int? flightId = null,
        string? flightNumber = null,
        string? bagTag = null);

    /// <summary>
    /// Retrieves audit log entries, optionally filtered by flight, action,
    /// user or date range, ordered newest-first.
    /// </summary>
    /// <param name="flightId">If provided, only entries for this flight.</param>
    /// <param name="action">If provided, only entries with this action category.</param>
    /// <param name="performedBy">If provided, only entries by this user (partial match).</param>
    /// <param name="fromUtc">If provided, only entries on or after this time.</param>
    /// <param name="toUtc">If provided, only entries on or before this time.</param>
    /// <param name="maxResults">Maximum number of results to return (default 500).</param>
    /// <returns>A list of matching audit log entries, newest first.</returns>
    Task<List<AuditLogEntry>> GetEntriesAsync(
        int? flightId = null,
        string? action = null,
        string? performedBy = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int maxResults = 500);
}
