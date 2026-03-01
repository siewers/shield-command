using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using ShieldCommander.UI.ViewModels;

namespace ShieldCommander.UI.Views;

public sealed partial class DeviceView : UserControl
{
    public DeviceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DeviceViewModel vm)
        {
            vm.ShowAuthorizationDialog = ShowAuthorizationDialogAsync;
        }
    }

    private async Task ShowAuthorizationDialogAsync(CancellationToken ct)
    {
        var dialog = new ContentDialog
        {
            Title = "Waiting for Device",
            Content = "Please check your TV and accept the connection.\n\n" +
                      "If your device is off, turn it on — the connection will be " +
                      "established automatically.\n\n" +
                      "A dialog should appear on your device asking you to allow USB debugging. " +
                      "Select \"Always allow from this computer\" and tap OK.",
            CloseButtonText = "Cancel",
        };

        // Auto-close when authorization is detected (token cancelled by VM)
        ct.Register(() =>
        {
            Dispatcher.UIThread.Post(() => dialog.Hide());
        });

        await dialog.ShowAsync();
    }

    private async void DropdownButton_Click(object? sender, RoutedEventArgs e)
    {
        var autoComplete = this.FindControl<AutoCompleteBox>("IpAutoComplete");
        if (autoComplete is null)
        {
            return;
        }

        autoComplete.Text = string.Empty;
        autoComplete.Focus();

        // Small delay so the focus and text change settle before opening
        await Task.Delay(50);
        autoComplete.IsDropDownOpen = true;
    }

    private async void BrowseAdbButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select ADB executable",
            AllowMultiple = false,
        });

        if (files.Count > 0 && DataContext is DeviceViewModel vm)
        {
            vm.AdbPath = files[0].Path.LocalPath;
        }
    }
}
