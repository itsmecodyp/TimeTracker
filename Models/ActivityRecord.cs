namespace ActivityTracker.Models;

public class ActivityRecord
{
    public DateTime Start { get; set; }

    public DateTime End { get; set; }

    public string Process { get; set; } = "";

    public bool IsActive { get; set; }
}