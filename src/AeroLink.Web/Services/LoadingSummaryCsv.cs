using System.Globalization;
using System.Text;
using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>Builds the downloadable CSV copy of a flight's saved final loading summary.</summary>
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
