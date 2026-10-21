using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services;

/// <inheritdoc cref="IBagVerificationService" />
public class BagVerificationService : IBagVerificationService
{
    private readonly AeroLinkDbContext _db;
    private readonly IAuditLogService _auditLog;

    /// <summary>Creates the service with injected database context and optional audit log.</summary>
    public BagVerificationService(AeroLinkDbContext db, IAuditLogService? auditLog = null)
    {
        _db = db;
        _auditLog = auditLog ?? new AuditLogService(db);
    }

    /// <inheritdoc />
    public async Task<BagVerificationResult> VerifyAsync(int flightId, string tag)
    {
        var normalisedTag = (tag ?? string.Empty).Trim();

        var bag = await _db.Bags
            .FirstOrDefaultAsync(b => b.Tag == normalisedTag);

        if (bag is null)
        {
            return new BagVerificationResult
            {
                Outcome = BagVerificationOutcome.InvalidTag,
                Message = $"No bag found with tag '{normalisedTag}'."
            };
        }

        if (bag.FlightId != flightId)
        {
            return new BagVerificationResult
            {
                Outcome = BagVerificationOutcome.WrongFlight,
                Bag = bag,
                Message = $"Bag '{normalisedTag}' belongs to a different flight and cannot be loaded here."
            };
        }

        if (bag.IsLoaded)
        {
            return new BagVerificationResult
            {
                Outcome = BagVerificationOutcome.AlreadyLoaded,
                Bag = bag,
                Message = $"Bag '{normalisedTag}' has already been confirmed as loaded."
            };
        }

        if (bag.IsSpecialHandling)
        {
            return new BagVerificationResult
            {
                Outcome = BagVerificationOutcome.NeedsHandlingAcknowledgement,
                Bag = bag,
                Message = $"Bag '{normalisedTag}' requires special handling: {bag.HandlingType}. " +
                           "Acknowledge the handling instructions before confirming loading."
            };
        }

        return new BagVerificationResult
        {
            Outcome = BagVerificationOutcome.Verified,
            Bag = bag,
            Message = $"Bag '{normalisedTag}' verified against the manifest. Ready to confirm loading."
        };
    }

    /// <inheritdoc />
    public async Task<BagVerificationResult> ConfirmLoadedAsync(int flightId, string tag, string? performedBy = null, string? userRole = null)
    {
        var result = await VerifyAsync(flightId, tag);
        var user = performedBy ?? "Unknown";

        if (!result.CanConfirmLoad || result.Bag is null)
        {
            // Log rejection
            var rejectionReason = result.Outcome switch
            {
                BagVerificationOutcome.InvalidTag => "Invalid tag",
                BagVerificationOutcome.WrongFlight => "Wrong flight",
                BagVerificationOutcome.AlreadyLoaded => "Already loaded",
                BagVerificationOutcome.NeedsHandlingAcknowledgement => "Special handling not acknowledged",
                _ => "Verification failed"
            };

            var flight = await _db.Flights.FindAsync(flightId);
            await _auditLog.LogAsync(
                "BagRejected",
                user,
                $"Bag tag '{(tag ?? "").Trim()}' rejected: {rejectionReason}.",
                userRole,
                flightId,
                flight?.FlightNumber,
                (tag ?? "").Trim());

            return result;
        }

        result.Bag.Outcome = "Loaded";
        result.Bag.LoadedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Log successful load
        var loadFlight = await _db.Flights.FindAsync(flightId);
        await _auditLog.LogAsync(
            "BagLoaded",
            user,
            $"Bag '{result.Bag.Tag}' confirmed as loaded.",
            userRole,
            flightId,
            loadFlight?.FlightNumber,
            result.Bag.Tag);

        result.Message = $"Bag '{result.Bag.Tag}' confirmed as loaded.";
        return result;
    }
}

