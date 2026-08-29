using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ActivityTracker.Monitoring;

public class ActiveWindow
{
    public string Process { get; set; } = "";
}

public static class WindowMonitor
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(
        IntPtr hWnd,
        out uint processId);

    public static ActiveWindow? GetActiveWindow()
    {
        IntPtr handle =
            GetForegroundWindow();

        if (handle == IntPtr.Zero)
            return null;

        GetWindowThreadProcessId(
            handle,
            out uint processId);

        string processName = "Unknown";

        try
        {
            using Process process =
                Process.GetProcessById(
                    (int)processId);

            processName =
                process.ProcessName;
        }
        catch
        {
        }

        return new ActiveWindow
        {
            Process = processName
        };
    }
}