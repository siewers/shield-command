using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;

namespace ShieldCommander.UI.Platform;

public sealed class WindowsPlatformUI : IPlatformUI
{
    public void ApplyTitleBarLayout(Control spacer, Button statusButton, NavigationView navView, Control appInfo)
    {
        spacer.Width = 0;
        statusButton.Margin = new Thickness(0, 0, 140, 0);
        appInfo.IsVisible = true;
        navView.IsPaneToggleButtonVisible = false;
        navView.IsPaneOpen = true;
        navView.PaneTitle = "";
    }
}
