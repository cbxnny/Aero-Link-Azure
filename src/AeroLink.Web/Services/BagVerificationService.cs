using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services;

/// <inheritdoc cref="IBagVerificationService" />
public class BagVerificationService : IBagVerificationService
{
    private readonly AeroLinkDbContext _db;

    /// <summary>Creates the service with an injected database context.</summary>
    public BagVerificationService(AeroLinkDbContext db)
    {
        _db = db;
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
    public async Task<BagVerificationResult> ConfirmLoadedAsync(int flightId, string tag)
    {
        var result = await VerifyAsync(flightId, tag);

        if (!result.CanConfirmLoad || result.Bag is null)
        {
            return result;
        }

        result.Bag.Outcome = "Loaded";
        result.Bag.LoadedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        result.Message = $"Bag '{result.Bag.Tag}' confirmed as loaded.";
        return result;
    }
}
