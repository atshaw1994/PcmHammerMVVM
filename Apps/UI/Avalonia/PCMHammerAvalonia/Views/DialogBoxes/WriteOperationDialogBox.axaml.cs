using Avalonia.Controls;
using PcmHacking;
using PCMHammerAvalonia.ViewModels;
using System.Security.AccessControl;

namespace PCMHammerAvalonia.Views.DialogBoxes;

public partial class WriteOperationDialogBox : Window
{
    private readonly WriteTypeViewModel _viewModel;

    public PcmType SelectedPCMType { get; set; }
    public WriteType SelectedWriteType { get; set; }
    public bool SuppressOSIDWarning { get; set; }

    public WriteOperationDialogBox()
    {
        InitializeComponent();

        _viewModel = new WriteTypeViewModel();
        DataContext = _viewModel;

        PCMTypeComboBox.SelectedIndex = 0;
        WriteTypeComboBox.SelectedIndex = 0;

        _viewModel.RequestClose += () => Close(false);
        _viewModel.RequestAcceptandClose += AcceptAndClose;
    }

    private void AcceptAndClose()
    {
        SelectedPCMType = _viewModel.SelectedPCMType;
        SelectedWriteType = _viewModel.SelectedWriteType;
        SuppressOSIDWarning = _viewModel.SuppressOSIDWarning;
        Close(true);
    }
}