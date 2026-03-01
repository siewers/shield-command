using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShieldCommander.Core.Services;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class InstallViewModel : ViewModelBase
{
    private readonly AdbService _adbService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _selectedApkPath = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    public InstallViewModel(AdbService adbService)
    {
        _adbService = adbService;
    }

    public ObservableCollection<string> ApkQueue { get; } = [];

    public bool DidInstall { get; private set; }

    public void ResetDidInstall() => DidInstall = false;

    public void AddApkFiles(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            if (path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) && !ApkQueue.Contains(path))
            {
                ApkQueue.Add(path);
            }
        }
    }

    [RelayCommand]
    private void RemoveApk(string path)
    {
        ApkQueue.Remove(path);
    }

    [RelayCommand]
    private void ClearQueue()
    {
        ApkQueue.Clear();
        StatusText = string.Empty;
    }

    [RelayCommand]
    private async Task InstallAllAsync()
    {
        if (ApkQueue.Count == 0)
        {
            StatusText = "No APKs to install";
            return;
        }

        IsBusy = true;
        var total = ApkQueue.Count;
        var succeeded = 0;

        for (var i = 0; i < ApkQueue.Count; i++)
        {
            var apk = ApkQueue[i];
            var fileName = Path.GetFileName(apk);
            StatusText = $"Installing {fileName} ({i + 1}/{total})...";

            var result = await _adbService.InstallApkAsync(apk);
            if (result.Success)
            {
                succeeded++;
            }
            else
            {
                StatusText = $"Failed: {fileName} - {(string.IsNullOrEmpty(result.Error) ? result.Output : result.Error)}";
            }
        }

        StatusText = $"Done: {succeeded}/{total} installed successfully";
        if (succeeded > 0)
        {
            DidInstall = true;
        }

        if (succeeded == total)
        {
            ApkQueue.Clear();
        }

        IsBusy = false;
    }
}
