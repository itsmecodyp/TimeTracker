using ActivityTracker.Models;
using ActivityTracker.Storage;

namespace ActivityTracker.Analysis;

public class ApplicationUsage
{
    public string Process { get; set; } = "";

    public TimeSpan Duration { get; set; }

    public TimeSpan ActiveDuration { get; set; }

    public TimeSpan IdleDuration { get; set; }
}

public class ApplicationSummary
{
    public string Process { get; set; } = "";

    public TimeSpan TotalTime { get; set; }

    public TimeSpan ActiveTime { get; set; }

    public TimeSpan IdleTime { get; set; }

    public double AverageActivePerDay { get; set; }

    public int DaysInPeriod { get; set; }
}

public class ActivityPeriod
{
    public DateTime Start { get; set; }

    public DateTime End { get; set; }

    public string Process { get; set; } = "";

    public TimeSpan Duration =>
        End - Start;
}

public class ActivityAnalyzer
{
    private readonly ActivityLogger _logger;

    public ActivityAnalyzer(
        ActivityLogger logger)
    {
        _logger = logger;
    }

    public async Task<List<ActivityRecord>>
        GetApplicationRecordsAsync(
            string process,
            TimeSpan period)
    {
        List<ActivityRecord> records =
            await _logger.LoadAsync();

        DateTime cutoff =
            DateTime.Now - period;

        return records
            .Where(x =>
                x.End >= cutoff &&
                x.Process.Equals(
                    process,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Start)
            .ToList();
    }

    public async Task<ApplicationSummary?>
        GetApplicationSummaryAsync(
            string process,
            TimeSpan period)
    {
        List<ActivityRecord> records =
            await GetApplicationRecordsAsync(
                process,
                period);

        if (records.Count == 0)
            return null;

        DateTime cutoff =
            DateTime.Now - period;

        TimeSpan active =
            TimeSpan.Zero;

        TimeSpan idle =
            TimeSpan.Zero;

        foreach (ActivityRecord record in records)
        {
            DateTime start =
                record.Start < cutoff
                    ? cutoff
                    : record.Start;

            DateTime end =
                record.End > DateTime.Now
                    ? DateTime.Now
                    : record.End;

            if (end <= start)
                continue;

            TimeSpan duration =
                end - start;

            if (record.IsActive)
                active += duration;
            else
                idle += duration;
        }

        return new ApplicationSummary
        {
            Process = process,
            TotalTime = active + idle,
            ActiveTime = active,
            IdleTime = idle,
            AverageActivePerDay =
                active.TotalSeconds /
                Math.Max(
                    1,
                    Math.Ceiling(period.TotalDays)),
            DaysInPeriod =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        period.TotalDays))
        };
    }

    public async Task<List<ApplicationUsage>>
        GetUsageAsync(
            TimeSpan period)
    {
        List<ActivityRecord> records =
            await _logger.LoadAsync();

        DateTime cutoff =
            DateTime.Now - period;

        var usage =
            new Dictionary<string, ApplicationUsage>(
                StringComparer.OrdinalIgnoreCase);

        foreach (ActivityRecord record in records)
        {
            if (record.End < cutoff)
                continue;

            DateTime start =
                record.Start < cutoff
                    ? cutoff
                    : record.Start;

            DateTime end =
                record.End > DateTime.Now
                    ? DateTime.Now
                    : record.End;

            if (end <= start)
                continue;

            TimeSpan duration =
                end - start;

            if (!usage.TryGetValue(
                    record.Process,
                    out ApplicationUsage? app))
            {
                app = new ApplicationUsage
                {
                    Process = record.Process
                };

                usage[record.Process] = app;
            }

            app.Duration += duration;

            if (record.IsActive)
                app.ActiveDuration += duration;
            else
                app.IdleDuration += duration;
        }

        return usage.Values
            .OrderByDescending(
                x => x.ActiveDuration)
            .ToList();
    }

    public async Task<List<ActivityPeriod>>
        GetActivePeriodsAsync(
            string process,
            TimeSpan period)
    {
        List<ActivityRecord> records =
            await GetApplicationRecordsAsync(
                process,
                period);

        DateTime cutoff =
            DateTime.Now - period;

        var periods =
            new List<ActivityPeriod>();

        foreach (ActivityRecord record in records)
        {
            if (!record.IsActive)
                continue;

            DateTime start =
                record.Start < cutoff
                    ? cutoff
                    : record.Start;

            DateTime end =
                record.End > DateTime.Now
                    ? DateTime.Now
                    : record.End;

            if (end <= start)
                continue;

            periods.Add(new ActivityPeriod
            {
                Start = start,
                End = end,
                Process = record.Process
            });
        }

        return periods;
    }

    public async Task<List<ActivityRecord>>
        GetTimelineAsync(
            TimeSpan period)
    {
        List<ActivityRecord> records =
            await _logger.LoadAsync();

        DateTime cutoff =
            DateTime.Now - period;

        return records
            .Where(x => x.End >= cutoff)
            .OrderBy(x => x.Start)
            .ToList();
    }
}