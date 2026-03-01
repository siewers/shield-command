using Avalonia.Controls;
using FluentAvalonia.UI.Controls;

namespace ShieldCommander.UI.Platform;

public sealed class MacPlatformUI : IPlatformUI
{
    public void ApplyTitleBarLayout(Control spacer, Button statusButton, NavigationView navView, Control appInfo)
    {
        // macOS defaults are correct: traffic lights on the left, 78px spacer already in XAML
    }
}
