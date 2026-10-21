using System.Security.Claims;
using System.Text.Json;
using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services;

/// <inheritdoc cref="ILoadingClosureService" />
public class LoadingClosureService : ILoadingClosureService
{
    private readonly AeroLinkDbContext _db;
    private readonly IAuditLogService _auditLog;

    /// <summary>Creates the service using the request's SQLite database context and audit log.</summary>
    public LoadingClosureService(AeroLinkDbContext db, IAuditLogService? auditLog = null)
    {
        _db = db;
        _auditLog = auditLog ?? new AuditLogService(db);
    }

    /// <inheritdoc />
    public async Task<LoadingReadiness?> GetReadinessAsync(int flightId)
    {
        var flight = await _db.Flights.AsNoTracking().Include(f => f.Bags)
            .SingleOrDefaultAsync(f => f.FlightId == flightId);
        if (flight is null) return null;

        var bagIds = flight.Bags.Select(b => b.BagId).ToList();
        var exceptions = await _db.Exceptions.AsNoTracking().Include(e => e.Bag)
            .Where(e => e.FlightId == flightId || bagIds.Contains(e.BagId))
            .OrderBy(e => e.ExceptionId).ToListAsync();
        return new LoadingReadiness { Flight = flight, Exceptions = exceptions };
    }

    /// <inheritdoc />
    public Task<LoadingCompletion?> GetCompletionAsync(int flightId) =>
        _db.LoadingCompletions.AsNoTracking().SingleOrDefaultAsync(c => c.FlightId == flightId);

    /// <inheritdoc />
    public async Task<LoadingClosureResult> CloseAsync(int flightId, ClaimsPrincipal supervisor)
    {
        var employeeId = supervisor.FindFirstValue(ClaimTypes.NameIdentifier);
        if (supervisor.Identity?.IsAuthenticated != true ||
            !supervisor.IsInRole("BG_Supervisor") || string.IsNullOrWhiteSpace(employeeId))
        {
            return new(LoadingClosureOutcome.Forbidden, "Only a signed-in Baggage Supervisor can close loading.");
        }

        // Readiness and completion are one SQLite transaction. Other operational
        // writes also check Completed within a transaction before saving.
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var readiness = await GetReadinessAsync(flightId);
        if (readiness is null)
            return new(LoadingClosureOutcome.FlightNotFound, "The selected departure flight was not found.");

        if (readiness.IsCompleted)
            return new(LoadingClosureOutcome.AlreadyCompleted, "Loading is already completed. The final record is read-only.",
                await GetCompletionAsync(flightId));

        if (!readiness.CanClose)
            return new(LoadingClosureOutcome.Blocked,
                readiness.ExpectedCount == 0
                    ? "Loading cannot close because this flight has no expected baggage manifest."
                    : $"Loading cannot close: {readiness.OutstandingBags.Count} outstanding bag(s) and {readiness.OpenExceptions.Count} open exception(s).");

        var flight = readiness.Flight;
        var summary = new LoadingSummary(flight.FlightId, flight.FlightNumber, flight.DepartureAirport,
            flight.ArrivalAirport, flight.ScheduledDepartureUtc, flight.AircraftId,
            readiness.ExpectedCount, readiness.LoadedCount, readiness.ApprovedNotToLoadCount,
            flight.Bags.OrderBy(b => b.Tag)
                .Select(b => new LoadingBagSummary(b.Tag, b.HandlingType, b.Outcome, b.LoadedAtUtc)).ToList(),
            readiness.Exceptions.Select(e => new LoadingExceptionSummary(e.ExceptionId,
                e.Bag?.Tag ?? $"Bag #{e.BagId}", e.Category, e.Status, e.Description,
                e.DecisionReason, e.DecidedByEmployeeName, e.DecidedAtUtc)).ToList());
        var completion = new LoadingCompletion
        {
            FlightId = flightId,
            CompletedAtUtc = DateTime.UtcNow,
            CompletedByEmployeeId = employeeId,
            CompletedByName = supervisor.Identity.Name ?? employeeId,
            SummaryJson = JsonSerializer.Serialize(summary)
        };

        // Use a targeted update so this operation only changes loading status.
        await _db.Flights.Where(f => f.FlightId == flightId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.LoadingStatus, "Completed"));
        _db.LoadingCompletions.Add(completion);
        await _db.SaveChangesAsync();

        // Logged before the commit so the closure and its audit entry are saved together.
        await _auditLog.LogAsync(
            "LoadingClosed",
            completion.CompletedByName,
            $"Loading closed for {flight.FlightNumber}: {readiness.LoadedCount} loaded, " +
            $"{readiness.ApprovedNotToLoadCount} approved not to load, {readiness.ExpectedCount} expected.",
            "BG_Supervisor",
            flightId,
            flight.FlightNumber);

        await transaction.CommitAsync();
        return new(LoadingClosureOutcome.Completed, "Loading completed. The final summary is now available.", completion);
    }
}
