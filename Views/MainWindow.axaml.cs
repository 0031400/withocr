using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using withocr.ViewModels;

namespace withocr.Views;

public partial class MainWindow : Window
{
    public static MainWindow? Current { get; private set; }

    public MainWindow()
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

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer is null)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var files = e.DataTransfer.TryGetFiles() ?? [];

        var hasImage = files.Any(file =>
        {
            var path = file.TryGetLocalPath();
            return path is not null && IsImageFile(path);
        });

        e.DragEffects = hasImage
            ? DragDropEffects.Copy
            : DragDropEffects.None;

    }



    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }


        var files = e.DataTransfer.TryGetFiles() ?? [];

        var imageFile = files
            .Select(file => file.TryGetLocalPath())
            .FirstOrDefault(path =>
                path is not null && IsImageFile(path));

        if (imageFile is null)
        {
            return;
        }

        await viewModel.LoadDroppedFileAsync(imageFile);
    }

    private static bool IsImageFile(string path)
    {
        var extension = Path.GetExtension(path);

        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase);
    }
}