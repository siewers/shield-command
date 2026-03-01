using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using ShieldCommander.Core.Services;
using ShieldCommander.UI.Models;
using ShieldCommander.UI.Platform;
using ShieldCommander.UI.ViewModels;

namespace ShieldCommander.UI.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetNavigationIcons();

        var platformUI = App.Services.GetRequiredService<IPlatformUI>();
        platformUI.ApplyTitleBarLayout(TitleBarLeftSpacer, TitleBarStatusButton, NavView, TitleBarAppInfo);

        var settings = App.Services.GetRequiredService<SettingsService>();

        Closing += async (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                if (WindowState == WindowState.Normal)
                {
                    var position = new System.Drawing.Point(Position.X, Position.Y);
                    var size = new System.Drawing.Size((int)Width, (int)Height);
                    settings.SaveWindowBounds(position, size);
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
            var windowSize = settings.WindowSize;
            if (windowSize.HasValue)
            {
                Width = windowSize.Value.Width;
                Height = windowSize.Value.Height;
            }

            var windowPosition = settings.WindowPosition;
            if (windowPosition.HasValue)
            {
                Position = new PixelPoint(windowPosition.Value.X, windowPosition.Value.Y);
            }
        };

        ApplyVisualTweaks();
    }

    private void ApplyVisualTweaks()
    {
        // Hide NavView until visual tweaks are applied to prevent layout shift
        NavView.Opacity = 0;

        Loaded += async (_, _) =>
        {
            // Small delay to ensure all templates are fully applied
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                foreach (var descendant in NavView.GetVisualDescendants())
                {
                    if (descendant is Viewbox { Name: "IconHost" } vb)
                    {
                        // Replace hamburger icon with app icon
                        var parent = vb.GetVisualParent();
                        while (parent != null && parent != NavView)
                        {
                            if (parent is Button { Name: "TogglePaneButton" })
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
                    if (descendant is TextBlock { Name: "PaneTitleTextBlock" } tb)
                    {
                        tb.Margin = new Thickness(tb.Margin.Left, tb.Margin.Top + 2, tb.Margin.Right, tb.Margin.Bottom);
                    }

                    // Nudge nav item text down to align with icon
                    if (descendant is ContentPresenter { Name: "ContentPresenter" } cp
                        && cp.GetVisualParent() is Control { Name: "ContentGrid" })
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
                if (DataContext is MainWindowViewModel { IsDeviceConnected: false } vm)
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

    private static readonly FontFamily PhosphorThin =
        new("avares://ShieldCommander/Assets/Fonts#Phosphor-Thin");

    private void SetNavigationIcons()
    {
        var topIcons = new IconSource[]
        {
            new FontIconSource { Glyph = "\ue2ce", FontFamily = PhosphorThin, FontSize = 20 }, // info
            new FontIconSource { Glyph = "\ue296", FontFamily = PhosphorThin, FontSize = 20 }, // grid-four
            new FontIconSource { Glyph = "\ue154", FontFamily = PhosphorThin, FontSize = 20 }, // chart-line
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
                new FontIconSource { Glyph = "\ue610", FontFamily = PhosphorThin, FontSize = 20 }, // cpu
                new FontIconSource { Glyph = "\ue9c4", FontFamily = PhosphorThin, FontSize = 20 }, // memory
                new FontIconSource { Glyph = "\ue29e", FontFamily = PhosphorThin, FontSize = 20 }, // hard-drive
                new FontIconSource { Glyph = "\uedde", FontFamily = PhosphorThin, FontSize = 20 }, // network
                new FontIconSource { Glyph = "\ue5c6", FontFamily = PhosphorThin, FontSize = 20 }, // thermometer
                new FontIconSource { Glyph = "\ueadc", FontFamily = PhosphorThin, FontSize = 20 }, // list-checks
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

        // Footer icons
        var footerItems = NavView.FooterMenuItems;
        if (footerItems.Count > 0 && footerItems[0] is NavigationViewItem settingsItem)
        {
            settingsItem.IconSource = new FontIconSource { Glyph = "\ue270", FontFamily = PhosphorThin, FontSize = 20 }; // gear
        }
        if (footerItems.Count > 1 && footerItems[1] is NavigationViewItem aboutItem)
        {
            aboutItem.IconSource = new FontIconSource { Glyph = "\ue3e8", FontFamily = PhosphorThin, FontSize = 20 }; // question
        }
    }

    private void ShowSettingsFlyout()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var flyout = new Flyout();

        var panel = new StackPanel { Spacing = 4, MinWidth = 200 };
        panel.Children.Add(new TextBlock
        {
            Text = "Update Frequency",
            FontWeight = FontWeight.SemiBold,
            FontSize = 13,
            Margin = new Thickness(8, 4),
        });

        foreach (var rate in RefreshRate.All)
        {
            var item = new RadioButton
            {
                Content = rate.Label,
                IsChecked = vm.ActivityMonitorPage.SelectedRefreshRate == rate,
                Margin = new Thickness(4, 0),
            };
            var capturedRate = rate;
            item.IsCheckedChanged += (_, _) =>
            {
                if (item.IsChecked == true)
                {
                    vm.ActivityMonitorPage.SelectedRefreshRate = capturedRate;
                }
            };
            panel.Children.Add(item);
        }

        flyout.Content = new Border
        {
            Padding = new Thickness(4),
            Child = panel,
        };

        flyout.ShowAt(SettingsNavItem);
    }

    private void UpdateConnectionIndicator()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var indicator = this.FindControl<Avalonia.Controls.Shapes.Ellipse>("ConnectionIndicator");
            indicator?.Fill = new SolidColorBrush(vm.IsDeviceConnected ? Color.Parse("#44BB44") : Color.Parse("#FF4444"));

            // Subscribe to changes
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.IsDeviceConnected) && indicator != null)
                {
                    indicator.Fill = new SolidColorBrush(vm.IsDeviceConnected ? Color.Parse("#44BB44") : Color.Parse("#FF4444"));
                }
            };
        }
    }

    private async void TitleBarStatusButton_Click(object? sender, RoutedEventArgs e)
    {
        await OpenDeviceDialog();
    }

    private async Task OpenDeviceDialog()
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
        if (sender is NativeMenuItem { Header: { } header, Parent: { } parentMenu } menuItem
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

    private void NavView_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is NavigationViewItem { Tag: string tag })
        {
            if (tag == "About")
            {
                App.ShowAboutDialog();
            }
            else if (tag == "Settings")
            {
                ShowSettingsFlyout();
            }
        }
    }
}
