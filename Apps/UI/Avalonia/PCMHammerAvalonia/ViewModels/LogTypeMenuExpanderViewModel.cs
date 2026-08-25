using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCMHammerAvalonia.ViewModels;
using System;

namespace PCMHammerAvalonia.ViewModels;

public partial class LogTypeMenuExpanderViewModel : ViewModelBase
{
    private readonly Action<LogType> _onLogTypeSelected;
    private readonly Action? _onExpandSidebarRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PanelWidth))]
    [NotifyPropertyChangedFor(nameof(ToggleIconText))]
    private bool _isExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleIconText))]
    private LogType _selectedLog = LogType.Results;

    private double _screenWidth = 360;

    public LogTypeMenuExpanderViewModel(Action<LogType> onLogTypeSelected, Action onExpandSidebarRequested)
    {
        _onLogTypeSelected = onLogTypeSelected;
        _onExpandSidebarRequested = onExpandSidebarRequested;
    }

    public double PanelWidth => IsExpanded ? Math.Max(56, _screenWidth - 32) : 56;

    public string ToggleIconText => IsExpanded ? "✕" : (SelectedLog == LogType.Results ? "R" : "D");

    [RelayCommand]
    private void ExpandMenu()
    {
        _onExpandSidebarRequested?.Invoke();
        IsExpanded = false; // Optionally close the bottom expander when opening the sidebar
    }

    [RelayCommand]
    private void ToggleExpand(double screenWidth)
    {
        if (screenWidth > 32) _screenWidth = screenWidth;
        IsExpanded = !IsExpanded;
    }

    [RelayCommand]
    private void SelectLogMode(string modeName)
    {
        if (Enum.TryParse<LogType>(modeName, ignoreCase: true, out var parsedMode))
        {
            SelectedLog = parsedMode;
            _onLogTypeSelected?.Invoke(parsedMode); // Notify MainViewModel
        }
        IsExpanded = false;
    }
}