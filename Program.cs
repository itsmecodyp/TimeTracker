using ActivityTracker.Monitoring;
using ActivityTracker.Commands;
using ActivityTracker.Storage;
using ActivityTracker.Models;
using ActivityTracker.Analysis;

const int HeartbeatSeconds = 5;

TimeSpan idleThreshold =
    TimeSpan.FromMinutes(5);

string logFile =
    Path.Combine(
        AppContext.BaseDirectory,
        "activity.jsonl");

var logger =
    new ActivityLogger(logFile);

var analyzer =
    new ActivityAnalyzer(logger);

var commands =
    new CommandHandler(analyzer);

Console.WriteLine("Activity Tracker");
Console.WriteLine("================");
Console.WriteLine();
Console.WriteLine(
    $"Heartbeat: {HeartbeatSeconds}s");

Console.WriteLine(
    $"Idle threshold: {idleThreshold.TotalMinutes:0} minutes");

Console.WriteLine(
    $"Log file: {logFile}");

Console.WriteLine();
Console.WriteLine(
    "Type 'help' for commands.");
Console.WriteLine();

using CancellationTokenSource cancellation =
    new();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

Task heartbeatTask =
    RunHeartbeatAsync(
        logger,
        HeartbeatSeconds,
        idleThreshold,
        cancellation.Token);

Task commandTask =
    RunCommandsAsync(
        commands,
        cancellation.Token);

await Task.WhenAny(
    heartbeatTask,
    commandTask);

cancellation.Cancel();

try
{
    await Task.WhenAll(
        heartbeatTask,
        commandTask);
}
catch (OperationCanceledException)
{
}

Console.WriteLine();
Console.WriteLine("Activity tracker stopped.");

static async Task RunHeartbeatAsync(
    ActivityLogger logger,
    int heartbeatSeconds,
    TimeSpan idleThreshold,
    CancellationToken token)
{
    ActivityRecord? currentPeriod = null;

    using PeriodicTimer timer =
        new(TimeSpan.FromSeconds(heartbeatSeconds));

    // Perform an immediate check on startup.
    currentPeriod =
        await CheckHeartbeatAsync(
            logger,
            currentPeriod,
            idleThreshold);

    try
    {
        while (await timer.WaitForNextTickAsync(token))
        {
            currentPeriod =
                await CheckHeartbeatAsync(
                    logger,
                    currentPeriod,
                    idleThreshold);
        }
    }
    catch (OperationCanceledException)
    {
        // Expected when the application shuts down.
    }

    // Close the final open period.
    if (currentPeriod != null)
    {
        currentPeriod.End = DateTime.Now;

        if (currentPeriod.End > currentPeriod.Start)
        {
            await logger.LogAsync(
                currentPeriod);
        }
    }
}

static async Task<ActivityRecord?> CheckHeartbeatAsync(
    ActivityLogger logger,
    ActivityRecord? currentPeriod,
    TimeSpan idleThreshold)
{
    ActiveWindow? window =
        WindowMonitor.GetActiveWindow();

    if (window == null)
        return currentPeriod;

    bool isIdle =
        IdleMonitor.IsIdle(idleThreshold);

    string process =
        window.Process;

    DateTime now =
        DateTime.Now;

    bool isActive =
        !isIdle;

    // First heartbeat.
    if (currentPeriod == null)
    {
        return new ActivityRecord
        {
            Start = now,
            End = now,
            Process = process,
            IsActive = isActive
        };
    }

    bool stateChanged =
        !currentPeriod.Process.Equals(
            process,
            StringComparison.OrdinalIgnoreCase)
        ||
        currentPeriod.IsActive != isActive;

    if (!stateChanged)
    {
        // Nothing changed. Keep the existing period open.
        return currentPeriod;
    }

    // The application or idle state changed.
    currentPeriod.End = now;

    if (currentPeriod.End > currentPeriod.Start)
    {
        await logger.LogAsync(
            currentPeriod);
    }

    /* log focus/status change, not needed
    Console.WriteLine(
        $"[{now:HH:mm:ss}] " +
        $"{(isActive ? "ACTIVE" : "IDLE")} " +
        $"{process}");
    */

    return new ActivityRecord
    {
        Start = now,
        End = now,
        Process = process,
        IsActive = isActive
    };
}

static async Task RunCommandsAsync(
    CommandHandler commands,
    CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        string? input =
            await Console.In.ReadLineAsync(token);

        if (input == null)
            break;

        bool keepRunning =
            await commands.ExecuteAsync(input);

        if (!keepRunning)
            return;
    }
}