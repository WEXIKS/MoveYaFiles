using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using MoveYaFiles.ViewModels;

namespace MoveYaFiles.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void AddRule_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.AddRule();
        }
    }

    private void RemoveRule_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.RemoveRule();
        }
    }

    private async void SelectSourceFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.HasSelectedRule)
        {
            var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Source Folder",
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                vm.SourcePath = result[0].Path.LocalPath;
            }
        }
    }

    private async void SelectDestinationFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.HasSelectedRule)
        {
            var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Destination Folder",
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                vm.DestinationPath = result[0].Path.LocalPath;
            }
        }
    }
}