using Avalonia.Controls;
using System;

namespace FluentPort.Overlays;

public partial class OfflineOverlay : UserControl, IOverlay
{
    public Action ClickAway => () => { };
    public UserControl UserControl => this;

    public OfflineOverlay()
    {
        InitializeComponent();
    }
}
