using ActivityTracker.Analysis;
using ActivityTracker.Storage;

namespace ActivityTracker.Commands;

public class CommandHandler
{
    private readonly ActivityAnalyzer _analyzer;

    public CommandHandler(ActivityAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public async Task<bool> ExecuteAsync(string input)
    {
        string[] parts = input
            .Trim()
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return true;

        string command = parts[0].ToLowerInvariant();

        switch (command)
        {
            case "help":
                PrintHelp();
                return true;

            case "stats":
            case "apps":
                await StatsAsync(parts);
                return true;

            case "app":
                await AppAsync(parts);
                return true;

            case "timeline":
                await TimelineAsync(parts);
                return true;

            case "quit":
            case "exit":
                return false;

            default:
                Console.WriteLine(
                    $"Unknown command: {command}");

                Console.WriteLine(
                    "Type 'help' for available commands.");

                return true;
        }
    }

    private async Task StatsAsync(string[] parts)
    {
        TimeSpan period = ParsePeriod(
            parts.Length > 1 ? parts[1] : "24h");

        List<ApplicationUsage> usage =
            await _analyzer.GetUsageAsync(period);

        Console.WriteLine();
        Console.WriteLine(
            $"Application Usage — Last {FormatPeriod(period)}");

        Console.WriteLine(
            "----------------------------------------");

        if (usage.Count == 0)
        {
            Console.WriteLine("No activity recorded.");
            Console.WriteLine();
            return;
        }

        foreach (ApplicationUsage item in usage)
        {
            Console.WriteLine(
                $"{item.Process,-25} {FormatDuration(item.Duration)}");
        }

        Console.WriteLine();
    }

    private async Task AppAsync(string[] parts)
    {
        if (parts.Length < 2)
        {
            Console.WriteLine(
                "Usage: app <process> [period]");

            return;
        }

        string process = parts[1];

        TimeSpan period =
            parts.Length > 2
                ? ParsePeriod(parts[2])
                : TimeSpan.FromHours(24);

        ApplicationSummary? summary =
            await _analyzer.GetApplicationSummaryAsync(
                process,
                period);

        if (summary == null)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"No activity found for '{process}'.");
            Console.WriteLine();

            return;
        }

        Console.WriteLine();

        Console.WriteLine(
            $"{process} — Last {FormatPeriod(period)}");

        Console.WriteLine(
            "========================================");

        Console.WriteLine(
            $"Total time:          " +
            $"{FormatDuration(summary.TotalTime)}");

        Console.WriteLine(
            $"Total active:        " +
            $"{FormatDuration(summary.ActiveTime)}");

        Console.WriteLine(
            $"Total idle:          " +
            $"{FormatDuration(summary.IdleTime)}");

        Console.WriteLine(
            $"Average active/day:  " +
            $"{FormatDuration(TimeSpan.FromSeconds(
                summary.AverageActivePerDay))}");

        Console.WriteLine(
            $"Days in period:      " +
            $"{summary.DaysInPeriod}");

        List<ActivityPeriod> periods =
            await _analyzer.GetActivePeriodsAsync(
                process,
                period);

        string csvPath =
            await CsvExporter.ExportApplicationAsync(
                process,
                period,
                periods,
                summary.ActiveTime,
                TimeSpan.FromSeconds(
                    summary.AverageActivePerDay));

        Console.WriteLine();

        Console.WriteLine(
            $"Active periods:      {periods.Count}");

        Console.WriteLine(
            $"CSV exported:        {csvPath}");

        Console.WriteLine();
    }
    private async Task TimelineAsync(string[] parts)
    {
        TimeSpan period = ParsePeriod(
            parts.Length > 1 ? parts[1] : "1h");

        List<ActivityTracker.Models.ActivityRecord> records =
            await _analyzer.GetTimelineAsync(period);

        Console.WriteLine();
        Console.WriteLine(
            $"Timeline — Last {FormatPeriod(period)}");

        Console.WriteLine(
            "----------------------------------------");

        foreach (var record in records)
        {
            Console.WriteLine(
                $"{record.Start:HH:mm:ss} - " +
                $"{record.End:HH:mm:ss} | " +
                $"{record.Process,-20} | " +
                $"{(record.IsActive ? "ACTIVE" : "IDLE")}");
        }

        Console.WriteLine();
    }

    private static TimeSpan ParsePeriod(string value)
    {
        value = value.Trim().ToLowerInvariant();

        if (value.EndsWith("m") &&
            double.TryParse(
                value[..^1],
                out double minutes))
        {
            return TimeSpan.FromMinutes(minutes);
        }

        if (value.EndsWith("h") &&
            double.TryParse(
                value[..^1],
                out double hours))
        {
            return TimeSpan.FromHours(hours);
        }

        if (value.EndsWith("d") &&
            double.TryParse(
                value[..^1],
                out double days))
        {
            return TimeSpan.FromDays(days);
        }

        Console.WriteLine(
            $"Invalid period '{value}', using 24h.");

        return TimeSpan.FromHours(24);
    }

    private static string FormatPeriod(TimeSpan period)
    {
        if (period.TotalDays >= 1)
            return $"{period.TotalDays:0.#}d";

        if (period.TotalHours >= 1)
            return $"{period.TotalHours:0.#}h";

        return $"{period.TotalMinutes:0.#}m";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return $"{(int)duration.TotalDays}d " +
                   $"{duration.Hours}h " +
                   $"{duration.Minutes}m";
        }

        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h " +
                   $"{duration.Minutes}m";
        }

        if (duration.TotalMinutes >= 1)
        {
            return $"{duration.Minutes}m " +
                   $"{duration.Seconds}s";
        }

        return $"{duration.Seconds}s";
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
        Commands:

          stats [period]          Application usage
          apps [period]           Same as stats

          app <name> [period] [--private]    Usage of one application

          timeline [period]       Show activity timeline

          help                    Show this help
          quit                    Exit the program

        Period examples:

          30m                     30 minutes
          1h                      1 hour
          6h                      6 hours
          24h                     24 hours
          7d                      7 days

        Examples:

          stats 1h
          stats 24h
          stats 7d

          app chrome 24h
          app devenv 7d
          app chrome 7d --private

          timeline 2h
        """);
    }
}