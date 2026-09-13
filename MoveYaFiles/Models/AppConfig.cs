using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace  MoveYaFiles.Models;

public class TransferRule : ObservableObject
{
    private string _sourcePath = "";
    public string SourcePath
    {
        get => _sourcePath;
        set
        {
            if (SetProperty(ref _sourcePath, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }
 
    public string DestinationPath { get; set; } = "";
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public long MinFileSizeBytes { get; set; } = 0; //2 GB = 2147483648 bajtów
    public long MaxFileSizeBytes { get; set; } = long.MaxValue;
    public string ConflictStrategy { get; set; } = "AddTimestamp";

    public string FileExtensions { get; set; } = string.Empty;
    public string CustomSuffix { get; set; } = "_copy";

    public int IntervalMinutes { get; set; } = 30;
    public string DisplayName => string.IsNullOrWhiteSpace(SourcePath) ? "New Rule" : SourcePath;
}

public class AppConfig
{
    public string AllowedExtensions { get; set; } = "";
    public int IntervalMinutes { get; set; } = 30;
    public bool AutoStartWithSystem { get; set; } = false;
    public DateTime? LastRun { get; set; }
    public List<TransferRule> Rules { get; set; } = new();
}

