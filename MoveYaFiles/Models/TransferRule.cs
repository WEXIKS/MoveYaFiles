using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MoveYaFiles.Models;

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
    
    // <-- Tutaj dodaj te pola:
    public long MinFileSizeBytes { get; set; } = 0;
    public long MaxFileSizeBytes { get; set; } = long.MaxValue;
    
    public string ConflictStrategy { get; set; } = "AddTimestamp";

    public string FileExtensions { get; set; } = string.Empty;
    public string CustomSuffix { get; set; } = "_copy";

    public int IntervalMinutes { get; set; } = 30;
    public string DisplayName => string.IsNullOrWhiteSpace(SourcePath) ? "New Rule" : SourcePath;
}