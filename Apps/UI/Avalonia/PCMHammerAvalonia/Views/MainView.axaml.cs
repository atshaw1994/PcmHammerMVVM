
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PCMHammerAvalonia.ViewModels;

namespace PCMHammerAvalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void OnBackdropTapped(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SidebarViewModel.ToggleSidebarCommand.Execute(null);
        }
    }
}