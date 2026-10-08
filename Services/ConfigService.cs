using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using withocr.Models;
namespace withocr.Services;

public sealed class ConfigService
{
    private readonly string _configPath;

    public ConfigService()
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDirectory);

        _configPath = Path.Combine(dataDirectory, "config.json");
    }

    public async Task<AppConfig> LoadAsync()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                var defaultConfig = new AppConfig();
                await SaveAsync(defaultConfig);
                return defaultConfig;
            }

            await using var stream = File.OpenRead(_configPath);

            var config = await JsonSerializer.DeserializeAsync(
      stream,
      AppJsonContext.Default.AppConfig);

            return config ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public async Task SaveAsync(AppConfig config)
    {
        var json = JsonSerializer.Serialize(
          config,
          AppJsonContext.Default.AppConfig);

        await File.WriteAllTextAsync(_configPath, json);
    }
}