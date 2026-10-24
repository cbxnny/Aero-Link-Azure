using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Xunit;

namespace AeroLink.Tests;

/// <summary>Tests for the CSV download of a saved final loading summary.</summary>
public class LoadingSummaryCsvTests
{
    private static LoadingCompletion Completion() => new()
    {
        FlightId = 1,
        CompletedAtUtc = new DateTime(2026, 10, 21, 3, 4, 5, DateTimeKind.Utc),
        CompletedByEmployeeId = "EMP-1001",
        CompletedByName = "Sarah Chen"
    };

    private static LoadingSummary Summary(params LoadingExceptionSummary[] exceptions) => new(
        1, "DEMO101", "BNE", "SYD", new DateTime(2026, 10, 21, 8, 30, 0, DateTimeKind.Utc), "VH-ABC",
        2, 1, 1,
        new List<LoadingBagSummary>
        {
            new("BAG-1", "Standard", "Loaded", new DateTime(2026, 10, 21, 2, 0, 0, DateTimeKind.Utc)),
            new("BAG-2", "Fragile", "ApprovedNotToLoad", null)
        },
        exceptions.ToList());

    [Fact]
    public void Build_IncludesFlightDetailsAndEveryBag()
    {
        var lines = LoadingSummaryCsv.Build(Summary(), Completion()).Split("\r\n");

        Assert.Contains("Flight,DEMO101", lines);
        Assert.Contains("Route,BNE to SYD", lines);
        Assert.Contains("Completed by,Sarah Chen (EMP-1001)", lines);
        Assert.Contains("Expected bags,2", lines);
        Assert.Contains("BAG-1,Standard,Loaded,2026-10-21 02:00:00", lines);
        Assert.Contains("BAG-2,Fragile,Approved not to load,", lines);
    }

    [Fact]
    public void Build_UsesCrLfLineEndings()
    {
        var csv = LoadingSummaryCsv.Build(Summary(), Completion());

        Assert.EndsWith("\r\n", csv);
        Assert.DoesNotContain("\n", csv.Replace("\r\n", string.Empty));
    }

    [Fact]
    public void Build_OmitsExceptionSectionWhenThereAreNone()
    {
        var csv = LoadingSummaryCsv.Build(Summary(), Completion());

        Assert.DoesNotContain("Problem category", csv);
    }

    [Fact]
    public void Build_IncludesExceptionDecisions()
    {
        var exception = new LoadingExceptionSummary(7, "BAG-2", "Damaged", "ApprovedNotToLoad",
            "Strap broken", "Unsafe to fly", "Sarah Chen", new DateTime(2026, 10, 21, 2, 30, 0, DateTimeKind.Utc));

        var lines = LoadingSummaryCsv.Build(Summary(exception), Completion()).Split("\r\n");

        Assert.Contains("BAG-2,Damaged,Strap broken,Approved not to load,Unsafe to fly,Sarah Chen,2026-10-21 02:30:00", lines);
    }

    [Fact]
    public void Build_QuotesCellsWithCommasQuotesAndLineBreaks()
    {
        var exception = new LoadingExceptionSummary(7, "BAG-2", "Damaged", "Resolved",
            "He said \"wet\", then left\nthe bag", "Checked", "Sarah Chen", null);

        var csv = LoadingSummaryCsv.Build(Summary(exception), Completion());

        Assert.Contains("\"He said \"\"wet\"\", then left\nthe bag\"", csv);
    }

    [Fact]
    public void Build_DefusesTextThatSpreadsheetsWouldRunAsFormulas()
    {
        var exception = new LoadingExceptionSummary(7, "BAG-2", "Other", "Resolved",
            "@SUM(A1)", "=1+1", "Sarah Chen", null);

        var csv = LoadingSummaryCsv.Build(Summary(exception), Completion());

        Assert.Contains(",'@SUM(A1),", csv);
        Assert.Contains(",'=1+1,", csv);
        Assert.DoesNotContain(",=1+1", csv);
        Assert.DoesNotContain(",@SUM", csv);
    }

