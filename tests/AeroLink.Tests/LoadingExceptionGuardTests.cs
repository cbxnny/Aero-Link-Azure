using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroLink.Tests;

/// <summary>
/// Black-box regression tests connecting I2/I3 loading to I4 exception reports
/// and I5 supervisor decisions through their public service interfaces.
/// </summary>
public class LoadingExceptionGuardTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AeroLinkDbContext _db;
    private readonly IBagVerificationService _loading;
    private readonly IExceptionService _reporting;
    private readonly ISupervisorReviewService _review;

    public LoadingExceptionGuardTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _db = new AeroLinkDbContext(new DbContextOptionsBuilder<AeroLinkDbContext>()
            .UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _db.Flights.Add(new Flight { FlightId = 1, FlightNumber = "DEMO101" });
        _db.Bags.AddRange(
            new Bag { FlightId = 1, Tag = "STANDARD", HandlingType = "Standard" },
            new Bag { FlightId = 1, Tag = "FRAGILE", HandlingType = "Fragile", HandlingInstruction = "Keep upright." });
        _db.SaveChanges();
        _loading = new BagVerificationService(_db);
        _reporting = new ExceptionService(_db);
        _review = new SupervisorReviewService(_db);
    }

    [Theory]
    [InlineData("STANDARD", false)]
    [InlineData("STANDARD", true)]
    [InlineData("FRAGILE", false)]
    [InlineData("FRAGILE", true)]
    public async Task OpenException_BlocksVerificationAndConfirmation(string tag, bool acknowledged)
    {
        var report = await _reporting.ReportAsync(1, tag, "DamagedBag", "Damaged case.", "Handler");
        Assert.True(report.Success);
        _db.ChangeTracker.Clear();

        var verification = await _loading.VerifyAsync(1, tag);
        var confirmation = await _loading.ConfirmLoadedAsync(1, tag, acknowledged);

        Assert.Equal(BagVerificationOutcome.HasOpenException, verification.Outcome);
        Assert.Equal(BagVerificationOutcome.HasOpenException, confirmation.Outcome);
        Assert.False(verification.CanConfirmLoad);
        Assert.False(confirmation.CanConfirmLoad);
        var bag = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == tag);
        Assert.Equal("Pending", bag.Outcome);
        Assert.Null(bag.LoadedAtUtc);
        Assert.Equal(0, await _db.Bags.CountAsync(b => b.Outcome == "Loaded"));
        Assert.Equal("Open", (await _db.Exceptions.AsNoTracking().SingleAsync()).Status);
    }

    [Theory]
    [InlineData("STANDARD", false)]
    [InlineData("STANDARD", true)]
    [InlineData("FRAGILE", false)]
    [InlineData("FRAGILE", true)]
    public async Task ApprovedNotToLoad_BlocksLoadingAndPreservesSupervisorDecision(string tag, bool acknowledged)
    {
        var report = await _reporting.ReportAsync(1, tag, "DamagedBag", "Damaged case.", "Handler");
        Assert.True(report.Success);
        var decision = await _review.ApproveNotToLoadAsync(report.Exception!.ExceptionId, "Retain for repair.", "Supervisor");
        Assert.True(decision.Success);
        _db.ChangeTracker.Clear();

        var verification = await _loading.VerifyAsync(1, tag);
        var confirmation = await _loading.ConfirmLoadedAsync(1, tag, acknowledged);

        Assert.Equal(BagVerificationOutcome.ApprovedNotToLoad, verification.Outcome);
        Assert.Equal(BagVerificationOutcome.ApprovedNotToLoad, confirmation.Outcome);
        Assert.False(verification.CanConfirmLoad);
        Assert.False(confirmation.CanConfirmLoad);
        var bag = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == tag);
        Assert.Equal("ApprovedNotToLoad", bag.Outcome);
        Assert.Null(bag.LoadedAtUtc);
        Assert.Equal(0, await _db.Bags.CountAsync(b => b.Outcome == "Loaded"));
        var exception = await _db.Exceptions.AsNoTracking().SingleAsync();
        Assert.Equal("ApprovedNotToLoad", exception.Status);
        Assert.Equal("Retain for repair.", exception.DecisionReason);
    }

    [Theory]
    [InlineData("STANDARD", false)]
    [InlineData("FRAGILE", true)]
    public async Task ResolvedException_AllowsLoadingWithRequiredAcknowledgement(string tag, bool acknowledged)
    {
        var report = await _reporting.ReportAsync(1, tag, "MissingBag", "Not found.", "Handler");
        Assert.True(report.Success);
        var decision = await _review.ResolveAsync(report.Exception!.ExceptionId, "Found in staging.", "Supervisor");
        Assert.True(decision.Success);
        _db.ChangeTracker.Clear();

        var result = await _loading.ConfirmLoadedAsync(1, tag, acknowledged);

        Assert.Equal(BagVerificationOutcome.Verified, result.Outcome);
        var bag = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == tag);
        Assert.Equal("Loaded", bag.Outcome);
        Assert.NotNull(bag.LoadedAtUtc);
        Assert.Equal(1, await _db.Bags.CountAsync(b => b.Outcome == "Loaded"));
        Assert.Equal("Resolved", (await _db.Exceptions.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task ResolvedException_DoesNotReplaceSpecialHandlingAcknowledgement()
    {
        var report = await _reporting.ReportAsync(1, "FRAGILE", "MissingBag", "Not found.", "Handler");
        Assert.True(report.Success);
        Assert.True((await _review.ResolveAsync(report.Exception!.ExceptionId, "Found in staging.", "Supervisor")).Success);

        var result = await _loading.ConfirmLoadedAsync(1, "FRAGILE");

        Assert.Equal(BagVerificationOutcome.NeedsHandlingAcknowledgement, result.Outcome);
        var bag = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == "FRAGILE");
        Assert.Equal("Pending", bag.Outcome);
        Assert.Null(bag.LoadedAtUtc);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExceptionReportedAfterVerification_IsRecheckedAtConfirmation(bool approveNotToLoad)
    {
        var verification = await _loading.VerifyAsync(1, "STANDARD");
        Assert.True(verification.CanConfirmLoad);
        var report = await _reporting.ReportAsync(1, "STANDARD", "DamagedBag", "Damage found after verification.", "Handler");
        Assert.True(report.Success);
        if (approveNotToLoad)
        {
            Assert.True((await _review.ApproveNotToLoadAsync(
                report.Exception!.ExceptionId, "Retain for repair.", "Supervisor")).Success);
        }
        _db.ChangeTracker.Clear();

        var result = await _loading.ConfirmLoadedAsync(1, "STANDARD", handlingAcknowledged: true);

        Assert.Equal(approveNotToLoad ? BagVerificationOutcome.ApprovedNotToLoad : BagVerificationOutcome.HasOpenException,
            result.Outcome);
        Assert.False(result.CanConfirmLoad);
        var bag = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == "STANDARD");
        Assert.Equal(approveNotToLoad ? "ApprovedNotToLoad" : "Pending", bag.Outcome);
        Assert.Null(bag.LoadedAtUtc);
        Assert.Equal(0, await _db.Bags.CountAsync(b => b.Outcome == "Loaded"));
    }

    [Fact]
    public async Task OpenExceptionForAnotherBag_DoesNotBlockThisBag()
    {
        Assert.True((await _reporting.ReportAsync(1, "FRAGILE", "DamagedBag", "Damaged case.", "Handler")).Success);

        var result = await _loading.ConfirmLoadedAsync(1, "STANDARD");

        Assert.Equal(BagVerificationOutcome.Verified, result.Outcome);
        Assert.Equal("Loaded", (await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == "STANDARD")).Outcome);
        Assert.Equal("Pending", (await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == "FRAGILE")).Outcome);
    }

    [Fact]
    public async Task EarlierResolvedException_DoesNotHideANewOpenException()
    {
        var first = await _reporting.ReportAsync(1, "STANDARD", "MissingBag", "Not found.", "Handler");
        Assert.True(first.Success);
        Assert.True((await _review.ResolveAsync(first.Exception!.ExceptionId, "Found in staging.", "Supervisor")).Success);
        Assert.True((await _reporting.ReportAsync(1, "STANDARD", "DamagedBag", "Damage found.", "Handler")).Success);
        _db.ChangeTracker.Clear();

        var result = await _loading.ConfirmLoadedAsync(1, "STANDARD");

        Assert.Equal(BagVerificationOutcome.HasOpenException, result.Outcome);
        Assert.False(result.CanConfirmLoad);
        var bag = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == "STANDARD");
        Assert.Equal("Pending", bag.Outcome);
        Assert.Null(bag.LoadedAtUtc);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
