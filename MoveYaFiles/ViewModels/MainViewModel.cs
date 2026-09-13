using System;
using System.Collections.Generic;
using Avalonia.Threading;
using MoveYaFiles.Services;
using System.Linq;

namespace MoveYaFiles.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly TransferEngine _engine = new();
    private string _statusMessage = "Ready to use";
    private string _lastRunText = "Never";
    private string _countdownText = "--:--";
    private string _sourcePath = string.Empty;
    private string _destinationPath = string.Empty;
    private DispatcherTimer? _timer;
    private int _secondsRemaining;
    private string _fileExtensions = string.Empty;
    private string _selectedConflictStrategy = "Skip";
    private string _customSuffix = "_copy";

    public List<string> ConflictStrategies { get; } = new()
    {
        "Skip",
        "Overwrite",
        "Timestamp (_yyyyMMdd_HHmmss)",
        "Own suffix"

    };

    public string SelectedConflictStrategy
    {
        get => _selectedConflictStrategy;
        set
        {
            if (SetProperty(ref _selectedConflictStrategy, value))
            {
                OnPropertyChanged(nameof(IsCustomSuffixEnabled));
                SaveCurrentPaths();
            }
        }
    }

    public string CustomSuffix
    {
        get => _customSuffix;
        set
        {
            if(SetProperty(ref _customSuffix, value)) SaveCurrentPaths();
        }
    }
    public bool IsCustomSuffixEnabled => SelectedConflictStrategy == "Own suffix";
    public string FileExtensions
    {
        get => _fileExtensions;
        set
        {
            if(SetProperty(ref _fileExtensions, value)) SaveCurrentPaths();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string LastRunText
    {
        get => _lastRunText;
        set => SetProperty(ref _lastRunText, value);
    }

    public string CountdownText
    {
        get => _countdownText;
        set => SetProperty(ref _countdownText, value);
    }

    public string SourcePath
    {
        get => _sourcePath;
        set
        {
            if (SetProperty(ref _sourcePath, value)) SaveCurrentPaths();
        }
    }

    public string DestinationPath
    {
        get => _destinationPath;
        set
        {
            if (SetProperty(ref _destinationPath, value)) SaveCurrentPaths();
        }
    }

    public MainViewModel()
    {
        LoadPathsFromConfig();
        RefreshLastRun();
        StartTimer();
    }

    private void LoadPathsFromConfig()
    {
        var config = _engine.LoadConfig();
        if (config.Rules.Count > 0)
        {
            _sourcePath = config.Rules[0].SourcePath;
            _destinationPath = config.Rules[0].DestinationPath;
            _fileExtensions = string.Join(", ", config.Rules[0].AllowedExtensions); 
            _selectedConflictStrategy = config.Rules[0].ConflictStrategy switch
            {
                "Overwrite" => "Overwrite",
                "AddTimestamp" => "Timestamp (_yyyyMMdd_HHmmss)",
                "CustomSuffix" => "Own suffix",
                _ => "Skip"
            };
            _customSuffix = string.IsNullOrEmpty(config.Rules[0].CustomSuffix) ? "_copy" : config.Rules[0].CustomSuffix;
            
        }
    }

    private void SaveCurrentPaths()
    {
        var config = _engine.LoadConfig();
        if (config.Rules.Count == 0)
        {
            config.Rules.Add(new Models.TransferRule());
        }
        config.Rules[0].SourcePath = SourcePath;
        config.Rules[0].DestinationPath = DestinationPath;
        _engine.SaveConfig(config);
        config.Rules[0].AllowedExtensions = string.IsNullOrWhiteSpace(FileExtensions)
            ? Array.Empty<string>()
            : FileExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ext => ext.StartsWith(".") ? ext : "." + ext)
                .ToArray();
        config.Rules[0].ConflictStrategy = SelectedConflictStrategy switch
        {
            "Overwrite" => "Overwrite",
            "Timestamp (_yyyyMMdd_HHmmss)" => "AddTimestamp",
            "Custom Suffix" => "CustomSuffix",
            _ => "Skip"
        };
        config.Rules[0].CustomSuffix = CustomSuffix;
    }

    public void RunTransfer()
    {
        StatusMessage = "Trwa archiwizacja plików...";
        var config = _engine.LoadConfig();
        _engine.ExecuteTransfer(config);
        RefreshLastRun();
        StatusMessage = "Archiwizacja zakończona sukcesem!";
        ResetCountdown();
    }

    private void StartTimer()
    {
        ResetCountdown();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => OnTimerTick();
        _timer.Start();
    }

    private void ResetCountdown()
    {
        var config = _engine.LoadConfig();
        _secondsRemaining = config.IntervalMinutes * 60;
        UpdateCountdownDisplay();
    }

    private void OnTimerTick()
    {
        _secondsRemaining--;
        if (_secondsRemaining <= 0) RunTransfer();
        else UpdateCountdownDisplay();
    }

    private void UpdateCountdownDisplay()
    {
        TimeSpan time = TimeSpan.FromSeconds(_secondsRemaining);
        CountdownText = time.ToString(@"mm\:ss");
    }

    private void RefreshLastRun()
    {
        var config = _engine.LoadConfig();
        LastRunText = config.LastRun.HasValue ? config.LastRun.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never";
    }
}