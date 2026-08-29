using System.Text;
using ActivityTracker.Analysis;

namespace ActivityTracker.Storage;

public static class CsvExporter
{
    public static async Task<string>
     ExportApplicationAsync(
         string process,
         TimeSpan period,
         List<ActivityPeriod> periods,
         TimeSpan totalActive,
         TimeSpan averageActivePerDay)
    {
        string safeProcess =
            string.Concat(
                process.Select(c =>
                    Path.GetInvalidFileNameChars()
                        .Contains(c)
                        ? '_'
                        : c));

        string periodText =
            FormatPeriod(period);

        string totalText =
            FormatFileDuration(totalActive);

        string averageText =
            FormatFileDuration(averageActivePerDay);

        string fileName =
            $"{safeProcess}_" +
            $"Last{periodText}_" +
            $"Total{totalText}_" +
            $"Avg{averageText}.csv";

        string filePath =
            Path.Combine(
                AppContext.BaseDirectory,
                fileName);

        var builder =
            new StringBuilder();

        builder.AppendLine(
            "Start,End,Process,Duration");

        foreach (ActivityPeriod activity in periods)
        {
            builder.AppendLine(
                $"{activity.Start:yyyy-MM-dd HH:mm:ss}," +
                $"{activity.End:yyyy-MM-dd HH:mm:ss}," +
                $"{Escape(activity.Process)}," +
                $"{FormatDuration(activity.Duration)}");
        }

        await File.WriteAllTextAsync(
            filePath,
            builder.ToString());

        return filePath;
    }

    private static string FormatPeriod(
    TimeSpan period)
    {
        if (period.TotalDays >= 1)
            return $"{period.TotalDays:0.#}d";

        if (period.TotalHours >= 1)
            return $"{period.TotalHours:0.#}h";

        return $"{period.TotalMinutes:0.#}m";
    }

    private static string FormatFileDuration(
        TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h" +
                   $"{duration.Minutes:00}m";
        }

        return $"{duration.Minutes}m";
    }

    private static string FormatDuration(
        TimeSpan duration)
    {
        return $"{(int)duration.TotalHours:00}:" +
               $"{duration.Minutes:00}:" +
               $"{duration.Seconds:00}";
    }

    private static string Escape(
        string value)
    {
        if (!value.Contains(',') &&
            !value.Contains('"') &&
            !value.Contains('\n') &&
            !value.Contains('\r'))
        {
            return value;
        }

        return "\"" +
               value.Replace(
                   "\"",
                   "\"\"") +
               "\"";
    }
}