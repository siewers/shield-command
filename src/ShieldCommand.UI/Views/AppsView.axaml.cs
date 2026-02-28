using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using ShieldCommand.UI.ViewModels;

namespace ShieldCommand.UI.Views;

public partial class AppsView : UserControl
{
    public AppsView()
    {
        InitializeComponent();

        var installButton = this.FindControl<Button>("InstallApkButton")!;
        installButton.Click += OnInstallApkClick;
    }

    private async void OnInstallApkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AppsViewModel appsVm)
            return;

        // Get the InstallViewModel from the main window
        var mainWindow = TopLevel.GetTopLevel(this) as Window;
        if (mainWindow?.DataContext is not MainWindowViewModel mainVm)
            return;

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

        // Refresh apps list only if something was installed
        if (mainVm.InstallPage.DidInstall)
        {
            mainVm.InstallPage.ResetDidInstall();
            appsVm.RefreshCommand.Execute(null);
        }
    }
}
