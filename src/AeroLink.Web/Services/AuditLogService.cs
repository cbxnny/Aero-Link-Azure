using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services;

/// <inheritdoc cref="IAuditLogService" />
public class AuditLogService : IAuditLogService
{
    private readonly AeroLinkDbContext _db;

    /// <summary>Creates the service with an injected database context.</summary>
    public AuditLogService(AeroLinkDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task LogAsync(
        string action,
        string performedBy,
        string details,
        string? userRole = null,
        int? flightId = null,
        string? flightNumber = null,
        string? bagTag = null)
    {
        var entry = new AuditLogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Action = action,
            PerformedBy = performedBy,
            UserRole = userRole,
            FlightId = flightId,
            FlightNumber = flightNumber,
            BagTag = bagTag,
            Details = details
        };

        _db.AuditLog.Add(entry);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<AuditLogEntry>> GetEntriesAsync(
        int? flightId = null,
        string? action = null,
        string? performedBy = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int maxResults = 500)
    {
        var query = _db.AuditLog.AsQueryable();

        if (flightId.HasValue)
        {
            query = query.Where(e => e.FlightId == flightId.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(e => e.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(performedBy))
        {
            query = query.Where(e => e.PerformedBy.Contains(performedBy));
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(e => e.TimestampUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(e => e.TimestampUtc <= toUtc.Value);
        }

        return await query
            .OrderByDescending(e => e.TimestampUtc)
            .Take(maxResults)
            .ToListAsync();
    }
}
