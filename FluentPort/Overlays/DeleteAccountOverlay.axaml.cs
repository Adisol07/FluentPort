using Avalonia.Controls;
using System;
using Avalonia.Interactivity;

namespace FluentPort.Overlays;

public partial class DeleteAccountOverlay : UserControl, IOverlay
{
    public Action ClickAway => () => { CancelClicked!.Invoke(); };
    public UserControl UserControl => this;

    public Action? DeleteClicked;
    public Action? CancelClicked;

    public DeleteAccountOverlay()
    {
        InitializeComponent();
    }

    private void DeleteBtnClicked(object? sender, RoutedEventArgs e)
    {
        if (DeleteClicked != null)
            DeleteClicked.Invoke();
    }
    private void CancelBtnClicked(object? sender, RoutedEventArgs e)
    {
        if (CancelClicked != null)
            CancelClicked.Invoke();
    }
}
