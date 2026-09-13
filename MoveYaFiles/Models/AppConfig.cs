using System;
using System.Collections.Generic;

namespace MoveYaFiles.Models;

public class AppConfig
{
    // Globalne ustawienia aplikacji
    public string SelectedInterval { get; set; } = "30 minutes";
    public bool AutoStartWithSystem { get; set; } = false;
    public DateTime? LastRun { get; set; }

    // Lista aktywnych reguł
    public List<TransferRule> Rules { get; set; } = new();

    // Rejestr / Historia synchronizacji
    public List<LogEntry> Logs { get; set; } = new();
}