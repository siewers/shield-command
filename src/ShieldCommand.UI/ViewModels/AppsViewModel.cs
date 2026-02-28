using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShieldCommand.Core.Models;
using ShieldCommand.Core.Services;

namespace ShieldCommand.UI.ViewModels;

public sealed partial class PackageRow : ObservableObject
{
    public InstalledPackage Package { get; }

    public string PackageName => Package.PackageName;
    public string? VersionName => Package.VersionName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SizeDisplay))]
    private string? _codeSize;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SizeDisplay))]
    private bool _isSizeLoading = true;

    public string SizeDisplay => IsSizeLoading ? "\u2026" : CodeSize ?? "\u2014";

    public PackageRow(InstalledPackage package)
    {
        Package = package;
        _codeSize = package.CodeSize;
        _isSizeLoading = package.CodeSize is null;
    }
}

public sealed partial class AppsViewModel : ViewModelBase
{
    private static readonly SemaphoreSlim s_sizeSemaphore = new(1);

    public AdbService AdbService { get; }

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private PackageRow? _selectedPackage;

    public ObservableCollection<PackageRow> Packages { get; } = [];

    public AppsViewModel(AdbService adbService)
    {
        AdbService = adbService;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        StatusText = "Loading packages...";

        var packages = await AdbService.GetInstalledPackagesAsync();
        var incoming = packages.ToDictionary(p => p.PackageName);
        var existing = Packages.ToDictionary(r => r.PackageName);

        // Remove packages no longer installed
        for (var i = Packages.Count - 1; i >= 0; i--)
        {
            if (!incoming.ContainsKey(Packages[i].PackageName))
            {
                Packages.RemoveAt(i);
            }
        }

        // Add new packages
        var newRows = new List<PackageRow>();
        foreach (var pkg in packages)
        {
            if (!existing.ContainsKey(pkg.PackageName))
            {
                var row = new PackageRow(pkg);
                Packages.Add(row);
                newRows.Add(row);
            }
        }

        StatusText = $"{Packages.Count} packages found";
        IsBusy = false;

        if (newRows.Count > 0)
        {
            _ = LoadSizesAsync(newRows);
        }
    }

    private async Task LoadSizesAsync(IEnumerable<PackageRow> rows)
    {
        var tasks = rows.Select(row => LoadSizeForPackageAsync(row));
        await Task.WhenAll(tasks);
    }

    private async Task LoadSizeForPackageAsync(PackageRow row)
    {
        await s_sizeSemaphore.WaitAsync();
        try
        {
            var detailed = await AdbService.GetPackageInfoAsync(row.PackageName, includeSize: true);
            row.CodeSize = detailed.CodeSize;
        }
        catch
        {
            // Size loading is best-effort; skip failures
        }
        finally
        {
            row.IsSizeLoading = false;
            s_sizeSemaphore.Release();
        }
    }

    [RelayCommand]
    private async Task UninstallAsync(PackageRow row)
    {
        IsBusy = true;
        StatusText = $"Uninstalling {row.PackageName}...";

        var result = await AdbService.UninstallPackageAsync(row.PackageName);

        if (result.Success)
        {
            Packages.Remove(row);
            StatusText = $"Uninstalled {row.PackageName}";
        }
        else
        {
            StatusText = $"Failed to uninstall: {(string.IsNullOrEmpty(result.Error) ? result.Output : result.Error)}";
        }

        IsBusy = false;
    }
}
