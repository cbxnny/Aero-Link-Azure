using System.Globalization;
using System.Text;
using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>Builds the downloadable CSV copy of a flight's saved final loading summary or current loading records.</summary>
public static class LoadingSummaryCsv
{
    private const string LineEnd = "\r\n";

    /// <summary>Returns the summary as CSV text: flight details, bag outcomes, then exception decisions.</summary>
    public static string Build(LoadingSummary summary, LoadingCompletion completion)
    {
        var sb = new StringBuilder();

        void Row(params string?[] cells) =>
            sb.Append(string.Join(',', cells.Select(Escape))).Append(LineEnd);

        Row("Flight", summary.FlightNumber);
        Row("Route", $"{summary.DepartureAirport} to {summary.ArrivalAirport}");
        Row("Aircraft", summary.AircraftId);
        Row("Scheduled departure (UTC)", Utc(summary.ScheduledDepartureUtc));
        Row("Completed (UTC)", Utc(completion.CompletedAtUtc));
        Row("Completed by", $"{completion.CompletedByName} ({completion.CompletedByEmployeeId})");
        Row("Expected bags", Number(summary.ExpectedCount));
        Row("Loaded bags", Number(summary.LoadedCount));
        Row("Approved not to load", Number(summary.ApprovedNotToLoadCount));
        Row();

        Row("Bag tag", "Handling category", "Final outcome", "Loaded at (UTC)");
        foreach (var bag in summary.Bags)
        {
            Row(bag.Tag, bag.HandlingType, Label(bag.Outcome), bag.LoadedAtUtc is { } loaded ? Utc(loaded) : "");
        }

        if (summary.Exceptions.Count > 0)
        {
            Row();
            Row("Bag tag", "Problem category", "Problem description", "Decision", "Reason", "Decided by", "Decided at (UTC)");
            foreach (var e in summary.Exceptions)
            {
                Row(e.Tag, e.Category, e.Description, Label(e.Status), e.DecisionReason, e.DecidedBy,
                    e.DecidedAtUtc is { } decided ? Utc(decided) : "");
            }
        }

        return sb.ToString();
    }

    /// <summary>Returns the live loading records for an in-progress or uncompleted flight as CSV text.</summary>
    public static string Build(LoadingReadiness readiness)
    {
        var sb = new StringBuilder();

        void Row(params string?[] cells) =>
            sb.Append(string.Join(',', cells.Select(Escape))).Append(LineEnd);

        Row("Flight", readiness.Flight.FlightNumber);
        Row("Route", $"{readiness.Flight.DepartureAirport} to {readiness.Flight.ArrivalAirport}");
        Row("Aircraft", readiness.Flight.AircraftId);
        Row("Scheduled departure (UTC)", Utc(readiness.Flight.ScheduledDepartureUtc));
        Row("Loading status", readiness.Flight.LoadingStatus);
        Row("Expected bags", Number(readiness.ExpectedCount));
        Row("Loaded bags", Number(readiness.LoadedCount));
        Row("Approved not to load", Number(readiness.ApprovedNotToLoadCount));
        Row("Outstanding bags", Number(readiness.OutstandingBags.Count));
        Row("Open exceptions", Number(readiness.OpenExceptions.Count));
        Row();

        Row("Bag tag", "Handling category", "Current outcome", "Loaded at (UTC)");
        foreach (var bag in readiness.Flight.Bags.OrderBy(b => b.Tag))
        {
            Row(bag.Tag, bag.HandlingType, Label(bag.Outcome), bag.LoadedAtUtc is { } loaded ? Utc(loaded) : "");
        }

        if (readiness.Exceptions.Count > 0)
        {
            Row();
            Row("Bag tag", "Problem category", "Problem description", "Status", "Decision reason", "Decided by", "Decided at (UTC)");
            foreach (var e in readiness.Exceptions)
            {
                Row(e.Bag?.Tag ?? $"Bag #{e.BagId}", e.Category, e.Description, Label(e.Status), e.DecisionReason, e.DecidedByEmployeeName,
                    e.DecidedAtUtc is { } decided ? Utc(decided) : "");
            }
        }

        return sb.ToString();
    }

    /// <summary>Returns all loading records across the specified flights as tabular CSV text.</summary>
    public static string BuildAll(IEnumerable<Flight> flights)
    {
        var sb = new StringBuilder();

        void Row(params string?[] cells) =>
            sb.Append(string.Join(',', cells.Select(Escape))).Append(LineEnd);

        Row("Flight", "Route", "Aircraft", "Scheduled departure (UTC)", "Loading status", "Bag tag", "Handling category", "Special handling", "Outcome", "Loaded at (UTC)");

        foreach (var flight in flights.OrderBy(f => f.ScheduledDepartureUtc))
        {
            var route = $"{flight.DepartureAirport} to {flight.ArrivalAirport}";
            var schedUtc = Utc(flight.ScheduledDepartureUtc);

            if (flight.Bags.Count == 0)
            {
                Row(flight.FlightNumber, route, flight.AircraftId, schedUtc, flight.LoadingStatus, "", "", "", "No bags", "");
                continue;
            }

            foreach (var bag in flight.Bags.OrderBy(b => b.Tag))
            {
                Row(flight.FlightNumber, route, flight.AircraftId, schedUtc, flight.LoadingStatus,
                    bag.Tag, bag.HandlingType, bag.IsSpecialHandling ? "Yes" : "No",
                    Label(bag.Outcome), bag.LoadedAtUtc is { } loaded ? Utc(loaded) : "");
            }
        }

        return sb.ToString();
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Utc(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private static string Label(string status) => status == "ApprovedNotToLoad" ? "Approved not to load" : status;

    /// <summary>Quotes a cell when needed and defuses text that a spreadsheet would run as a formula.</summary>
    private static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Length > 0 && "=+-@\t\r".Contains(text[0]))
        {
            text = "'" + text;
        }

        return text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
            ? "\"" + text.Replace("\"", "\"\"") + "\""
            : text;
    }
}
