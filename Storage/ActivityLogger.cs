using System.Text.Json;
using ActivityTracker.Models;

namespace ActivityTracker.Storage;

public class ActivityLogger
{
    private readonly string _filePath;

    // True until the first record of each program session
    // has been written.
    private bool _firstRecord = true;

    public ActivityLogger(string filePath)
    {
        _filePath = filePath;
    }

    public async Task LogAsync(
        ActivityRecord record)
    {
        long duration =
            Math.Max(
                0,
                (long)(record.End - record.Start)
                    .TotalSeconds);

        if (_firstRecord)
        {
            long timestamp =
                new DateTimeOffset(record.Start)
                    .ToUnixTimeSeconds();

            var data = new
            {
                t = timestamp,
                d = duration,
                p = record.Process,
                a = record.IsActive ? 1 : 0
            };

            _firstRecord = false;

            await AppendAsync(data);

            return;
        }

        var subsequentData = new
        {
            d = duration,
            p = record.Process,
            a = record.IsActive ? 1 : 0
        };

        await AppendAsync(subsequentData);
    }

    private async Task AppendAsync(object data)
    {
        string json =
            JsonSerializer.Serialize(data);

        await File.AppendAllTextAsync(
            _filePath,
            json + Environment.NewLine);
    }

    public async Task<List<ActivityRecord>>
        LoadAsync()
    {
        var records =
            new List<ActivityRecord>();

        if (!File.Exists(_filePath))
            return records;

        await using FileStream stream =
            File.OpenRead(_filePath);

        using StreamReader reader =
            new(stream);

        DateTime? currentTime = null;

        while (await reader.ReadLineAsync()
               is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(line);

                JsonElement root =
                    document.RootElement;

                long duration =
                    root.GetProperty("d")
                        .GetInt64();

                string process =
                    root.GetProperty("p")
                        .GetString() ?? "";

                bool isActive =
                    root.GetProperty("a")
                        .GetInt32() == 1;

                // A 't' property means this is the
                // beginning of a new program session.
                if (root.TryGetProperty(
                        "t",
                        out JsonElement timestampElement))
                {
                    long timestamp =
                        timestampElement.GetInt64();

                    currentTime =
                        DateTimeOffset
                            .FromUnixTimeSeconds(timestamp)
                            .LocalDateTime;
                }

                // We cannot interpret a duration without
                // knowing the current timestamp.
                if (currentTime == null)
                    continue;

                DateTime start =
                    currentTime.Value;

                DateTime end =
                    start.AddSeconds(duration);

                records.Add(
                    new ActivityRecord
                    {
                        Start = start,
                        End = end,
                        Process = process,
                        IsActive = isActive
                    });

                // The next record starts where this one ended.
                currentTime = end;
            }
            catch (JsonException)
            {
                // Ignore malformed records.
            }
        }

        return records;
    }
}