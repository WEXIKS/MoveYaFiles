using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Threading;
using Microsoft.Win32;
using MoveYaFiles.Models;
using MoveYaFiles.Services;

namespace MoveYaFiles.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly TransferEngine _transferEngine = new();
    private readonly DispatcherTimer _executionTimer;
    private readonly DispatcherTimer _countdownTimer;
    private DateTime _nextRunTime;

    public ObservableCollection<TransferRule> Rules { get; set; } = [];

    private TransferRule? _selectedRule;
    public TransferRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (SetProperty(ref _selectedRule, value))
            {
                OnPropertyChanged(nameof(HasSelectedRule));
                OnPropertyChanged(nameof(SourcePath));
                OnPropertyChanged(nameof(DestinationPath));
                OnPropertyChanged(nameof(FileExtensions));
                OnPropertyChanged(nameof(SelectedConflictStrategy));
                OnPropertyChanged(nameof(CustomSuffix));
                OnPropertyChanged(nameof(IsCustomSuffixEnabled));
            }
        }
    }

    public bool HasSelectedRule => SelectedRule != null;

    public string SourcePath
    {
        get => SelectedRule?.SourcePath ?? string.Empty;
        set
        {
            if (SelectedRule != null && SelectedRule.SourcePath != value)
            {
                SelectedRule.SourcePath = value;
                OnPropertyChanged();
                SaveConfig();
            }
        }
    }

    public string DestinationPath
    {
        get => SelectedRule?.DestinationPath ?? string.Empty;
        set
        {
            if (SelectedRule != null && SelectedRule.DestinationPath != value)
            {
                SelectedRule.DestinationPath = value;
                OnPropertyChanged();
                SaveConfig();
            }
        }
    }

    public string FileExtensions
    {
        get => SelectedRule?.AllowedExtensions != null ? string.Join(", ", SelectedRule.AllowedExtensions) : string.Empty;
        set
        {
            if (SelectedRule != null)
            {
                SelectedRule.AllowedExtensions = value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
                OnPropertyChanged();
                SaveConfig();
            }
        }
    }

    public ObservableCollection<string> ConflictStrategies { get; } =
    [
        "Skip",
        "Overwrite",
        "Timestamp (_yyyyMMdd_HHmmss)",
        "Custom Suffix"
    ];

    public string SelectedConflictStrategy
    {
        get => SelectedRule?.ConflictStrategy ?? "Skip";
        set
        {
            if (SelectedRule != null)
            {
                SelectedRule.ConflictStrategy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCustomSuffixEnabled));
                SaveConfig();
            }
        }
    }

    public string CustomSuffix
    {
        get => SelectedRule?.CustomSuffix ?? "_kopia";
        set
        {
            if (SelectedRule != null)
            {
                SelectedRule.CustomSuffix = value;
                OnPropertyChanged();
                SaveConfig();
            }
        }
    }

    public bool IsCustomSuffixEnabled => SelectedConflictStrategy.Contains("Custom");

    // Rozszerzona lista interwałów
    public ObservableCollection<string> IntervalOptions { get; } =
    [
        "15 minutes",
        "30 minutes",
        "1 hour",
        "2 hours",
        "6 hours",
        "12 hours",
        "24 hours",
        "7 days",
        "30 days"
    ];

    private string _selectedInterval = "30 minutes";
    public string SelectedInterval
    {
        get => _selectedInterval;
        set
        {
            if (SetProperty(ref _selectedInterval, value))
            {
                UpdateTimerInterval();
            }
        }
    }

    private string _lastSyncText = "Last sync: Never";
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

    private bool _runAtStartup;
    public bool RunAtStartup
    {
        get => _runAtStartup;
        set
        {
            if (SetProperty(ref _runAtStartup, value))
            {
                SetAutoStart(value);
            }
        }
    }

    public ICommand AddRuleCommand { get; }
    public ICommand RemoveRuleCommand { get; }
    public ICommand RunArchivingCommand { get; }

    public MainViewModel()
    {
        AddRuleCommand = new RelayCommand(AddRule);
        RemoveRuleCommand = new RelayCommand(RemoveRule, () => HasSelectedRule);
        RunArchivingCommand = new RelayCommand(RunTransfer);

        LoadConfig();

        // Timer obsługujący cykliczne przenoszenie plików
        _executionTimer = new DispatcherTimer();
        _executionTimer.Tick += (_, _) => RunTransfer();

        // Timer odliczający czas co sekundę dla interfejsu
        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += (_, _) => UpdateCountdownText();

        UpdateTimerInterval();
        _executionTimer.Start();
        _countdownTimer.Start();
    }

    public void AddRule()
    {
        var newRule = new TransferRule();
        Rules.Add(newRule);
        SelectedRule = newRule;
        SaveConfig();
    }

    public void RemoveRule()
    {
        if (SelectedRule != null)
        {
            Rules.Remove(SelectedRule);
            SelectedRule = Rules.FirstOrDefault();
            SaveConfig();
        }
    }

    public void RunTransfer()
    {
        if (Rules.Count == 0)
        {
            StatusText = "No active rules to process.";
            return;
        }

        try
        {
            StatusText = "Archiving in progress...";

            var config = new AppConfig { Rules = Rules.ToList() };
            _transferEngine.ExecuteTransfer(config);

            LastSyncText = $"Last sync: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            StatusText = "Archiving completed successfully.";

            // Reset odliczania po wykonaniu
            ResetNextRunTime();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private int GetCurrentIntervalMinutes()
    {
        return SelectedInterval switch
        {
            "15 minutes" => 15,
            "1 hour" => 60,
            "2 hours" => 120,
            "6 hours" => 360,
            "12 hours" => 720,
            "24 hours" => 1440,
            "7 days" => 10080,   // 7 * 24 * 60
            "30 days" => 43200,  // 30 * 24 * 60
            _ => 30
        };
    }

    private void UpdateTimerInterval()
    {
        int minutes = GetCurrentIntervalMinutes();
        _executionTimer.Interval = TimeSpan.FromMinutes(minutes);
        ResetNextRunTime();
    }

    private void ResetNextRunTime()
    {
        _nextRunTime = DateTime.Now.AddMinutes(GetCurrentIntervalMinutes());
        UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        var remaining = _nextRunTime - DateTime.Now;

        if (remaining <= TimeSpan.Zero)
        {
            NextSyncText = "Next auto-archiving: Pending...";
        }
        else if (remaining.TotalDays >= 1)
        {
            NextSyncText = $"Next auto-archiving in: {remaining.Days}d {remaining.Hours}h {remaining.Minutes}m";
        }
        else
        {
            NextSyncText = $"Next auto-archiving in: {remaining:hh\\:mm\\:ss}";
        }
    }

    private void SetAutoStart(bool enable)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (enable)
                        key.SetValue("MoveYaFiles", $"\"{appPath}\"");
                    else
                        key.DeleteValue("MoveYaFiles", false);
                }
            }
        }
        catch
        {
            // Ignorowanie błędów uprawnień
        }
    }

    private void LoadConfig()
    {
        var config = _transferEngine.LoadConfig();
        Rules.Clear();

        foreach (var rule in config.Rules)
        {
            Rules.Add(rule);
        }

        if (config.LastRun != DateTime.MinValue)
        {
            LastSyncText = $"Last sync: {config.LastRun:yyyy-MM-dd HH:mm:ss}";
        }

        SelectedRule = Rules.FirstOrDefault();
    }

    public void SaveConfig()
    {
        var config = new AppConfig { Rules = Rules.ToList() };
        _transferEngine.SaveConfig(config);
    }
}