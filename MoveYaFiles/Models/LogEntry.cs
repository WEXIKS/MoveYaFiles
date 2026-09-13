using System;

namespace MoveYaFiles.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string RuleName { get; set; } = string.Empty;
    public int FilesMoved { get; set; }
    public string Status { get; set; } = "OK"; // "OK", "Warning", "Error"
    public string Message { get; set; } = string.Empty;

    public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
}