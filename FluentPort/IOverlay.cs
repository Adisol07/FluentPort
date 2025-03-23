using System;
using Avalonia.Controls;

namespace FluentPort;

public interface IOverlay
{
    public Action ClickAway { get; }
    public UserControl UserControl { get; }
}
