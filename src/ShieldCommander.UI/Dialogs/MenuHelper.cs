using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using ShieldCommander.Core.Services.Platform;

namespace ShieldCommander.UI.Dialogs;

internal sealed class MenuHelper(IPlatformShell shell)
{
    private static readonly FontFamily PhosphorThin =
        new("avares://ShieldCommander/Assets/Fonts#Phosphor-Thin");

    public MenuItem CreateItem(string header, Action onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => onClick();
        return item;
    }

    public MenuItem CreateItem(string header, string glyph, Action onClick, bool isEnabled = true)
    {
        var item = new MenuItem
        {
            Header = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new FontIcon
                    {
                        Glyph = glyph,
                        FontFamily = PhosphorThin,
                        FontSize = 20,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    new TextBlock
                    {
                        Text = header,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 2, 0, 0),
                    },
                },
            },
        };

        item.IsEnabled = isEnabled;
        item.Click += (_, _) => onClick();
        return item;
    }

    public MenuItem CreateGoogleSearchItem(string searchTerm)
    {
        var url = $"https://www.google.com/search?q=what+is+%22{Uri.EscapeDataString(searchTerm)}%22+android";
        return CreateItem(
            "Search Google",
            "\ue30c",
            () => shell.OpenUrl(url));
    }
}
