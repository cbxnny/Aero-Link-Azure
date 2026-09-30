using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroLink.Tests;

/// <summary>
/// Black-box unit tests for user story I2 (verify a bag tag and confirm
/// loading). Each test drives the service through its public API only and
/// checks the resulting outcome, matching the assignment's black-box testing
/// requirement.
/// </summary>
public class BagVerificationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AeroLinkDbContext _db;
    private readonly IBagVerificationService _sut;

    public BagVerificationServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AeroLinkDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AeroLinkDbContext(options);
        _db.Database.EnsureCreated();

        _db.Flights.AddRange(
            new Flight { FlightId = 1, FlightLabel = "A", FlightNumber = "DEMO101", DepartureAirport = "BNE", ArrivalAirport = "SYD", AircraftId = "AIR-1" },
            new Flight { FlightId = 2, FlightLabel = "B", FlightNumber = "DEMO102", DepartureAirport = "BNE", ArrivalAirport = "MEL", AircraftId = "AIR-2" });

        _db.Bags.AddRange(
            new Bag { BagId = 1, FlightId = 1, Tag = "TAG-001", HandlingType = "Standard", Outcome = "Pending" },
            new Bag { BagId = 2, FlightId = 1, Tag = "TAG-002", HandlingType = "Standard", Outcome = "Loaded", LoadedAtUtc = DateTime.UtcNow },
            new Bag { BagId = 3, FlightId = 2, Tag = "TAG-003", HandlingType = "Standard", Outcome = "Pending" },
            new Bag { BagId = 4, FlightId = 1, Tag = "TAG-004", HandlingType = "Fragile", Outcome = "Pending" });

        _db.SaveChanges();

        _sut = new BagVerificationService(_db);
    }

    [Fact]
    public async Task VerifyAsync_ValidTagOnCorrectFlight_ReturnsVerified()
    {
        var result = await _sut.VerifyAsync(flightId: 1, tag: "TAG-001");

        Assert.Equal(BagVerificationOutcome.Verified, result.Outcome);
        Assert.True(result.CanConfirmLoad);
    }

    [Fact]
    public async Task VerifyAsync_UnknownTag_ReturnsInvalidTag()
    {
        var result = await _sut.VerifyAsync(flightId: 1, tag: "DOES-NOT-EXIST");

        Assert.Equal(BagVerificationOutcome.InvalidTag, result.Outcome);
        Assert.False(result.CanConfirmLoad);
    }

    [Fact]
    public async Task VerifyAsync_TagBelongsToAnotherFlight_ReturnsWrongFlight()
    {
        var result = await _sut.VerifyAsync(flightId: 2, tag: "TAG-001");

        Assert.Equal(BagVerificationOutcome.WrongFlight, result.Outcome);
        Assert.False(result.CanConfirmLoad);
    }

    [Fact]
    public async Task VerifyAsync_AlreadyLoadedBag_ReturnsAlreadyLoaded()
    {
        var result = await _sut.VerifyAsync(flightId: 1, tag: "TAG-002");

        Assert.Equal(BagVerificationOutcome.AlreadyLoaded, result.Outcome);
        Assert.False(result.CanConfirmLoad);
    }

    [Fact]
    public async Task VerifyAsync_SpecialHandlingBag_ReturnsNeedsHandlingAcknowledgement()
    {
        var result = await _sut.VerifyAsync(flightId: 1, tag: "TAG-004");

        Assert.Equal(BagVerificationOutcome.NeedsHandlingAcknowledgement, result.Outcome);
        Assert.False(result.CanConfirmLoad);
    }

    [Fact]
    public async Task ConfirmLoadedAsync_ValidBag_SetsOutcomeToLoadedAndTimestamps()
    {
        var result = await _sut.ConfirmLoadedAsync(flightId: 1, tag: "TAG-001");

        Assert.Equal(BagVerificationOutcome.Verified, result.Outcome);
        Assert.NotNull(result.Bag);
        Assert.Equal("Loaded", result.Bag!.Outcome);
        Assert.NotNull(result.Bag.LoadedAtUtc);
    }

    [Fact]
    public async Task ConfirmLoadedAsync_DoesNotIncreaseLoadedCount_WhenTagInvalid()
    {
        var loadedBefore = _db.Bags.Count(b => b.Outcome == "Loaded");

        await _sut.ConfirmLoadedAsync(flightId: 1, tag: "NOT-A-REAL-TAG");

        var loadedAfter = _db.Bags.Count(b => b.Outcome == "Loaded");
        Assert.Equal(loadedBefore, loadedAfter);
    }

    [Fact]
    public async Task ConfirmLoadedAsync_SpecialHandlingBag_IsRejectedUntilAcknowledged()
    {
        var result = await _sut.ConfirmLoadedAsync(flightId: 1, tag: "TAG-004");

        Assert.Equal(BagVerificationOutcome.NeedsHandlingAcknowledgement, result.Outcome);
        Assert.Equal("Pending", _db.Bags.First(b => b.Tag == "TAG-004").Outcome);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
