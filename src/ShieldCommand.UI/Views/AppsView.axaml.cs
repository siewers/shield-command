using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using ShieldCommand.Core.Models;
using ShieldCommand.UI.Helpers;
using ShieldCommand.UI.ViewModels;

namespace ShieldCommand.UI.Views;

public sealed partial class AppsView : UserControl
{
    public AppsView()
    {
        InitializeComponent();

        var installButton = this.FindControl<Button>("InstallApkButton")!;
        installButton.Click += OnInstallApkClick;

        PackageList.AddHandler(PointerReleasedEvent, OnPackageListPointerReleased, RoutingStrategies.Tunnel);
        PackageList.DoubleTapped += OnPackageListDoubleTapped;
    }

    private void OnPackageListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (PackageList.SelectedItem is InstalledPackage package && DataContext is AppsViewModel vm)
        {
            ShowInfoDialog(package, vm);
        }
    }

    private void OnPackageListPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Right)
        {
            return;
        }

        if (PackageList.SelectedItem is not InstalledPackage package
            || DataContext is not AppsViewModel vm)
        {
            return;
        }

        e.Handled = true;

        var flyout = new MenuFlyout
        {
            OverlayDismissEventPassThrough = true,
            Items =
            {
                MenuHelper.CreateItem("Info", "\uf05a", () => ShowInfoDialog(package, vm)),
                MenuHelper.CreateGoogleSearchItem(package.PackageName),
                new Separator(),
                MenuHelper.CreateItem("Uninstall", "\uf2ed", () => ShowUninstallDialog(package, vm)),
            }
        };

        flyout.ShowAt(PackageList, true);
    }

    private async void ShowInfoDialog(InstalledPackage package, AppsViewModel vm)
    {
        var grid = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("Auto,*"),
            RowDefinitions = RowDefinitions.Parse("Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto"),
            MinWidth = 350,
        };

        var labels = new[]
        {
            "Package", "Version", "Version Code", "Installer",
            "First Installed", "Last Updated", "Target SDK", "Min SDK", "Data Dir",
        };

        var values = new[]
        {
            package.PackageName, package.VersionName, package.VersionCode,
            package.InstallerPackageName, package.FirstInstallTime, package.LastUpdateTime,
            package.TargetSdk, package.MinSdk, package.DataDir,
        };

        for (var i = 0; i < labels.Length; i++)
        {
            var label = new TextBlock
            {
                Text = labels[i],
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Margin = new Avalonia.Thickness(0, 2, 12, 2),
            };
            Grid.SetRow(label, i);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            var value = new TextBlock
            {
                Text = values[i] ?? "\u2014",
                Margin = new Avalonia.Thickness(0, 2),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            };
            Grid.SetRow(value, i);
            Grid.SetColumn(value, 1);
            grid.Children.Add(value);
        }

        var dialog = new ContentDialog
        {
            Title = package.PackageName,
            Content = grid,
            PrimaryButtonText = "Uninstall",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await vm.UninstallCommand.ExecuteAsync(package);
        }
    }

    private async void ShowUninstallDialog(InstalledPackage package, AppsViewModel vm)
    {
        var dialog = new ContentDialog
        {
            Title = "Uninstall App",
            Content = $"Are you sure you want to uninstall {package.PackageName}?",
            PrimaryButtonText = "Uninstall",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await vm.UninstallCommand.ExecuteAsync(package);
        }
    }

    private async void OnInstallApkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AppsViewModel appsVm)
        {
            return;
        }

        var mainWindow = TopLevel.GetTopLevel(this) as Window;
        if (mainWindow?.DataContext is not MainWindowViewModel mainVm)
        {
            return;
        }

        var installView = new InstallView
        {
            DataContext = mainVm.InstallPage,
            MinWidth = 500,
        };

        var dialog = new ContentDialog
        {
            Title = "Install APK",
            Content = installView,
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
        };

        await dialog.ShowAsync();

        if (mainVm.InstallPage.DidInstall)
        {
            mainVm.InstallPage.ResetDidInstall();
            appsVm.RefreshCommand.Execute(null);
        }
    }
}