    [Fact]
    public void Build_WithLoadingReadiness_ExportsUncompletedFlight()
    {
        var flight = new Flight
        {
            FlightId = 2,
            FlightNumber = "DEMO202",
            DepartureAirport = "BNE",
            ArrivalAirport = "MEL",
            AircraftId = "VH-XYZ",
            ScheduledDepartureUtc = new DateTime(2026, 10, 21, 10, 0, 0, DateTimeKind.Utc),
            LoadingStatus = "InProgress",
            Bags = new List<Bag>
            {
                new() { BagId = 1, Tag = "BAG-A", HandlingType = "Standard", Outcome = "Loaded", LoadedAtUtc = new DateTime(2026, 10, 21, 9, 30, 0, DateTimeKind.Utc) },
                new() { BagId = 2, Tag = "BAG-B", HandlingType = "Fragile", Outcome = "Pending" }
            }
        };

        var readiness = new LoadingReadiness
        {
            Flight = flight,
            Exceptions = new List<BaggageException>
            {
                new()
                {
                    ExceptionId = 1,
                    BagId = 2,
                    Bag = flight.Bags[1],
                    Category = "DamagedBag",
                    Description = "Broken zipper",
                    Status = "Open"
                }
            }
        };

        var csv = LoadingSummaryCsv.Build(readiness);
        var lines = csv.Split("\r\n");

        Assert.Contains("Flight,DEMO202", lines);
        Assert.Contains("Loading status,InProgress", lines);
        Assert.Contains("Expected bags,2", lines);
        Assert.Contains("Loaded bags,1", lines);
        Assert.Contains("Outstanding bags,1", lines);
        Assert.Contains("Open exceptions,1", lines);
        Assert.Contains("BAG-A,Standard,Loaded,2026-10-21 09:30:00", lines);
        Assert.Contains("BAG-B,Fragile,Pending,", lines);
        Assert.Contains("BAG-B,DamagedBag,Broken zipper,Open,,,", lines);
    }

    [Fact]
    public void BuildAll_ExportsAllFlightsLoadingRecords()
    {
        var flights = new List<Flight>
        {
            new()
            {
                FlightId = 1,
                FlightNumber = "QF100",
                DepartureAirport = "BNE",
                ArrivalAirport = "SYD",
                AircraftId = "VH-AAA",
                LoadingStatus = "InProgress",
                ScheduledDepartureUtc = new DateTime(2026, 10, 21, 8, 0, 0, DateTimeKind.Utc),
                Bags = new List<Bag>
                {
                    new() { Tag = "QF-1", HandlingType = "Standard", Outcome = "Loaded", LoadedAtUtc = new DateTime(2026, 10, 21, 7, 50, 0, DateTimeKind.Utc) }
                }
            },
            new()
            {
                FlightId = 2,
                FlightNumber = "QF200",
                DepartureAirport = "BNE",
                ArrivalAirport = "MEL",
                AircraftId = "VH-BBB",
                LoadingStatus = "NotStarted",
                ScheduledDepartureUtc = new DateTime(2026, 10, 21, 9, 0, 0, DateTimeKind.Utc),
                Bags = new List<Bag>
                {
                    new() { Tag = "QF-2", HandlingType = "Fragile", Outcome = "Pending" }
                }
            }
        };

        var csv = LoadingSummaryCsv.BuildAll(flights);
        var lines = csv.Split("\r\n");

        Assert.Contains("Flight,Route,Aircraft,Scheduled departure (UTC),Loading status,Bag tag,Handling category,Special handling,Outcome,Loaded at (UTC)", lines);
        Assert.Contains("QF100,BNE to SYD,VH-AAA,2026-10-21 08:00:00,InProgress,QF-1,Standard,No,Loaded,2026-10-21 07:50:00", lines);
        Assert.Contains("QF200,BNE to MEL,VH-BBB,2026-10-21 09:00:00,NotStarted,QF-2,Fragile,Yes,Pending,", lines);
    }
}

