using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class PackageRow : ObservableObject
{
    [ObservableProperty]
    private long? _codeSize;

    [ObservableProperty]
    private bool _isSizeLoading = true;

    public PackageRow(InstalledPackage package)
    {
        Package = package;
        _codeSize = package.CodeSize;
        _isSizeLoading = package.CodeSize is null;
    }

    public InstalledPackage Package { get; }

    public string PackageName => Package.PackageName;

    public string? VersionName => Package.VersionName;
}

public sealed partial class AppsViewModel(AdbService adbService) : ViewModelBase
{
    private static readonly SemaphoreSlim SizeSemaphore = new(1);

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private PackageRow? _selectedPackage;

    [ObservableProperty]
    private string _statusText = string.Empty;

    public AdbService AdbService { get; } = adbService;

    public ObservableCollection<PackageRow> Packages { get; } = [];

    public void Clear()
    {
        Packages.Clear();
        SelectedPackage = null;
        StatusText = string.Empty;
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
        await SizeSemaphore.WaitAsync();
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
            SizeSemaphore.Release();
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
