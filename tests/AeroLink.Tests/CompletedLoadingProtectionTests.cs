using System.Security.Claims;
using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroLink.Tests;

/// <summary>Verifies that existing I2–I5 write operations respect I6's completed-flight boundary.</summary>
public class CompletedLoadingProtectionTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly AeroLinkDbContext _db;

    public CompletedLoadingProtectionTests()
    {
        _connection.Open();
        _db = new AeroLinkDbContext(new DbContextOptionsBuilder<AeroLinkDbContext>()
            .UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _db.Flights.Add(new Flight { FlightId = 1, FlightNumber = "DEMO101" });
        _db.Bags.AddRange(
            new Bag { BagId = 1, FlightId = 1, Tag = "STANDARD" },
            new Bag { BagId = 2, FlightId = 1, Tag = "FRAGILE", HandlingType = "Fragile" });
        _db.SaveChanges();
    }

    private async Task MarkLegacyFlightCompletedAsync() =>
        await _db.Flights.Where(f => f.FlightId == 1)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.LoadingStatus, "Completed"));

    [Theory]
    [InlineData("STANDARD", false)]
    [InlineData("STANDARD", true)]
    [InlineData("FRAGILE", false)]
    [InlineData("FRAGILE", true)]
    public async Task CompletedFlight_BlocksVerificationAndLoadingEvenWithAcknowledgement(string tag, bool acknowledged)
    {
        // Older/imported Completed records must also be read-only, even without a saved summary.
        await MarkLegacyFlightCompletedAsync();
        var loading = new BagVerificationService(_db);
        Assert.Equal(BagVerificationOutcome.FlightCompleted, (await loading.VerifyAsync(1, tag)).Outcome);
        Assert.Equal(BagVerificationOutcome.FlightCompleted,
            (await loading.ConfirmLoadedAsync(1, tag, acknowledged)).Outcome);
        var bag = await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == tag);
        Assert.Equal("Pending", bag.Outcome);
        Assert.Null(bag.LoadedAtUtc);
    }

    [Theory]
    [InlineData("STANDARD")]
    [InlineData("FRAGILE")]
    public async Task CompletedFlight_BlocksNewExceptionReports(string tag)
    {
        await MarkLegacyFlightCompletedAsync();
        var result = await new ExceptionService(_db).ReportAsync(1, tag, "DamagedBag", "New report.", "Handler");
        Assert.Equal(ExceptionReportOutcome.FlightCompleted, result.Outcome);
        Assert.False(result.Success);
        Assert.Empty(await _db.Exceptions.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedFlight_BlocksBothSupervisorDecisionPaths(bool approveNotToLoad)
    {
        var report = await new ExceptionService(_db).ReportAsync(1, "STANDARD", "DamagedBag", "Original report.", "Handler");
        Assert.True(report.Success);
        await MarkLegacyFlightCompletedAsync();
        var review = new SupervisorReviewService(_db);

        var result = approveNotToLoad
            ? await review.ApproveNotToLoadAsync(report.Exception!.ExceptionId, "Changed decision.", "Supervisor")
            : await review.ResolveAsync(report.Exception!.ExceptionId, "Changed decision.", "Supervisor");

        Assert.False(result.Success);
        Assert.Contains("completed", result.Message);
        var exception = await _db.Exceptions.AsNoTracking().SingleAsync();
        Assert.Equal("Open", exception.Status);
        Assert.Null(exception.DecisionReason);
        Assert.Null(exception.DecidedAtUtc);
        Assert.Equal("Pending", (await _db.Bags.AsNoTracking().SingleAsync(b => b.Tag == "STANDARD")).Outcome);
    }

    [Fact]
    public async Task ClosureAfterVerification_BlocksAStaleConfirmationRequest()
    {
        var loading = new BagVerificationService(_db);
        Assert.True((await loading.VerifyAsync(1, "STANDARD")).CanConfirmLoad);
        await MarkLegacyFlightCompletedAsync();

        var result = await loading.ConfirmLoadedAsync(1, "STANDARD");

        Assert.Equal(BagVerificationOutcome.FlightCompleted, result.Outcome);
        Assert.Equal(0, await _db.Bags.CountAsync(b => b.Outcome == "Loaded"));
    }

    [Fact]
    public async Task CompletedMixedManifest_RemainsReviewableAndUnchangedAfterAllWriteAttempts()
    {
        var loading = new BagVerificationService(_db);
        var reporting = new ExceptionService(_db);
        var review = new SupervisorReviewService(_db);
        await loading.ConfirmLoadedAsync(1, "STANDARD");
        var report = await reporting.ReportAsync(1, "FRAGILE", "DamagedBag", "Damaged.", "Handler");
        await review.ApproveNotToLoadAsync(report.Exception!.ExceptionId, "Retain for repair.", "Supervisor");
        var supervisor = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "EMP-1001"),
            new Claim(ClaimTypes.Name, "Supervisor"),
            new Claim(ClaimTypes.Role, "BG_Supervisor")
        }, "TestCookie"));
        var closure = new LoadingClosureService(_db);
        var result = await closure.CloseAsync(1, supervisor);
        Assert.Equal(LoadingClosureOutcome.Completed, result.Outcome);
        var saved = result.Completion!;

        Assert.Equal(BagVerificationOutcome.FlightCompleted,
            (await loading.ConfirmLoadedAsync(1, "FRAGILE", true)).Outcome);
        Assert.Equal(ExceptionReportOutcome.FlightCompleted,
            (await reporting.ReportAsync(1, "FRAGILE", "Other", "Change record.", "Handler")).Outcome);
        Assert.False((await review.ResolveAsync(report.Exception.ExceptionId, "Change decision.", "Supervisor")).Success);
        Assert.False((await review.ApproveNotToLoadAsync(report.Exception.ExceptionId, "New reason.", "Supervisor")).Success);

        // A new context models a later task/request reading the durable record.
        using var nextRequest = new AeroLinkDbContext(new DbContextOptionsBuilder<AeroLinkDbContext>()
            .UseSqlite(_connection).Options);
        var persisted = await new LoadingClosureService(nextRequest).GetCompletionAsync(1);
        Assert.Equal(saved.SummaryJson, persisted!.SummaryJson);
        Assert.Equal(saved.CompletedAtUtc, persisted.CompletedAtUtc);
        var manifest = await new ManifestService(nextRequest).GetFlightWithBagsAsync(1);
        Assert.Equal("Completed", manifest!.LoadingStatus);
        Assert.Equal("Loaded", manifest.Bags.Single(b => b.Tag == "STANDARD").Outcome);
        Assert.Equal("ApprovedNotToLoad", manifest.Bags.Single(b => b.Tag == "FRAGILE").Outcome);
        Assert.Equal("Retain for repair.", (await nextRequest.Exceptions.SingleAsync()).DecisionReason);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
