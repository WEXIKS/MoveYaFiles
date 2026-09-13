using System;
using System.Collections.Generic;

namespace  MoveYaFiles.Models;

public class TransferRule
{
    public string SourcePath { get; set; } = "";
    public string DestinationPath { get; set; } = "";
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public long MinFileSizeBytes { get; set; } = 0; //2 GB = 2147483648 bajtów
    public long MaxFileSizeBytes { get; set; } = long.MaxValue;
    public string ConflictStrategy { get; set; } = "AddTimestamp";

    public string FileExtensions { get; set; } = string.Empty;
    public string CustomSuffix { get; set; } = "_copy";
}

public class AppConfig
{
    public int IntervalMinutes { get; set; } = 30;
    public bool AutoStartWithSystem { get; set; } = false;
    public DateTime? LastRun { get; set; }
    public List<TransferRule> Rules { get; set; } = new();
}

