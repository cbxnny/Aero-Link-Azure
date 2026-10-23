using System.Security.Claims;
using System.Text.Json;
using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroLink.Tests;

/// <summary>Black-box I6 tests for readiness, role restrictions, closure and saved summaries.</summary>
public class LoadingClosureServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly AeroLinkDbContext _db;
    private readonly ILoadingClosureService _sut;

    public LoadingClosureServiceTests()
    {
        _connection.Open();
        _db = new AeroLinkDbContext(new DbContextOptionsBuilder<AeroLinkDbContext>()
            .UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _db.Flights.AddRange(
            new Flight { FlightId = 1, FlightNumber = "DEMO101", DepartureAirport = "BNE", ArrivalAirport = "SYD" },
            new Flight { FlightId = 2, FlightNumber = "DEMO102" });
        _db.Bags.AddRange(
            new Bag { BagId = 1, FlightId = 1, Tag = "BAG-1" },
            new Bag { BagId = 2, FlightId = 1, Tag = "BAG-2", HandlingType = "Fragile" },
            new Bag { BagId = 3, FlightId = 2, Tag = "OTHER-FLIGHT" });
        _db.SaveChanges();
        _sut = new LoadingClosureService(_db);
    }

    private static ClaimsPrincipal Employee(string role = "BG_Supervisor", bool authenticated = true,
        bool hasId = true) => new(new ClaimsIdentity(
        new[]
        {
            new Claim(ClaimTypes.Name, "Test Supervisor"),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.NameIdentifier, hasId ? "EMP-1001" : "")
        }, authenticated ? "TestCookie" : null));

    private async Task LoadAllAsync()
    {
        var loading = new BagVerificationService(_db);
        await loading.ConfirmLoadedAsync(1, "BAG-1");
        await loading.ConfirmLoadedAsync(1, "BAG-2", handlingAcknowledged: true);
    }

    [Fact]
    public async Task Readiness_ReplacementExceptionChangesRevisionEvenWithIdenticalCounts()
    {
        var original = new BaggageException { FlightId = 1, BagId = 1, Description = "Missing bag" };
        _db.Exceptions.Add(original);
        await _db.SaveChangesAsync();
        var before = (await _sut.GetReadinessAsync(1))!;

        original.Status = "Resolved";
        _db.Exceptions.Add(new BaggageException { FlightId = 1, BagId = 2, Description = "Damaged bag" });
        await _db.SaveChangesAsync();
        var after = (await _sut.GetReadinessAsync(1))!;

        Assert.Equal(before.OutstandingBags.Count, after.OutstandingBags.Count);
        Assert.Equal(before.OpenExceptions.Count, after.OpenExceptions.Count);
        Assert.NotEqual(before.Revision, after.Revision);
    }

    [Fact]
    public async Task Readiness_EditedProblemChangesRevisionWithoutChangingExceptionIdentity()
    {
        var problem = new BaggageException { FlightId = 1, BagId = 1, Description = "Initial description" };
        _db.Exceptions.Add(problem);
        await _db.SaveChangesAsync();
        var before = (await _sut.GetReadinessAsync(1))!;

        problem.Description = "Updated handling problem";
        await _db.SaveChangesAsync();
        var after = (await _sut.GetReadinessAsync(1))!;

        Assert.Equal(Assert.Single(before.OpenExceptions).ExceptionId, Assert.Single(after.OpenExceptions).ExceptionId);
        Assert.NotEqual(before.Revision, after.Revision);
    }

    [Fact]
    public async Task Readiness_ReplacingOutstandingBagChangesRevisionWithIdenticalTotals()
    {
        var first = await _db.Bags.FindAsync(1);
        var second = await _db.Bags.FindAsync(2);
        first!.Outcome = "Loaded";
        await _db.SaveChangesAsync();
        var before = (await _sut.GetReadinessAsync(1))!;

        first.Outcome = "Pending";
        second!.Outcome = "Loaded";
        await _db.SaveChangesAsync();
        var after = (await _sut.GetReadinessAsync(1))!;

        Assert.Equal(before.LoadedCount, after.LoadedCount);
        Assert.Equal(before.OutstandingBags.Count, after.OutstandingBags.Count);
        Assert.NotEqual(before.Revision, after.Revision);
    }

    [Fact]
    public async Task Readiness_StableAcrossRepeatedReadsAndUnrelatedFlightChanges()
    {
        var before = (await _sut.GetReadinessAsync(1))!;
        var repeated = (await _sut.GetReadinessAsync(1))!;
        Assert.Equal(before.Revision, repeated.Revision);

        _db.Exceptions.Add(new BaggageException { FlightId = 2, BagId = 3, Description = "Other flight problem" });
        await _db.SaveChangesAsync();
        Assert.Equal(before.Revision, (await _sut.GetReadinessAsync(1))!.Revision);
    }

    private async Task AssertUnchangedAsync()
    {
        Assert.Equal("NotStarted", (await _db.Flights.AsNoTracking().SingleAsync(f => f.FlightId == 1)).LoadingStatus);
        Assert.Empty(await _db.LoadingCompletions.ToListAsync());
    }

    [Theory]
    [InlineData("BG_Handler", true, true)]
    [InlineData("BG_Supervisor", false, true)]
    [InlineData("BG_Supervisor", true, false)]
    public async Task Close_RequiresAuthenticatedSupervisorWithEmployeeId(string role, bool authenticated, bool hasId)
    {
        await LoadAllAsync();
        var result = await _sut.CloseAsync(1, Employee(role, authenticated, hasId));
        Assert.Equal(LoadingClosureOutcome.Forbidden, result.Outcome);
        await AssertUnchangedAsync();
    }

    [Fact]
    public async Task UnknownFlight_ReturnsNotFoundWithoutCreatingARecord()
    {
        Assert.Null(await _sut.GetReadinessAsync(999));
        Assert.Null(await _sut.GetCompletionAsync(999));
        Assert.Equal(LoadingClosureOutcome.FlightNotFound, (await _sut.CloseAsync(999, Employee())).Outcome);
        await AssertUnchangedAsync();
    }

    [Fact]
    public async Task PendingBag_IsListedAndPreventsClosure()
    {
        await new BagVerificationService(_db).ConfirmLoadedAsync(1, "BAG-1");
        var readiness = await _sut.GetReadinessAsync(1);
        Assert.Equal(2, readiness!.ExpectedCount);
        Assert.Equal(1, readiness.LoadedCount);
        Assert.Equal("BAG-2", Assert.Single(readiness.OutstandingBags).Tag);
        Assert.False(readiness.CanClose);
        Assert.Equal(LoadingClosureOutcome.Blocked, (await _sut.CloseAsync(1, Employee())).Outcome);
        await AssertUnchangedAsync();
    }

    [Fact]
    public async Task OpenException_BlocksClosureEvenWhenEveryBagIsAccountedFor()
    {
        // Model a report already present alongside a recorded loaded outcome.
        await LoadAllAsync();
        _db.Exceptions.Add(new BaggageException { BagId = 1, FlightId = 1, Status = "Open", Description = "Investigate." });
        await _db.SaveChangesAsync();
        var readiness = await _sut.GetReadinessAsync(1);
        Assert.Empty(readiness!.OutstandingBags);
        Assert.Single(readiness.OpenExceptions);
        Assert.False(readiness.CanClose);
        Assert.Equal(LoadingClosureOutcome.Blocked, (await _sut.CloseAsync(1, Employee())).Outcome);
        await AssertUnchangedAsync();
    }

    [Fact]
    public async Task MissingManifest_BlocksClosure()
    {
        _db.Flights.Add(new Flight { FlightId = 3, FlightNumber = "EMPTY" });
        await _db.SaveChangesAsync();
        Assert.False((await _sut.GetReadinessAsync(3))!.CanClose);
        Assert.Equal(LoadingClosureOutcome.Blocked, (await _sut.CloseAsync(3, Employee())).Outcome);
        Assert.Null(await _sut.GetCompletionAsync(3));
    }

    [Fact]
    public async Task UnrecognisedBagOutcome_IsOutstanding()
    {
        (await _db.Bags.FindAsync(1))!.Outcome = "Investigating";
        await _db.SaveChangesAsync();
        var readiness = await _sut.GetReadinessAsync(1);
        Assert.Contains(readiness!.OutstandingBags, b => b.Tag == "BAG-1");
        Assert.Equal(LoadingClosureOutcome.Blocked, (await _sut.CloseAsync(1, Employee())).Outcome);
        await AssertUnchangedAsync();
    }

    [Fact]
    public async Task AllLoaded_SavesCompletionIdentityTimeAndSnapshot()
    {
        await LoadAllAsync();
        var before = DateTime.UtcNow;
        Assert.True((await _sut.GetReadinessAsync(1))!.CanClose);
        var result = await _sut.CloseAsync(1, Employee());
        Assert.Equal(LoadingClosureOutcome.Completed, result.Outcome);
        var record = await _sut.GetCompletionAsync(1);
        Assert.NotNull(record);
        Assert.InRange(record!.CompletedAtUtc, before, DateTime.UtcNow);
        Assert.Equal("EMP-1001", record.CompletedByEmployeeId);
        Assert.Equal("Test Supervisor", record.CompletedByName);
        var summary = JsonSerializer.Deserialize<LoadingSummary>(record.SummaryJson)!;
        Assert.Equal("DEMO101", summary.FlightNumber);
        Assert.Equal(2, summary.ExpectedCount);
        Assert.Equal(2, summary.LoadedCount);
        Assert.Equal(0, summary.ApprovedNotToLoadCount);
        Assert.Equal(new[] { "BAG-1", "BAG-2" }, summary.Bags.Select(b => b.Tag));
        Assert.All(summary.Bags, b => Assert.Equal("Loaded", b.Outcome));
        var readiness = await _sut.GetReadinessAsync(1);
        Assert.True(readiness!.IsCompleted);
        Assert.False(readiness.CanClose);
        Assert.Equal("NotStarted", (await _sut.GetReadinessAsync(2))!.Flight.LoadingStatus);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupervisorApprovedOutcomes_CountAsAccountedForAndAppearInSummary(bool approveBoth)
    {
        var reportService = new ExceptionService(_db);
        var review = new SupervisorReviewService(_db);
        foreach (var tag in approveBoth ? new[] { "BAG-1", "BAG-2" } : new[] { "BAG-2" })
        {
            var report = await reportService.ReportAsync(1, tag, "DamagedBag", "Damaged case.", "Handler");
            Assert.True(report.Success);
            Assert.True((await review.ApproveNotToLoadAsync(
                report.Exception!.ExceptionId, "Retain for repair.", "Test Supervisor")).Success);
        }
        if (!approveBoth) await new BagVerificationService(_db).ConfirmLoadedAsync(1, "BAG-1");

        var result = await _sut.CloseAsync(1, Employee());

        Assert.Equal(LoadingClosureOutcome.Completed, result.Outcome);
        var summary = JsonSerializer.Deserialize<LoadingSummary>(result.Completion!.SummaryJson)!;
        Assert.Equal(approveBoth ? 2 : 1, summary.ApprovedNotToLoadCount);
        Assert.Equal(approveBoth ? 0 : 1, summary.LoadedCount);
        Assert.All(summary.Exceptions, e =>
        {
            Assert.Equal("ApprovedNotToLoad", e.Status);
            Assert.Equal("Retain for repair.", e.DecisionReason);
        });
    }

    [Fact]
    public async Task ResolvingAnException_StillRequiresTheBagToBeLoaded()
    {
        var report = await new ExceptionService(_db).ReportAsync(1, "BAG-1", "MissingBag", "Not found.", "Handler");
        await new SupervisorReviewService(_db).ResolveAsync(report.Exception!.ExceptionId, "Found.", "Supervisor");
        Assert.Equal(LoadingClosureOutcome.Blocked, (await _sut.CloseAsync(1, Employee())).Outcome);
        await LoadAllAsync();
        Assert.Equal(LoadingClosureOutcome.Completed, (await _sut.CloseAsync(1, Employee())).Outcome);
    }

    [Fact]
    public async Task AnotherFlightsOpenException_DoesNotBlockThisFlight()
    {
        await new ExceptionService(_db).ReportAsync(2, "OTHER-FLIGHT", "MissingBag", "Not found.", "Handler");
        await LoadAllAsync();
        Assert.Equal(LoadingClosureOutcome.Completed, (await _sut.CloseAsync(1, Employee())).Outcome);
    }

    [Fact]
    public async Task ReadinessIsRechecked_WhenAProblemAppearsBeforeClosure()
    {
        await LoadAllAsync();
        Assert.True((await _sut.GetReadinessAsync(1))!.CanClose);
        _db.Exceptions.Add(new BaggageException { BagId = 2, FlightId = 1, Status = "Open" });
        await _db.SaveChangesAsync();
        Assert.Equal(LoadingClosureOutcome.Blocked, (await _sut.CloseAsync(1, Employee())).Outcome);
        await AssertUnchangedAsync();
    }

    [Fact]
    public async Task RepeatedClosure_PreservesOriginalSummaryAndTimestamp()
    {
        await LoadAllAsync();
        var first = await _sut.CloseAsync(1, Employee());
        var second = await _sut.CloseAsync(1, Employee());
        Assert.Equal(LoadingClosureOutcome.AlreadyCompleted, second.Outcome);
        Assert.Equal(first.Completion!.CompletedAtUtc, second.Completion!.CompletedAtUtc);
        Assert.Equal(first.Completion.SummaryJson, second.Completion.SummaryJson);
        Assert.Single(await _db.LoadingCompletions.ToListAsync());
    }

    [Fact]
    public async Task ExistingDatabaseUpgrade_IsRepeatableAndPreservesOperationalData()
    {
        await LoadAllAsync();
        await _db.Database.ExecuteSqlRawAsync("DROP TABLE LoadingCompletions");
        await DbSeeder.SeedAsync(_db, "/unused-no-seeding-expected");
        await DbSeeder.SeedAsync(_db, "/unused-no-seeding-expected");
        Assert.Equal(3, await _db.Bags.CountAsync());
        Assert.Equal(2, await _db.Bags.CountAsync(b => b.Outcome == "Loaded"));
        Assert.Empty(await _db.LoadingCompletions.ToListAsync());
        Assert.Equal(LoadingClosureOutcome.Completed, (await _sut.CloseAsync(1, Employee())).Outcome);
    }

    [Fact]
    public async Task Close_Completed_WritesLoadingClosedAuditEntry()
    {
        await LoadAllAsync();

        var result = await _sut.CloseAsync(1, Employee());

        Assert.Equal(LoadingClosureOutcome.Completed, result.Outcome);
        var entry = Assert.Single(await _db.AuditLog.AsNoTracking().Where(e => e.Action == "LoadingClosed").ToListAsync());
        Assert.Equal("Test Supervisor", entry.PerformedBy);
        Assert.Equal("BG_Supervisor", entry.UserRole);
        Assert.Equal(1, entry.FlightId);
        Assert.Equal("DEMO101", entry.FlightNumber);
    }

    [Fact]
    public async Task Close_Blocked_DoesNotWriteLoadingClosedAuditEntry()
    {
        var result = await _sut.CloseAsync(1, Employee());

        Assert.Equal(LoadingClosureOutcome.Blocked, result.Outcome);
        Assert.Empty(await _db.AuditLog.AsNoTracking().Where(e => e.Action == "LoadingClosed").ToListAsync());
    }

    [Fact]
    public async Task Close_RepeatedRequest_WritesOnlyOneLoadingClosedAuditEntry()
    {
        await LoadAllAsync();

        await _sut.CloseAsync(1, Employee());
        var second = await _sut.CloseAsync(1, Employee());

        Assert.Equal(LoadingClosureOutcome.AlreadyCompleted, second.Outcome);
        Assert.Single(await _db.AuditLog.AsNoTracking().Where(e => e.Action == "LoadingClosed").ToListAsync());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
