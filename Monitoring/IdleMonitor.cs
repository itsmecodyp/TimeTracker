using System.Runtime.InteropServices;

namespace ActivityTracker.Monitoring;

public static class IdleMonitor
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(
        ref LASTINPUTINFO plii);

    public static TimeSpan GetIdleTime()
    {
        var info = new LASTINPUTINFO
        {
            cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>()
        };

        if (!GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        uint currentTick = unchecked((uint)Environment.TickCount);

        uint idleMilliseconds =
            unchecked(currentTick - info.dwTime);

        return TimeSpan.FromMilliseconds(idleMilliseconds);
    }

    public static bool IsIdle(TimeSpan threshold)
    {
        return GetIdleTime() >= threshold;
    }
}