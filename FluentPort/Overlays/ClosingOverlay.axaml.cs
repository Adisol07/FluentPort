using Avalonia.Controls;
using System;
using Avalonia.Interactivity;

namespace FluentPort.Overlays;

public partial class ClosingOverlay : UserControl, IOverlay
{
    public Action ClickAway => () => { CancelClicked!.Invoke(); };
    public UserControl UserControl => this;

    public Action? ContinueClicked;
    public Action? CancelClicked;

    public ClosingOverlay()
    {
        InitializeComponent();
    }

    private void ContinueBtnClicked(object? sender, RoutedEventArgs e)
    {
        if (ContinueClicked != null)
            ContinueClicked.Invoke();
    }
    private void CancelBtnClicked(object? sender, RoutedEventArgs e)
    {
        if (CancelClicked != null)
            CancelClicked.Invoke();
    }
}
