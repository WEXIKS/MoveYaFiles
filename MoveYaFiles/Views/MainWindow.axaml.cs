using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MoveYaFiles.ViewModels;


namespace MoveYaFiles.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    private void AddRule_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.AddRule();
    }

    private void RemoveRule_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.RemoveRule();
    }
    private async void SelectSourceFolder_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select source folder",
            AllowMultiple = false
        });

        if (folders.Count > 0 && DataContext is MainViewModel vm)
        {
            vm.SourcePath = folders[0].Path.LocalPath;
        }
    }

    private async void SelectDestinationFolder_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select a destination folder",
            AllowMultiple = false
        });

        if (folders.Count > 0 && DataContext is MainViewModel vm)
        {
            vm.DestinationPath = folders[0].Path.LocalPath;
        }
    }
}