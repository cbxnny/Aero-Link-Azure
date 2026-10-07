using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>
/// Records problems reported against bags during loading. Supports user
/// story I4: report a missing, damaged, or otherwise problematic bag so a
/// Supervisor can investigate it, and prevent premature loading completion
/// while the problem remains open.
/// </summary>
public interface IExceptionService
{
    /// <summary>
    /// Reports a problem against the bag matching the given tag on the given
    /// flight. Rejects tags that are invalid, belong to another flight,
    /// already loaded, or that already have an open exception.
    /// </summary>
    Task<ExceptionReportResult> ReportAsync(
        int flightId,
        string tag,
        string category,
        string description,
        string? reportedByEmployeeName);
}
