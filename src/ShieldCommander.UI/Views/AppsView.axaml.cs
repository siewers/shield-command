using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using ShieldCommander.UI.Dialogs;
using ShieldCommander.UI.ViewModels;

namespace ShieldCommander.UI.Views;

public sealed partial class AppsView : UserControl
{
    public AppsView()
    {
        InitializeComponent();

        var installButton = this.FindControl<Button>("InstallApkButton")!;
        installButton.Click += OnInstallApkClick;

        PackageGrid.AddHandler(PointerReleasedEvent, OnPackageGridPointerReleased, RoutingStrategies.Tunnel);
        PackageGrid.DoubleTapped += OnPackageGridDoubleTapped;
    }

    private void OnPackageGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is AppsViewModel { SelectedPackage: { } row } vm)
        {
            _ = ShowInfoAndUninstallAsync(row, vm);
        }
    }

    private void OnPackageGridPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Right)
        {
            return;
        }

        if (DataContext is not AppsViewModel { SelectedPackage: { } row } vm)
        {
            return;
        }

        e.Handled = true;

        var menu = App.Services.GetRequiredService<MenuHelper>();
        var flyout = new MenuFlyout
        {
            OverlayDismissEventPassThrough = true,
            Items =
            {
                menu.CreateItem("Info", "\ue2ce", () => _ = ShowInfoAndUninstallAsync(row, vm)),
                menu.CreateGoogleSearchItem(row.PackageName),
                new Separator(),
                menu.CreateItem("Uninstall", "\ue4a6", () => _ = ShowUninstallAsync(row, vm)),
            }
        };

        flyout.ShowAt(PackageGrid, true);
    }

    private static async Task ShowInfoAndUninstallAsync(PackageRow row, AppsViewModel vm)
    {
        var detailed = await vm.AdbService.GetPackageInfoAsync(row.PackageName, includeSize: true);
        if (await PackageInfoDialog.ShowAsync(detailed, "Uninstall", $"Are you sure you want to uninstall {row.PackageName}?"))
        {
            await vm.UninstallCommand.ExecuteAsync(row);
        }
    }

    private static async Task ShowUninstallAsync(PackageRow row, AppsViewModel vm)
    {
        var dialog = new ContentDialog
        {
            Title = "Uninstall App",
            Content = $"Are you sure you want to uninstall {row.PackageName}?",
            PrimaryButtonText = "Uninstall",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await vm.UninstallCommand.ExecuteAsync(row);
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
