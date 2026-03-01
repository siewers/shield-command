using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ShieldCommander.UI.ViewModels;

namespace ShieldCommander.UI.Views;

public sealed partial class InstallView : UserControl
{
    public InstallView()
    {
        InitializeComponent();

        var dropZone = this.FindControl<Border>("DropZone")!;
        dropZone.AddHandler(DragDrop.DropEvent, OnDrop);
        dropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);

        var browseButton = this.FindControl<Button>("BrowseButton")!;
        browseButton.Click += OnBrowseClick;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not InstallViewModel vm)
        {
            return;
        }

#pragma warning disable CS0618 // Data is obsolete, use DataTransfer
        var files = e.Data.GetFiles();
#pragma warning restore CS0618
        if (files != null)
        {
            var paths = files
                .Select(f => f.Path.LocalPath)
                .Where(p => p.EndsWith(".apk", StringComparison.OrdinalIgnoreCase));
            vm.AddApkFiles(paths);
        }

        e.Handled = true;
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not InstallViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select APK Files",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("APK Files") { Patterns = ["*.apk"] },
                new FilePickerFileType("All Files") { Patterns = ["*"] },
            ],
        });

        if (files.Count > 0)
        {
            var paths = files.Select(f => f.Path.LocalPath);
            vm.AddApkFiles(paths);
        }
    }
}
