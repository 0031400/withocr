using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using withocr.Models;
using withocr.Services;

namespace withocr.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ConfigService _configService;

    public SettingsViewModel(
        AppConfig config,
        ConfigService configService)
    {
        Config = new AppConfig
        {
            TesseractPath = config.TesseractPath
        };

        _configService = configService;
        TesseractPath = Config.TesseractPath;
    }

    public AppConfig Config { get; }

    [ObservableProperty]
    private string tesseractPath = string.Empty;

    [ObservableProperty]
    private string message = string.Empty;

    public bool IsSaved { get; private set; }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        var window = Views.SettingsWindow.Current;

        if (window is null)
        {
            return;
        }

        var files = await window.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "选择 Tesseract 程序",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("可执行文件")
                    {
                        Patterns = ["*.exe"]
                    }
                ]
            });

        if (files.Count == 0)
        {
            return;
        }

        var path = files[0].TryGetLocalPath();

        if (!string.IsNullOrWhiteSpace(path))
        {
            TesseractPath = path;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(TesseractPath))
        {
            Message = "请输入 Tesseract 程序路径。";
            return;
        }

        Config.TesseractPath = TesseractPath.Trim();

        await _configService.SaveAsync(Config);

        IsSaved = true;

        Views.SettingsWindow.Current?.Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        Views.SettingsWindow.Current?.Close();
    }
}