using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroLink.Tests;

public class AuditLogServiceTests
{
    private static (AeroLinkDbContext Context, SqliteConnection Connection) CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AeroLinkDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AeroLinkDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    [Fact]
    public async Task LogAsync_PersistsEntryToDatabase()
    {
        var (db, connection) = CreateContext();
        using (connection)
        using (db)
        {
            var sut = new AuditLogService(db);

            await sut.LogAsync(
                action: "BagLoaded",
                performedBy: "Alex Chen",
                details: "Bag 'TAG-101' loaded into Hold A.",
                userRole: "BG_Handler",
                flightId: 10,
                flightNumber: "QF501",
                bagTag: "TAG-101");

            var entries = await db.AuditLog.ToListAsync();
            Assert.Single(entries);

            var entry = entries[0];
            Assert.Equal("BagLoaded", entry.Action);
            Assert.Equal("Alex Chen", entry.PerformedBy);
            Assert.Equal("BG_Handler", entry.UserRole);
            Assert.Equal(10, entry.FlightId);
            Assert.Equal("QF501", entry.FlightNumber);
            Assert.Equal("TAG-101", entry.BagTag);
            Assert.Equal("Bag 'TAG-101' loaded into Hold A.", entry.Details);
            Assert.True(entry.TimestampUtc <= DateTime.UtcNow);
        }
    }

    [Fact]
    public async Task GetEntriesAsync_FiltersByFlightAndAction()
    {
        var (db, connection) = CreateContext();
        using (connection)
        using (db)
        {
            var sut = new AuditLogService(db);

            await sut.LogAsync("BagLoaded", "Handler 1", "Loaded bag 1", flightId: 1);
            await sut.LogAsync("ExceptionRaised", "Handler 1", "Missing bag", flightId: 1);
            await sut.LogAsync("BagLoaded", "Handler 2", "Loaded bag 2", flightId: 2);

            var flight1Loaded = await sut.GetEntriesAsync(flightId: 1, action: "BagLoaded");
            Assert.Single(flight1Loaded);
            Assert.Equal(1, flight1Loaded[0].FlightId);
            Assert.Equal("BagLoaded", flight1Loaded[0].Action);

            var allFlight1 = await sut.GetEntriesAsync(flightId: 1);
            Assert.Equal(2, allFlight1.Count);

            var allLoaded = await sut.GetEntriesAsync(action: "BagLoaded");
            Assert.Equal(2, allLoaded.Count);
        }
    }

    [Fact]
    public async Task GetEntriesAsync_FiltersByPerformedBy()
    {
        var (db, connection) = CreateContext();
        using (connection)
        using (db)
        {
            var sut = new AuditLogService(db);

            await sut.LogAsync("Login", "Sam Taylor", "Supervisor logged in");
            await sut.LogAsync("Login", "Jordan Lee", "Handler logged in");

            var results = await sut.GetEntriesAsync(performedBy: "Taylor");
            Assert.Single(results);
            Assert.Equal("Sam Taylor", results[0].PerformedBy);
        }
    }

    [Fact]
    public async Task BagVerification_ConfirmLoaded_CreatesAuditLogEntry()
    {
        var (db, connection) = CreateContext();
        using (connection)
        using (db)
        {
            db.Flights.Add(new Flight { FlightId = 1, FlightNumber = "QF101", FlightLabel = "A", DepartureAirport = "BNE", ArrivalAirport = "SYD" });
            db.Bags.Add(new Bag { BagId = 1, FlightId = 1, Tag = "TAG-AUDIT-1", HandlingType = "Standard", Outcome = "Pending" });
            await db.SaveChangesAsync();

            var auditLog = new AuditLogService(db);
            var verificationService = new BagVerificationService(db, auditLog);

            var result = await verificationService.ConfirmLoadedAsync(1, "TAG-AUDIT-1", "Test Handler", "BG_Handler");

            Assert.Equal(BagVerificationOutcome.Verified, result.Outcome);

            var entries = await auditLog.GetEntriesAsync(flightId: 1);
            Assert.Contains(entries, e => e.Action == "BagLoaded" && e.BagTag == "TAG-AUDIT-1" && e.PerformedBy == "Test Handler");
        }
    }

    [Fact]
    public async Task ExceptionService_ReportAsync_CreatesAuditLogEntry()
    {
        var (db, connection) = CreateContext();
        using (connection)
        using (db)
        {
            db.Flights.Add(new Flight { FlightId = 1, FlightNumber = "QF102", FlightLabel = "A", DepartureAirport = "BNE", ArrivalAirport = "MEL" });
            db.Bags.Add(new Bag { BagId = 1, FlightId = 1, Tag = "TAG-EX-1", HandlingType = "Standard", Outcome = "Pending" });
            await db.SaveChangesAsync();

            var auditLog = new AuditLogService(db);
            var exceptionService = new ExceptionService(db, auditLog);

            var result = await exceptionService.ReportAsync(1, "TAG-EX-1", "DamagedBag", "Handle snapped", "Reporter Name");

            Assert.True(result.Success);

            var entries = await auditLog.GetEntriesAsync(flightId: 1);
            Assert.Contains(entries, e => e.Action == "ExceptionRaised" && e.BagTag == "TAG-EX-1" && e.PerformedBy == "Reporter Name");
        }
    }

    [Fact]
    public async Task SupervisorReviewService_ResolveAsync_CreatesAuditLogEntry()
    {
        var (db, connection) = CreateContext();
        using (connection)
        using (db)
        {
            db.Flights.Add(new Flight { FlightId = 1, FlightNumber = "QF103", FlightLabel = "A", DepartureAirport = "BNE", ArrivalAirport = "PER" });
            db.Bags.Add(new Bag { BagId = 1, FlightId = 1, Tag = "TAG-REV-1", HandlingType = "Standard", Outcome = "Pending" });
            db.Exceptions.Add(new BaggageException
            {
                ExceptionId = 10,
                BagId = 1,
                FlightId = 1,
                Category = "DamagedBag",
                Description = "Torn strap",
                Status = "Open",
                ReportedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var auditLog = new AuditLogService(db);
            var reviewService = new SupervisorReviewService(db, auditLog);

            var result = await reviewService.ResolveAsync(10, "Repackaged and cleared for loading", "Supervisor Sarah");

            Assert.True(result.Success);

            var entries = await auditLog.GetEntriesAsync(flightId: 1);
            Assert.Contains(entries, e => e.Action == "ExceptionResolved" && e.PerformedBy == "Supervisor Sarah");
        }
    }
}
