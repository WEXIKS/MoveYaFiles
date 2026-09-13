using System;
using System.Collections.Generic;
using Avalonia.Threading;
using MoveYaFiles.Services;
using System.Linq;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using MoveYaFiles.Models;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

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
    private string _selectedInterval = "30 minutes";
    private bool _runAtStartup;
    
    public ObservableCollection<TransferRule> Rules { get; set; } = new();

    private TransferRule? _selectedRule;
    private string _lastSyncText = "Last sync: Never";
    public ICommand RunArchivingCommand { get; }
  
    public string LastSyncText
    {
        get => _lastSyncText;
        set => SetProperty(ref _lastSyncText, value);
    }

    private string _nextSyncText = "Next auto-archiving in: --:--";
    public string NextSyncText
    {
        get => _nextSyncText;
        set => SetProperty(ref _nextSyncText, value);
    }

    private string _statusText = "Ready to use";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }
    public TransferRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (SetProperty(ref _selectedRule, value))
            {
                // Powiadom UI o zmianie powiązanych pól wybranej reguły
                OnPropertyChanged(nameof(HasSelectedRule));
            }
        }
    }

    public bool HasSelectedRule => SelectedRule != null;
    public void AddRule()
    {
        var newRule = new TransferRule
        {
            SourcePath = "",
            DestinationPath = "",
            ConflictStrategy = "Skip",
            IntervalMinutes = 30
        };
    
        Rules.Add(newRule);
        SelectedRule = newRule;
        SaveCurrentPaths();
    }

    public void RemoveRule()
    {
        if (SelectedRule != null)
        {
            Rules.Remove(SelectedRule);
            SelectedRule = Rules.FirstOrDefault();
            SaveCurrentPaths();
        }
    }

    public bool RunAtStartup
    {
        get => _runAtStartup;
        set
        {
            if (SetProperty(ref _runAtStartup, value))
            {
                SetAutostart(value);
            }
        }
    }

    private void SetAutostart(bool enable)
    {
        if (!OperatingSystem.IsWindows()) return;

        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
        if (key is null) return;

        string? appPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(appPath)) return;

        if (enable)
        {
            key.SetValue("MoveYaFiles", $"\"{appPath}\"");
        }
        else
        {
            if (key.GetValue("MoveYaFiles") is not null)
            {
                key.DeleteValue("MoveYaFiles", throwOnMissingValue: false);
            }
        }
    }

    public Dictionary<string, int> IntervalOptions { get; } = new()
    {
        { "15 minutes", 15 },
        { "30 minutes", 30 },
        { "1 hour", 60 },
        { "6 hours", 360 },
        { "24 hours", 1440 },
        { "7 days", 10080 },
        { "30 days (Monthly)", 43200 }
    };

    public string SelectedInterval
    {
        get => _selectedInterval;
        set
        {
            if (SetProperty(ref _selectedInterval, value))
            {
                UpdateTimerInterval();
                SaveCurrentPaths();
            }
        }
    }

    private void UpdateTimerInterval()
    {
        if (IntervalOptions.TryGetValue(SelectedInterval, out int minutes))
        {
            if (_timer != null)
            {
                _timer.Interval = TimeSpan.FromMinutes(minutes);
            }
        }
    }

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
        RunArchivingCommand = new RelayCommand(RunTransfer);
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
            int savedMinutes = config.Rules[0].IntervalMinutes;
            _selectedInterval = IntervalOptions.FirstOrDefault(x => x.Value == savedMinutes).Key ?? "30 minutes" ;
            UpdateTimerInterval();
            if (OperatingSystem.IsWindows())
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                _runAtStartup = key?.GetValue("MoveYaFiles") != null;
            }
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
        if (IntervalOptions.TryGetValue(SelectedInterval, out int minutes))
        {
            config.Rules[0].IntervalMinutes = minutes;
        }
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