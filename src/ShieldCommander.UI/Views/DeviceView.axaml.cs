using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ShieldCommander.UI.ViewModels;

namespace ShieldCommander.UI.Views;

public sealed partial class DeviceView : UserControl
{
    public DeviceView()
    {
        InitializeComponent();
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
