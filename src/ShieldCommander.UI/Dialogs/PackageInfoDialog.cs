using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using ShieldCommander.Core.Models;

using ShieldCommander.UI.Formatters;


namespace ShieldCommander.UI.Dialogs;

internal static class PackageInfoDialog
{
    /// Shows a package info dialog. Returns true if the user clicked the primary action button.
    public static async Task<bool> ShowAsync(
        InstalledPackage package, string? primaryButtonText = null, string? confirmMessage = null)
    {
        return await ShowCoreAsync(
            package.PackageName,
            BuildPackageRows(package),
            primaryButtonText,
            confirmMessage);
    }

    /// Shows a process info dialog with process details, plus package info for user apps.
    public static async Task<bool> ShowAsync(
        ProcessDetails process,
        InstalledPackage? package,
        string? primaryButtonText = null,
        string? confirmMessage = null)
    {
        var rows = new List<(string Label, string? Value)>
        {
            ("PID", process.Pid.ToString()),
            ("Command", process.Name),
            ("State", process.State),
            ("Parent PID", process.PPid),
            ("UID", process.Uid),
            ("Threads", process.Threads),
            ("Memory (RSS)", UnitFormatter.Bytes(process.VmRss)),
        };

        if (package is not null)
        {
            rows.Add(("", null)); // spacer
            rows.AddRange(BuildPackageRows(package));
        }

        return await ShowCoreAsync(process.Name, rows, primaryButtonText, confirmMessage);
    }

    private static List<(string Label, string? Value)> BuildPackageRows(InstalledPackage package)
    {
        return
        [
            ("Package", package.PackageName),
            ("Version", package.VersionName),
            ("Version Code", package.VersionCode),
            ("Installer", package.InstallerPackageName),
            ("First Installed", package.FirstInstallTime),
            ("Last Updated", package.LastUpdateTime),
            ("Target SDK", package.TargetSdk),
            ("Min SDK", package.MinSdk),
            ("Size", UnitFormatter.Bytes(package.CodeSize)),
            ("Data Dir", package.DataDir),
        ];
    }

private static async Task<bool> ShowCoreAsync(
        string title,
        List<(string Label, string? Value)> rows,
        string? primaryButtonText,
        string? confirmMessage = null)
    {
        var grid = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("Auto,*"),
            RowDefinitions = RowDefinitions.Parse(string.Join(',', Enumerable.Repeat("Auto", rows.Count))),
            MinWidth = 350,
            Margin = new Thickness(0, 8, 0, 0),
        };

        for (var i = 0; i < rows.Count; i++)
        {
            var (labelText, valueText) = rows[i];

            if (string.IsNullOrEmpty(labelText))
            {
                // Spacer row — add vertical gap
                var spacer = new Border { Height = 12 };
                Grid.SetRow(spacer, i);
                Grid.SetColumnSpan(spacer, 2);
                grid.Children.Add(spacer);
                continue;
            }

            var label = new TextBlock
            {
                Text = labelText,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 4, 20, 4),
            };
            Grid.SetRow(label, i);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            var value = new TextBlock
            {
                Text = valueText ?? "\u2014",
                Margin = new Thickness(0, 4),
                TextWrapping = TextWrapping.Wrap,
            };
            Grid.SetRow(value, i);
            Grid.SetColumn(value, 1);
            grid.Children.Add(value);
        }

        var confirmed = false;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = grid,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
        };

        if (confirmMessage is not null)
        {
            dialog.PrimaryButtonClick += async (_, args) =>
            {
                var deferral = args.GetDeferral();
                var confirm = new ContentDialog
                {
                    Title = primaryButtonText,
                    Content = confirmMessage,
                    PrimaryButtonText = primaryButtonText,
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                };

                confirmed = await confirm.ShowAsync() == ContentDialogResult.Primary;
                args.Cancel = !confirmed;
                deferral.Complete();
            };
        }

        var result = await dialog.ShowAsync();
        return confirmMessage is not null ? confirmed : result == ContentDialogResult.Primary;
    }
}
