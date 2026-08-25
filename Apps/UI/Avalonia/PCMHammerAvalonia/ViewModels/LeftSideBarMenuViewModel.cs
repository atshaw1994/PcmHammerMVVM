using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCMHammerAvalonia.ViewModels;

namespace PCMHammerAvalonia.ViewModels;

public partial class LeftSideBarMenuViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SidebarWidth))]
    [NotifyPropertyChangedFor(nameof(IsSidebarOverlayVisible))]
    private bool _isSidebarOpen;

    private double _screenWidth = 360;
    private double _pointerStartX;

    public double SidebarWidth => IsSidebarOpen ? _screenWidth * 0.75 : 0;
    public bool IsSidebarOverlayVisible => IsSidebarOpen;

    public void SetScreenWidth(double width) => _screenWidth = width;

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;

    public void OnPointerPressed(double x) => _pointerStartX = x;

    public void OnPointerReleased(double x)
    {
        double delta = x - _pointerStartX;

        if (!IsSidebarOpen && delta > 100)
            IsSidebarOpen = true;
        else if (IsSidebarOpen && delta < -100)
            IsSidebarOpen = false;
    }
}