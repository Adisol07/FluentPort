using Avalonia.Controls;
using System;
using Avalonia.Interactivity;

namespace FluentPort.Overlays;

public partial class AddTunnelHelpOverlay : UserControl, IOverlay
{
    public Action ClickAway => () => { };
    public UserControl UserControl => this;

    public Action? ContinueClicked;

    public AddTunnelHelpOverlay()
    {
        InitializeComponent();
    }

    private void ContinueBtnClicked(object? sender, RoutedEventArgs e)
    {
        if (ContinueClicked != null)
            ContinueClicked.Invoke();
    }
}
