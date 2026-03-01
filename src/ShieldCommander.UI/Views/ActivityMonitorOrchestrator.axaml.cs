using System.ComponentModel;
using Avalonia.Controls;
using ShieldCommander.UI.ViewModels;

namespace ShieldCommander.UI.Views;

public sealed partial class ActivityMonitorOrchestrator : UserControl
{
    private readonly Dictionary<string, Control> _metricViews = new()
    {
        ["CPU"] = new CpuView(),
        ["Memory"] = new MemoryView(),
        ["Disk"] = new DiskView(),
        ["Network"] = new NetworkView(),
        ["Thermals"] = new ThermalsView(),
    };

    public ActivityMonitorOrchestrator()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SubscribeToViewModel();
    }

    private ViewModels.ActivityMonitorOrchestrator? _subscribedVm;

    private void SubscribeToViewModel()
    {
        if (_subscribedVm is not null)
        {
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _subscribedVm = DataContext as ViewModels.ActivityMonitorOrchestrator;

        if (_subscribedVm is not null)
        {
            AssignChildDataContexts(_subscribedVm);
            _subscribedVm.PropertyChanged += OnViewModelPropertyChanged;
            UpdateContent(_subscribedVm.SelectedMetric);
        }
    }

    private void AssignChildDataContexts(ViewModels.ActivityMonitorOrchestrator vm)
    {
        _metricViews["CPU"].DataContext = vm.CpuVm;
        _metricViews["Memory"].DataContext = vm.MemoryVm;
        _metricViews["Disk"].DataContext = vm.DiskVm;
        _metricViews["Network"].DataContext = vm.NetworkVm;
        _metricViews["Thermals"].DataContext = vm.ThermalVm;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.ActivityMonitorOrchestrator.SelectedMetric) && _subscribedVm is not null)
        {
            UpdateContent(_subscribedVm.SelectedMetric);
        }
    }

    private void UpdateContent(string metric)
    {
        MetricContent.Content = _metricViews.GetValueOrDefault(metric);
    }
}
