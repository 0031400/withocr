using System;
using Avalonia.Media.Imaging;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using withocr.Models;
using withocr.Services;
using withocr.Views;

namespace withocr.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly OcrService _ocrService;

    private AppConfig _config = new();

    private string? _currentImagePath;
    private Bitmap? _currentBitmap;

    public MainViewModel()
    {
        _configService = new ConfigService();
        _ocrService = new OcrService();

        LoadConfigCommand = new AsyncRelayCommand(LoadConfigAsync);

        _ = LoadConfigAsync();
    }

    [ObservableProperty]
    private Bitmap? image;

    [ObservableProperty]
    private string ocrText = string.Empty;

    [ObservableProperty]
    private string statusText = "请选择图片，或将图片拖到左侧区域。";

    [ObservableProperty]
    private bool isBusy;

    public IAsyncRelayCommand LoadConfigCommand { get; }

    public async Task LoadConfigAsync()
    {
        _config = await _configService.LoadAsync();
    }

    [RelayCommand]
    private async Task OpenImageAsync()
    {
        var window = MainWindow.Current;

        if (window is null)
        {
            return;
        }

        var files = await window.StorageProvider.OpenFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "选择图片",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new Avalonia.Platform.Storage.FilePickerFileType("图片")
                    {
                        Patterns =
                        [
                            "*.png",
                            "*.jpg",
                            "*.jpeg",
                            "*.bmp",
                            "*.gif",
                            "*.webp",
                            "*.tif",
                            "*.tiff"
                        ]
                    }
                ]
            });

        if (files.Count == 0)
        {
            return;
        }

        var path = files[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
        {
            StatusText = "无法读取所选文件路径。";
            return;
        }

        await LoadImageAndRecognizeAsync(path);
    }

    [RelayCommand]
    private async Task PasteImageAsync()
    {
        var window = MainWindow.Current;

        if (window?.Clipboard is null)
        {
            StatusText = "当前平台不支持剪贴板。";
            return;
        }

        try
        {
            var clipboardFile = await window.Clipboard.TryGetFileAsync();

            if (clipboardFile is not null)
            {
                var filePath = clipboardFile.TryGetLocalPath();

                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    await LoadImageAndRecognizeAsync(filePath);
                    return;
                }
            }

            var bitmap = await window.Clipboard.TryGetBitmapAsync();

            if (bitmap is null)
            {
                StatusText = "剪贴板中没有图片。";
                return;
            }

            await LoadBitmapAndRecognizeAsync(bitmap);
        }
        catch (Exception ex)
        {
            StatusText = $"读取剪贴板失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CopyTextAsync()
    {
        var window = MainWindow.Current;

        if (window?.Clipboard is null)
        {
            StatusText = "当前平台不支持剪贴板。";
            return;
        }

        if (string.IsNullOrWhiteSpace(OcrText))
        {
            StatusText = "当前没有可复制的 OCR 文字。";
            return;
        }

        await window.Clipboard.SetTextAsync(OcrText);
        StatusText = "OCR 文字已复制。";
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var owner = MainWindow.Current;

        if (owner is null)
        {
            return;
        }

        var settingsViewModel = new SettingsViewModel(
            _config,
            _configService);

        var settingsWindow = new SettingsWindow
        {
            DataContext = settingsViewModel
        };

        await settingsWindow.ShowDialog(owner);

        if (settingsViewModel.IsSaved)
        {
            _config = settingsViewModel.Config;
            StatusText = "设置已保存。";
        }
    }

    public async Task LoadDroppedFileAsync(string filePath)
    {
        await LoadImageAndRecognizeAsync(filePath);
    }



    private async Task LoadImageAndRecognizeAsync(string imagePath)
    {
        if (!IsImageFile(imagePath))
        {
            StatusText = "请选择 PNG、JPG、BMP、GIF、WEBP 或 TIFF 图片。";
            return;
        }

        if (!File.Exists(imagePath))
        {
            StatusText = "图片文件不存在。";
            return;
        }

        try
        {
            await using var stream = File.OpenRead(imagePath);
            var bitmap = new Bitmap(stream);

            _currentImagePath = imagePath;
            _currentBitmap?.Dispose();
            _currentBitmap = bitmap;

            Image = bitmap;
            OcrText = string.Empty;

            await RecognizeAsync(
                () => _ocrService.RecognizeAsync(
                    _config.TesseractPath,
                    imagePath));
        }
        catch (Exception ex)
        {
            StatusText = $"打开图片失败：{ex.Message}";
        }
    }

    private async Task LoadBitmapAndRecognizeAsync(Bitmap bitmap)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "withocr");
        Directory.CreateDirectory(tempDirectory);

        var tempPath = Path.Combine(
            tempDirectory,
            $"clipboard-{Guid.NewGuid():N}.png");

        try
        {
            using var output = File.Create(tempPath);
            bitmap.Save(output, new PngBitmapEncoderOptions());

            _currentImagePath = tempPath;

            _currentBitmap?.Dispose();
            _currentBitmap = bitmap;

            Image = bitmap;
            OcrText = string.Empty;

            await RecognizeAsync(
                () => _ocrService.RecognizeAsync(
                    _config.TesseractPath,
                    tempPath));
        }
        catch (Exception ex)
        {
            StatusText = $"读取剪贴板图片失败：{ex.Message}";
        }
    }

    private async Task RecognizeAsync(Func<Task<string>> recognizeFunc)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusText = "正在进行 OCR 识别...";

            OcrText = await recognizeFunc();

            StatusText = string.IsNullOrWhiteSpace(OcrText)
                ? "OCR 完成，但没有识别到文字。"
                : "OCR 识别完成。";
        }
        catch (Exception ex)
        {
            OcrText = string.Empty;
            StatusText = $"OCR 失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
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