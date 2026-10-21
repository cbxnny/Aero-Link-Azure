using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services;

/// <inheritdoc cref="IExceptionService" />
public class ExceptionService : IExceptionService
{
    private readonly AeroLinkDbContext _db;
    private readonly IAuditLogService _auditLog;

    /// <summary>Creates the service with injected database context and optional audit log.</summary>
    public ExceptionService(AeroLinkDbContext db, IAuditLogService? auditLog = null)
    {
        _db = db;
        _auditLog = auditLog ?? new AuditLogService(db);
    }

    /// <inheritdoc />
    public async Task<ExceptionReportResult> ReportAsync(
        int flightId,
        string tag,
        string category,
        string description,
        string? reportedByEmployeeName)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var normalisedTag = (tag ?? string.Empty).Trim();

        var bag = await _db.Bags
            .FirstOrDefaultAsync(b => b.Tag == normalisedTag);

        if (bag is null)
        {
            return new ExceptionReportResult
            {
                Outcome = ExceptionReportOutcome.InvalidTag,
                Message = $"No bag found with tag '{normalisedTag}'."
            };
        }

        if (bag.FlightId != flightId)
        {
            return new ExceptionReportResult
            {
                Outcome = ExceptionReportOutcome.WrongFlight,
                Message = $"Bag '{normalisedTag}' belongs to a different flight."
            };
        }

        if (await _db.Flights.AnyAsync(f => f.FlightId == flightId && f.LoadingStatus == "Completed"))
        {
            return new ExceptionReportResult
            {
                Outcome = ExceptionReportOutcome.FlightCompleted,
                Message = "Loading for this flight is completed. No further problems can be recorded against the final record."
            };
        }

        if (bag.IsLoaded)
        {
            return new ExceptionReportResult
            {
                Outcome = ExceptionReportOutcome.AlreadyLoaded,
                Message = $"Bag '{normalisedTag}' has already been loaded and cannot be reported."
            };
        }

        var existingOpenException = await _db.Exceptions
            .FirstOrDefaultAsync(e => e.BagId == bag.BagId && e.Status == "Open");

        if (existingOpenException is not null)
        {
            return new ExceptionReportResult
            {
                Outcome = ExceptionReportOutcome.AlreadyHasOpenException,
                Exception = existingOpenException,
                Message = $"Bag '{normalisedTag}' already has an open exception awaiting a Supervisor decision."
            };
        }

        var exception = new BaggageException
        {
            BagId = bag.BagId,
            FlightId = bag.FlightId,
            Category = string.IsNullOrWhiteSpace(category) ? "Other" : category,
            Description = (description ?? string.Empty).Trim(),
            Status = "Open",
            ReportedAtUtc = DateTime.UtcNow,
            ReportedByEmployeeName = reportedByEmployeeName
        };

        _db.Exceptions.Add(exception);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        // Audit log: exception raised
        var flight = await _db.Flights.FindAsync(flightId);
        await _auditLog.LogAsync(
            "ExceptionRaised",
            reportedByEmployeeName ?? "Unknown",
            $"Exception reported for bag '{normalisedTag}': {exception.Category} – {exception.Description}",
            null,
            flightId,
            flight?.FlightNumber,
            normalisedTag);

        return new ExceptionReportResult
        {
            Outcome = ExceptionReportOutcome.Reported,
            Exception = exception,
            Message = $"Problem reported for bag '{normalisedTag}'. It is now flagged for Supervisor review."
        };
    }
}

