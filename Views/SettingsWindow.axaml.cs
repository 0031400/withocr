using System;
using Avalonia.Controls;

namespace withocr.Views;

public partial class SettingsWindow : Window
{
    public static SettingsWindow? Current { get; private set; }

    public SettingsWindow()
    {
        InitializeComponent();
        Current = this;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (ReferenceEquals(Current, this))
        {
            Current = null;
        }

        base.OnClosed(e);
    }
}