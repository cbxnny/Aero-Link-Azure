using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroLink.Web.Tests;

/// <summary>
/// Tests for I4 (ExceptionService) and I5 (SupervisorReviewService).
///
/// NOTE: I haven't seen the actual AeroLink.Tests/BagVerificationServiceTests.cs
/// content, only its filename - if that file sets up its AeroLinkDbContext
/// differently (e.g. the EF Core InMemory provider instead of in-memory SQLite),
/// adjust CreateContext() below to match so both test files share one convention.
/// </summary>
public class ExceptionAndReviewServiceTests
{
    private static AeroLinkDbContext CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AeroLinkDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AeroLinkDbContext(options);
        context.Database.EnsureCreated();

        context.Flights.Add(new Flight { FlightId = 1, FlightLabel = "A", FlightNumber = "DEMO1", DepartureAirport = "BNE", ArrivalAirport = "SYD" });
        context.Bags.Add(new Bag { BagId = 1, FlightId = 1, Tag = "TAG-1", Outcome = "Pending" });
        context.Bags.Add(new Bag { BagId = 2, FlightId = 1, Tag = "TAG-2", Outcome = "Loaded" });
        context.SaveChanges();

        return context;
    }

    // ── I4: ExceptionService.ReportAsync ─────────────────────────────

    [Fact]
    public async Task ReportAsync_UnknownTag_ReturnsInvalidTag()
    {
        using var db = CreateContext();
        var service = new ExceptionService(db);

        var result = await service.ReportAsync(1, "NOT-REAL", "MissingBag", "Can't find it", "Test Handler");

        Assert.Equal(ExceptionReportOutcome.InvalidTag, result.Outcome);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReportAsync_AlreadyLoadedBag_IsRejected()
    {
        using var db = CreateContext();
        var service = new ExceptionService(db);

        var result = await service.ReportAsync(1, "TAG-2", "DamagedBag", "Torn zip", "Test Handler");

        Assert.Equal(ExceptionReportOutcome.AlreadyLoaded, result.Outcome);
    }

    [Fact]
    public async Task ReportAsync_ValidBag_CreatesOpenException()
    {
        using var db = CreateContext();
        var service = new ExceptionService(db);

        var result = await service.ReportAsync(1, "TAG-1", "MissingBag", "Not on the belt", "Test Handler");

        Assert.True(result.Success);
        Assert.Equal("Open", result.Exception!.Status);
    }

    [Fact]
    public async Task ReportAsync_BagAlreadyHasOpenException_IsRejected()
    {
        using var db = CreateContext();
        var service = new ExceptionService(db);

        await service.ReportAsync(1, "TAG-1", "MissingBag", "First report", "Test Handler");
        var second = await service.ReportAsync(1, "TAG-1", "DamagedBag", "Second report", "Test Handler");

        Assert.Equal(ExceptionReportOutcome.AlreadyHasOpenException, second.Outcome);
    }

    // ── I5: SupervisorReviewService ──────────────────────────────────

    [Fact]
    public async Task ResolveAsync_LeavesBagOutcomeUnchanged()
    {
        using var db = CreateContext();
        var exceptionService = new ExceptionService(db);
        var reviewService = new SupervisorReviewService(db);

        var report = await exceptionService.ReportAsync(1, "TAG-1", "MissingBag", "Not found", "Test Handler");
        var decision = await reviewService.ResolveAsync(report.Exception!.ExceptionId, "Found it on recheck", "Test Supervisor");

        Assert.True(decision.Success);
        var bag = await db.Bags.FindAsync(1);
        Assert.Equal("Pending", bag!.Outcome);
    }

    [Fact]
    public async Task ApproveNotToLoadAsync_SetsBagOutcome()
    {
        using var db = CreateContext();
        var exceptionService = new ExceptionService(db);
        var reviewService = new SupervisorReviewService(db);

        var report = await exceptionService.ReportAsync(1, "TAG-1", "MissingBag", "Not found", "Test Handler");
        var decision = await reviewService.ApproveNotToLoadAsync(report.Exception!.ExceptionId, "Not located after search", "Test Supervisor");

        Assert.True(decision.Success);
        var bag = await db.Bags.FindAsync(1);
        Assert.Equal("ApprovedNotToLoad", bag!.Outcome);
    }

    [Fact]
    public async Task GetOpenExceptionsAsync_ExcludesDecidedExceptions()
    {
        using var db = CreateContext();
        var exceptionService = new ExceptionService(db);
        var reviewService = new SupervisorReviewService(db);

        var report = await exceptionService.ReportAsync(1, "TAG-1", "MissingBag", "Not found", "Test Handler");
        await reviewService.ResolveAsync(report.Exception!.ExceptionId, "Found it", "Test Supervisor");

        var openExceptions = await reviewService.GetOpenExceptionsAsync(1);

        Assert.Empty(openExceptions);
    }
}
