using System.IO;
using System.Text.Json;
using CatiaAutoDrawing.Core;

namespace CatiaAutoDrawing.Config;

/// <summary>
/// Role: Loads JSON configuration files.
/// TODO: Add schema validation and user-friendly error messages.
/// </summary>
public sealed class ConfigLoader
{
    public Result<AppSettings> LoadAppSettings(string path)
    {
        if (!File.Exists(path))
        {
            return Result<AppSettings>.Failure($"Configuration file not found: {path}");
        }

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return settings is null
            ? Result<AppSettings>.Failure("Configuration file is empty or invalid.")
            : Result<AppSettings>.Success(settings);
    }
}
