using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services;

/// <inheritdoc cref="ISupervisorReviewService" />
public class SupervisorReviewService : ISupervisorReviewService
{
    private readonly AeroLinkDbContext _db;

    /// <summary>Creates the service with an injected database context.</summary>
    public SupervisorReviewService(AeroLinkDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<List<BaggageException>> GetOpenExceptionsAsync(int? flightId = null)
    {
        var query = _db.Exceptions
            .Include(e => e.Bag)
            .Where(e => e.Status == "Open");

        if (flightId.HasValue)
        {
            query = query.Where(e => e.FlightId == flightId.Value);
        }

        return await query.OrderBy(e => e.ReportedAtUtc).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ExceptionDecisionResult> ResolveAsync(int exceptionId, string reason, string? decidedByEmployeeName)
    {
        var exception = await _db.Exceptions.FindAsync(exceptionId);

        if (exception is null || exception.Status != "Open")
        {
            return new ExceptionDecisionResult { Success = false, Message = "This exception is not open, or does not exist." };
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return new ExceptionDecisionResult { Success = false, Message = "A reason for the decision is required." };
        }

        exception.Status = "Resolved";
        exception.DecisionReason = reason.Trim();
        exception.DecidedAtUtc = DateTime.UtcNow;
        exception.DecidedByEmployeeName = decidedByEmployeeName;

        await _db.SaveChangesAsync();
        return new ExceptionDecisionResult { Success = true, Message = "Problem resolved. The bag remains pending for loading." };
    }

    /// <inheritdoc />
    public async Task<ExceptionDecisionResult> ApproveNotToLoadAsync(int exceptionId, string reason, string? decidedByEmployeeName)
    {
        var exception = await _db.Exceptions
            .Include(e => e.Bag)
            .FirstOrDefaultAsync(e => e.ExceptionId == exceptionId);

        if (exception is null || exception.Status != "Open")
        {
            return new ExceptionDecisionResult { Success = false, Message = "This exception is not open, or does not exist." };
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return new ExceptionDecisionResult { Success = false, Message = "A reason for the decision is required." };
        }

        exception.Status = "ApprovedNotToLoad";
        exception.DecisionReason = reason.Trim();
        exception.DecidedAtUtc = DateTime.UtcNow;
        exception.DecidedByEmployeeName = decidedByEmployeeName;

        if (exception.Bag is not null)
        {
            exception.Bag.Outcome = "ApprovedNotToLoad";
        }

        await _db.SaveChangesAsync();
        return new ExceptionDecisionResult { Success = true, Message = "Bag approved not to load." };
    }
}
