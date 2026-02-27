using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using NvidiaShieldManager.UI.ViewModels;

namespace NvidiaShieldManager.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetNavigationIcons();
        ReplaceHamburgerWithAppIcon();
    }

    private void ReplaceHamburgerWithAppIcon()
    {
        // Hide NavView until visual tweaks are applied to prevent layout shift
        NavView.Opacity = 0;

        Loaded += async (_, _) =>
        {
            // Small delay to ensure all templates are fully applied
            await System.Threading.Tasks.Task.Delay(100);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                foreach (var descendant in NavView.GetVisualDescendants())
                {
                    if (descendant is Viewbox vb && vb.Name == "IconHost")
                    {
                        // Verify it's inside TogglePaneButton
                        var parent = vb.GetVisualParent();
                        while (parent != null && parent != NavView)
                        {
                            if (parent is Button btn && btn.Name == "TogglePaneButton")
                            {
                                vb.Child = new Image
                                {
                                    Source = new Bitmap(
                                        AssetLoader.Open(new Uri("avares://NvidiaShieldManager/Assets/app-icon.png"))),
                                    Width = 18,
                                    Height = 18,
                                };
                                break;
                            }
                            parent = parent.GetVisualParent();
                        }
                    }

                    // Nudge the PaneTitle text down to align with the icon
                    if (descendant is TextBlock tb && tb.Name == "PaneTitleTextBlock")
                    {
                        tb.Margin = new Thickness(tb.Margin.Left, tb.Margin.Top + 2, tb.Margin.Right, tb.Margin.Bottom);
                    }

                    // Nudge nav item text down to align with icon
                    if (descendant is ContentPresenter cp && cp.Name == "ContentPresenter"
                        && cp.GetVisualParent() is Control cpParent && cpParent.Name == "ContentGrid")
                    {
                        cp.Margin = new Thickness(cp.Margin.Left, cp.Margin.Top + 4, cp.Margin.Right, cp.Margin.Bottom);
                    }
                }

                NavView.Opacity = 1;
            });
        };
    }

    private void SetNavigationIcons()
    {
        var symbols = new[] { Symbol.Phone, Symbol.AllApps, Symbol.Download, Symbol.Settings, Symbol.Alert };
        var items = NavView.MenuItems;

        for (var i = 0; i < items.Count && i < symbols.Length; i++)
        {
            if (items[i] is NavigationViewItem item)
            {
                item.IconSource = new SymbolIconSource { Symbol = symbols[i] };
            }
        }
    }

    private void NavView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem is NavigationViewItem item &&
            item.Tag is string tag &&
            DataContext is MainWindowViewModel vm)
        {
            vm.NavigateTo(tag);
        }
    }
}
