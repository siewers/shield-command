using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Media;

namespace ShieldCommander.UI.Helpers;

internal static class MenuHelper
{
    private static readonly FontFamily FontAwesome =
        new("avares://ShieldCommander/Assets/Fonts#Font Awesome 5 Pro Light");

    public static MenuItem CreateItem(string header, Action onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => onClick();
        return item;
    }

    public static MenuItem CreateItem(string header, string glyph, Action onClick)
    {
        var item = new MenuItem
        {
            Header = header,
            Icon = new TextBlock
            {
                Text = glyph,
                FontFamily = FontAwesome,
                FontSize = 14,
            },
        };
        item.Click += (_, _) => onClick();
        return item;
    }

    public static MenuItem CreateGoogleSearchItem(string searchTerm)
    {
        var url = $"https://www.google.com/search?q=what+is+%22{Uri.EscapeDataString(searchTerm)}%22+android";
        return CreateItem(
            $"Search Google for \"{searchTerm}\"",
            "\uf002",
            () => OpenBrowser(url));
    }

    public static void OpenBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else
        {
            Process.Start("xdg-open", url);
        }
    }
}
