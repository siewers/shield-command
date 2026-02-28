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
using ShieldCommand.UI.Models;
using ShieldCommand.UI.ViewModels;

namespace ShieldCommand.UI.Views;

public partial class MainWindow : Window
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
                vm.ActivityMonitorPage.Stop();
                vm.ProcessesPage.Stop();
                vm.CloseAdbSession();

                if (vm.IsDeviceConnected)
                {
                    await vm.DevicePage.DisconnectCommand.ExecuteAsync(null);
                }
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
                                        AssetLoader.Open(new Uri("avares://ShieldCommand/Assets/app-icon.png"))),
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

    private void SetNavigationIcons()
    {
        var symbolFont = new Avalonia.Media.FontFamily("avares://FluentAvalonia/Fonts#Symbols");
        var symbols = new IconSource[]
        {
            new FontIconSource { Glyph = "\uE770", FontFamily = symbolFont }, // System
            new SymbolIconSource { Symbol = Symbol.AllApps },
            new FontIconSource { Glyph = "\uE9D9", FontFamily = symbolFont }, // Diagnostic
            new FontIconSource { Glyph = "\uE9F5", FontFamily = symbolFont }, // Processing
        };
        var items = NavView.MenuItems;

        for (var i = 0; i < items.Count && i < symbols.Length; i++)
        {
            if (items[i] is NavigationViewItem item)
            {
                item.IconSource = symbols[i];
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
