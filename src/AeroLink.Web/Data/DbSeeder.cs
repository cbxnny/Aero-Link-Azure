using System.Text.Json;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Data;

/// <summary>
/// Loads the supplied baggage manifest dataset (originally
/// IAB251_Synthetic_Baggage_Manifest.xlsx, exported to JSON) into the
/// SQLite database on first run.
/// </summary>
public static class DbSeeder
{
    private sealed class SeedFlight
    {
        public int FlightId { get; set; }
        public string FlightLabel { get; set; } = string.Empty;
        public string FlightNumber { get; set; } = string.Empty;
        public string DepartureAirport { get; set; } = string.Empty;
        public string ArrivalAirport { get; set; } = string.Empty;
        public DateTime ScheduledDepartureUtc { get; set; }
        public string AircraftId { get; set; } = string.Empty;
        public string LoadingStatus { get; set; } = "NotStarted";
    }

    private sealed class SeedBag
    {
        public int BagId { get; set; }
        public int FlightId { get; set; }
        public string Tag { get; set; } = string.Empty;
        public string HandlingType { get; set; } = "Standard";
        public string HandlingInstruction { get; set; } = string.Empty;
        public string Outcome { get; set; } = "Pending";
    }

    private sealed class SeedRoot
    {
        public List<SeedFlight> Flights { get; set; } = new();
        public List<SeedBag> Bags { get; set; } = new();
    }

    /// <summary>
    /// Applies pending migrations and seeds flights and bags from
    /// Data/Seed/baggage-manifest.json when the database is empty.
    /// </summary>
    public static async Task SeedAsync(AeroLinkDbContext db, string contentRootPath)
    {
        await db.Database.EnsureCreatedAsync();

        // Ensure Exceptions table exists in existing databases without requiring DB deletion
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Exceptions"" (
                ""ExceptionId"" INTEGER NOT NULL CONSTRAINT ""PK_Exceptions"" PRIMARY KEY AUTOINCREMENT,
                ""BagId"" INTEGER NOT NULL,
                ""FlightId"" INTEGER NOT NULL DEFAULT 0,
                ""Category"" TEXT NOT NULL DEFAULT 'Other',
                ""Description"" TEXT NOT NULL DEFAULT '',
                ""Status"" TEXT NOT NULL DEFAULT 'Open',
                ""ReportedAtUtc"" TEXT NOT NULL,
                ""ReportedByEmployeeName"" TEXT NULL,
                ""DecisionReason"" TEXT NULL,
                ""DecidedAtUtc"" TEXT NULL,
                ""DecidedByEmployeeName"" TEXT NULL,
                CONSTRAINT ""FK_Exceptions_Bags_BagId"" FOREIGN KEY (""BagId"") REFERENCES ""Bags"" (""BagId"") ON DELETE CASCADE
            );
        ");

        if (db.Flights.Any())
        {
            return;
        }

        var seedPath = Path.Combine(contentRootPath, "Data", "Seed", "baggage-manifest.json");
        if (!File.Exists(seedPath))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(seedPath);
        var seed = JsonSerializer.Deserialize<SeedRoot>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (seed is null)
        {
            return;
        }

        var flights = seed.Flights.Select(f => new Flight
        {
            FlightId = f.FlightId,
            FlightLabel = f.FlightLabel,
            FlightNumber = f.FlightNumber,
            DepartureAirport = f.DepartureAirport,
            ArrivalAirport = f.ArrivalAirport,
            ScheduledDepartureUtc = f.ScheduledDepartureUtc,
            AircraftId = f.AircraftId,
            LoadingStatus = f.LoadingStatus
        });

        var bags = seed.Bags.Select(b => new Bag
        {
            BagId = b.BagId,
            FlightId = b.FlightId,
            Tag = b.Tag,
            HandlingType = b.HandlingType,
            HandlingInstruction = b.HandlingInstruction,
            Outcome = b.Outcome
        });

        await db.Flights.AddRangeAsync(flights);
        await db.Bags.AddRangeAsync(bags);
        await db.SaveChangesAsync();
    }
}
