using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using ShieldCommander.Core.Services;
using ShieldCommander.UI.Models;
using ShieldCommander.UI.ViewModels;

namespace ShieldCommander.UI.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetNavigationIcons();
        ApplyVisualTweaks();

        Closing += async (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                if (WindowState == WindowState.Normal)
                {
                    AppSettingsAccessor.Settings.SaveWindowBounds(Position.X, Position.Y, Width, Height);
                }

                vm.ActivityMonitorPage.Stop();
                vm.ProcessesPage.Stop();
                vm.CloseAdbSession();

                if (vm.IsDeviceConnected)
                {
                    await vm.DevicePage.DisconnectCommand.ExecuteAsync(null);
                }
            }
        };

        Opened += (_, _) =>
        {
            var settings = AppSettingsAccessor.Settings;
            if (settings.WindowSize is var (w, h))
            {
                Width = w;
                Height = h;
            }

            if (settings.WindowPosition is var (x, y))
            {
                Position = new PixelPoint((int)x, (int)y);
            }
        };
    }

    private void ApplyVisualTweaks()
    {
        // Hide NavView until visual tweaks are applied to prevent layout shift
        NavView.Opacity = 0;

        Loaded += async (_, _) =>
        {
            // Small delay to ensure all templates are fully applied
            await System.Threading.Tasks.Task.Delay(100);

            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                foreach (var descendant in NavView.GetVisualDescendants())
                {
                    if (descendant is Viewbox vb && vb.Name == "IconHost")
                    {
                        // Replace hamburger icon with app icon
                        var parent = vb.GetVisualParent();
                        while (parent != null && parent != NavView)
                        {
                            if (parent is Button btn && btn.Name == "TogglePaneButton")
                            {
                                vb.Child = new Image
                                {
                                    Source = new Bitmap(
                                        AssetLoader.Open(new Uri("avares://ShieldCommander/Assets/app-icon.png"))),
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

                // Update connection indicator color
                UpdateConnectionIndicator();

                // Auto-select first nav item
                NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault();

                // Auto-connect or open device dialog on startup
                if (DataContext is MainWindowViewModel vm && !vm.IsDeviceConnected)
                {
                    var autoConnected = await vm.DevicePage.AutoConnectAsync();
                    if (!autoConnected)
                    {
                        _ = OpenDeviceDialog();
                    }
                }
            });
        };
    }

    private static readonly Avalonia.Media.FontFamily FontAwesome =
        new("avares://ShieldCommander/Assets/Fonts#Font Awesome 5 Pro Light");

    private void SetNavigationIcons()
    {
        var topIcons = new IconSource[]
        {
            new FontIconSource { Glyph = "\uf05a", FontFamily = FontAwesome }, // circle-info
            new FontIconSource { Glyph = "\uf009", FontFamily = FontAwesome }, // table-cells-large
            new FontIconSource { Glyph = "\uf1fe", FontFamily = FontAwesome }, // chart-area
        };
        var items = NavView.MenuItems;

        for (var i = 0; i < items.Count && i < topIcons.Length; i++)
        {
            if (items[i] is NavigationViewItem item)
            {
                item.IconSource = topIcons[i];
            }
        }

        // Child icons under Activity Monitor
        if (items.Count > 2 && items[2] is NavigationViewItem activityMonitor)
        {
            var childIcons = new IconSource[]
            {
                new FontIconSource { Glyph = "\uf2db", FontFamily = FontAwesome }, // microchip
                new FontIconSource { Glyph = "\uf538", FontFamily = FontAwesome }, // memory
                new FontIconSource { Glyph = "\uf0a0", FontFamily = FontAwesome }, // hard-drive
                new FontIconSource { Glyph = "\uf6ff", FontFamily = FontAwesome }, // network-wired
                new FontIconSource { Glyph = "\uf2c9", FontFamily = FontAwesome }, // temperature-half
                new FontIconSource { Glyph = "\uf0ae", FontFamily = FontAwesome }, // list-check
            };
            var children = activityMonitor.MenuItems;
            for (var i = 0; i < children.Count && i < childIcons.Length; i++)
            {
                if (children[i] is NavigationViewItem child)
                {
                    child.IconSource = childIcons[i];
                }
            }
        }
    }

    private void UpdateConnectionIndicator()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var indicator = this.FindControl<Avalonia.Controls.Shapes.Ellipse>("ConnectionIndicator");
            if (indicator != null)
            {
                indicator.Fill = new SolidColorBrush(
                    vm.IsDeviceConnected ? Color.Parse("#44BB44") : Color.Parse("#FF4444"));
            }

            // Subscribe to changes
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.IsDeviceConnected) && indicator != null)
                {
                    indicator.Fill = new SolidColorBrush(
                        vm.IsDeviceConnected ? Color.Parse("#44BB44") : Color.Parse("#FF4444"));
                }
            };
        }
    }

    private async void TitleBarStatusButton_Click(object? sender, RoutedEventArgs e)
    {
        await OpenDeviceDialog();
    }

    private async System.Threading.Tasks.Task OpenDeviceDialog()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var deviceView = new DeviceView
        {
            DataContext = vm.DevicePage,
            MinWidth = 500,
        };

        var dialog = new ContentDialog
        {
            Title = "Device Connection",
            Content = deviceView,
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
        };

        // Auto-close dialog when device connects
        void OnPropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DeviceViewModel.IsConnected) && vm.DevicePage.IsConnected)
            {
                vm.DevicePage.PropertyChanged -= OnPropertyChanged;
                dialog.Hide();
            }
        }
        vm.DevicePage.PropertyChanged += OnPropertyChanged;

        await dialog.ShowAsync();

        // Clean up in case dialog was closed manually
        vm.DevicePage.PropertyChanged -= OnPropertyChanged;
    }

    private void UpdateFrequency_Click(object? sender, EventArgs e)
    {
        if (sender is NativeMenuItem menuItem
            && menuItem.Header is string header
            && menuItem.Parent is NativeMenu parentMenu
            && DataContext is MainWindowViewModel vm)
        {
            // Uncheck all siblings, check the clicked item
            foreach (var item in parentMenu.Items.OfType<NativeMenuItem>())
            {
                item.IsChecked = item == menuItem;
            }

            var rate = RefreshRate.All.FirstOrDefault(r => r.Label == header);
            if (rate is not null)
            {
                vm.ActivityMonitorPage.SelectedRefreshRate = rate;
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
