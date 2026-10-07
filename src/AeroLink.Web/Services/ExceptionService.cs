using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services;

/// <inheritdoc cref="IExceptionService" />
public class ExceptionService : IExceptionService
{
    private readonly AeroLinkDbContext _db;

    /// <summary>Creates the service with an injected database context.</summary>
    public ExceptionService(AeroLinkDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<ExceptionReportResult> ReportAsync(
        int flightId,
        string tag,
        string category,
        string description,
        string? reportedByEmployeeName)
    {
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

        return new ExceptionReportResult
        {
            Outcome = ExceptionReportOutcome.Reported,
            Exception = exception,
            Message = $"Problem reported for bag '{normalisedTag}'. It is now flagged for Supervisor review."
        };
    }
}
