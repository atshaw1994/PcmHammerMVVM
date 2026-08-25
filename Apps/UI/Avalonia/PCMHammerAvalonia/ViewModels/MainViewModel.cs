using CommunityToolkit.Mvvm.ComponentModel;
using PCMHammerAvalonia.ViewModels;

namespace PCMHammerAvalonia.ViewModels;

public enum LogType
{
    Results,
    Debug
}

public partial class MainViewModel : ViewModelBase
{
    public LogTypeMenuExpanderViewModel MenuExpanderViewModel { get; }
    public LeftSideBarMenuViewModel SidebarViewModel { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentLogText))]
    private LogType _selectedLog = LogType.Results;

    public MainViewModel()
    {
        SidebarViewModel = new LeftSideBarMenuViewModel();

        MenuExpanderViewModel = new LogTypeMenuExpanderViewModel(
            onLogTypeSelected: selected => SelectedLog = selected,
            onExpandSidebarRequested: () => SidebarViewModel.IsSidebarOpen = true
        );
    }

    public string CurrentLogText => SelectedLog switch
    {
        LogType.Results => "[06:12:58.474]  PCM Hammer Results Log...\n[06:12:58.615]  Ready.",
        LogType.Debug => "[DEBUG] [06:12:58.474] Initializing VPW Driver...\n[DEBUG] Port opened.",
        _ => string.Empty
    };
}