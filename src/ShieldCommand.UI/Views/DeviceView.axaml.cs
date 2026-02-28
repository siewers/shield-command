using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ShieldCommand.UI.Views;

public partial class DeviceView : UserControl
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
}
