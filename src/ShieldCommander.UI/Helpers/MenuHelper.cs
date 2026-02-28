using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;

namespace ShieldCommander.UI.Helpers;

internal static class MenuHelper
{
    private static readonly FontFamily PhosphorThin =
        new("avares://ShieldCommander/Assets/Fonts#Phosphor-Thin");

    public static MenuItem CreateItem(string header, Action onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => onClick();
        return item;
    }

    public static MenuItem CreateItem(string header, string glyph, Action onClick, bool isEnabled = true)
    {
        var item = new MenuItem
        {
            Header = new Avalonia.Controls.StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Children =
                {
                    new FontIcon
                    {
                        Glyph = glyph,
                        FontFamily = PhosphorThin,
                        FontSize = 20,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    },
                    new Avalonia.Controls.TextBlock
                    {
                        Text = header,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 2, 0, 0),
                    },
                },
            },
        };
        item.IsEnabled = isEnabled;
        item.Click += (_, _) => onClick();
        return item;
    }

    public static MenuItem CreateGoogleSearchItem(string searchTerm)
    {
        var url = $"https://www.google.com/search?q=what+is+%22{Uri.EscapeDataString(searchTerm)}%22+android";
        return CreateItem(
            "Search Google",
            "\ue30c",
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
