using Avalonia.Controls;
using FluentAvalonia.UI.Controls;

namespace ShieldCommander.UI.Platform;

public interface IPlatformUI
{
    void ApplyTitleBarLayout(Control spacer, Button statusButton, NavigationView navView, Control appInfo);
}
