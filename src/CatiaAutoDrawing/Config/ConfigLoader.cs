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
        var resolvedPath = ResolvePath(path);
        if (!File.Exists(resolvedPath))
        {
            return Result<AppSettings>.Failure($"Configuration file not found: {path}");
        }

        var json = File.ReadAllText(resolvedPath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return settings is null
            ? Result<AppSettings>.Failure("Configuration file is empty or invalid.")
            : Result<AppSettings>.Success(settings);
    }

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path) || File.Exists(path))
        {
            return path;
        }

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "CatiaAutoDrawing.sln")))
            {
                return Path.Combine(current.FullName, path);
            }

            current = current.Parent;
        }

        return path;
    }
}
