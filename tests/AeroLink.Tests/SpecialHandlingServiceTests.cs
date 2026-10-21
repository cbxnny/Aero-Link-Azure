using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroLink.Tests;

/// <summary>
/// Black-box tests for I3, exercising the public verification service and
/// checking persisted loading outcomes using an isolated SQLite database.
/// </summary>
public class SpecialHandlingServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AeroLinkDbContext _db;
    private readonly IBagVerificationService _sut;

    public SpecialHandlingServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _db = new AeroLinkDbContext(new DbContextOptionsBuilder<AeroLinkDbContext>()
            .UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();

        _db.Flights.AddRange(
            new Flight { FlightId = 1, FlightNumber = "DEMO101" },
            new Flight { FlightId = 2, FlightNumber = "DEMO102" });
        _db.Bags.AddRange(
            new Bag { FlightId = 1, Tag = "FRAGILE", HandlingType = "Fragile", HandlingInstruction = "Handle with care. Keep upright." },
            new Bag { FlightId = 1, Tag = "OVERSIZED", HandlingType = "Oversized", HandlingInstruction = "Use the oversized baggage loading area." },
            new Bag { FlightId = 1, Tag = "WHEELCHAIR", HandlingType = "Wheelchair", HandlingInstruction = "Secure mobility equipment carefully." },
            new Bag { FlightId = 1, Tag = "SPORT", HandlingType = "SportingEquipment", HandlingInstruction = "Protect sporting equipment during loading." },
            new Bag { FlightId = 1, Tag = "STANDARD", HandlingType = "Standard" },
            new Bag { FlightId = 2, Tag = "OTHER-FLIGHT", HandlingType = "Fragile" });
        _db.SaveChanges();
        _sut = new BagVerificationService(_db);
    }

    [Theory]
    [InlineData("FRAGILE", "Fragile", "Handle with care. Keep upright.")]
    [InlineData("OVERSIZED", "Oversized", "Use the oversized baggage loading area.")]
    [InlineData("WHEELCHAIR", "Wheelchair", "Secure mobility equipment carefully.")]
    [InlineData("SPORT", "SportingEquipment", "Protect sporting equipment during loading.")]
    public async Task Verify_SpecialBag_ReturnsCategoryAndInstructionsWithoutLoading(
        string tag, string category, string instruction)
    {
        var result = await _sut.VerifyAsync(1, tag);

        Assert.Equal(BagVerificationOutcome.NeedsHandlingAcknowledgement, result.Outcome);
        Assert.False(result.CanConfirmLoad);
        Assert.NotNull(result.Bag);
        Assert.Equal(category, result.Bag!.HandlingType);
        Assert.Equal(instruction, result.Bag.HandlingInstruction);
        var persisted = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == tag);
        Assert.Equal("Pending", persisted.Outcome);
        Assert.Null(persisted.LoadedAtUtc);
    }

    [Theory]
    [InlineData("FRAGILE")]
    [InlineData("OVERSIZED")]
    [InlineData("WHEELCHAIR")]
    [InlineData("SPORT")]
    public async Task Confirm_WithoutAcknowledgement_LeavesSpecialBagPending(string tag)
    {
        var countBefore = await _db.Bags.CountAsync(b => b.Outcome == "Loaded");

        var result = await _sut.ConfirmLoadedAsync(1, tag, handlingAcknowledged: false);

        Assert.Equal(BagVerificationOutcome.NeedsHandlingAcknowledgement, result.Outcome);
        Assert.False(result.CanConfirmLoad);
        var persisted = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == tag);
        Assert.Equal("Pending", persisted.Outcome);
        Assert.Null(persisted.LoadedAtUtc);
        Assert.Equal(countBefore, await _db.Bags.CountAsync(b => b.Outcome == "Loaded"));
    }

    [Theory]
    [InlineData("FRAGILE")]
    [InlineData("OVERSIZED")]
    [InlineData("WHEELCHAIR")]
    [InlineData("SPORT")]
    public async Task Confirm_WithAcknowledgement_LoadsSpecialBagAndRecordsTime(string tag)
    {
        var countBefore = await _db.Bags.CountAsync(b => b.Outcome == "Loaded");
        var before = DateTime.UtcNow;

        var result = await _sut.ConfirmLoadedAsync(1, tag, handlingAcknowledged: true);

        Assert.Equal(BagVerificationOutcome.Verified, result.Outcome);
        var persisted = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == tag);
        Assert.Equal("Loaded", persisted.Outcome);
        Assert.NotNull(persisted.LoadedAtUtc);
        Assert.InRange(persisted.LoadedAtUtc!.Value, before, DateTime.UtcNow);
        Assert.Equal(countBefore + 1, await _db.Bags.CountAsync(b => b.Outcome == "Loaded"));
    }

    [Theory]
    [InlineData("UNKNOWN", BagVerificationOutcome.InvalidTag)]
    [InlineData("OTHER-FLIGHT", BagVerificationOutcome.WrongFlight)]
    public async Task Confirm_AcknowledgementDoesNotBypassTagValidation(
        string tag, BagVerificationOutcome expectedOutcome)
    {
        var result = await _sut.ConfirmLoadedAsync(1, tag, handlingAcknowledged: true);

        Assert.Equal(expectedOutcome, result.Outcome);
        Assert.False(result.CanConfirmLoad);
        Assert.All(await _db.Bags.AsNoTracking().ToListAsync(), bag =>
        {
            Assert.Equal("Pending", bag.Outcome);
            Assert.Null(bag.LoadedAtUtc);
        });
    }

    [Fact]
    public async Task Confirm_RepeatedAcknowledgedConfirmation_DoesNotLoadTwice()
    {
        await _sut.ConfirmLoadedAsync(1, "FRAGILE", handlingAcknowledged: true);
        var firstTimestamp = await _db.Bags.Where(b => b.Tag == "FRAGILE")
            .Select(b => b.LoadedAtUtc).SingleAsync();

        var result = await _sut.ConfirmLoadedAsync(1, "FRAGILE", handlingAcknowledged: true);

        Assert.Equal(BagVerificationOutcome.AlreadyLoaded, result.Outcome);
        Assert.False(result.CanConfirmLoad);
        Assert.Equal(1, await _db.Bags.CountAsync(b => b.Outcome == "Loaded"));
        var persisted = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == "FRAGILE");
        Assert.Equal(firstTimestamp, persisted.LoadedAtUtc);
    }

    [Fact]
    public async Task Confirm_AcknowledgementForOneBag_DoesNotCarryOverToAnother()
    {
        await _sut.ConfirmLoadedAsync(1, "FRAGILE", handlingAcknowledged: true);

        var result = await _sut.ConfirmLoadedAsync(1, "WHEELCHAIR");

        Assert.Equal(BagVerificationOutcome.NeedsHandlingAcknowledgement, result.Outcome);
        var persisted = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == "WHEELCHAIR");
        Assert.Equal("Pending", persisted.Outcome);
        Assert.Null(persisted.LoadedAtUtc);
        Assert.Equal(1, await _db.Bags.CountAsync(b => b.Outcome == "Loaded"));
    }

    [Fact]
    public async Task Confirm_StandardBag_DoesNotRequireAcknowledgement()
    {
        var result = await _sut.ConfirmLoadedAsync(1, "STANDARD");

        Assert.Equal(BagVerificationOutcome.Verified, result.Outcome);
        var persisted = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == "STANDARD");
        Assert.Equal("Loaded", persisted.Outcome);
        Assert.NotNull(persisted.LoadedAtUtc);
        Assert.Equal(1, await _db.Bags.CountAsync(b => b.Outcome == "Loaded"));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
