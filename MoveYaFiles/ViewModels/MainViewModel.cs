using MoveYaFiles.Services;

namespace MoveYaFiles.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly TransferEngine _engine = new();
    private string _statusMessage = "Ready to use";
    private string _lastRunText = "No data";

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

    public MainViewModel()
    {
        RefreshLastRun();
    }

    public void RunTransfer()
    {
        StatusMessage = "Transferring...";
        
        var config = _engine.LoadConfig();
        _engine.ExecuteTransfer(config);

        RefreshLastRun();
        StatusMessage = "Transfer Complete!";
    }

    private void RefreshLastRun()
    {
        var config = _engine.LoadConfig();
        LastRunText = config.LastRun.HasValue ? config.LastRun.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never";
    }
}

