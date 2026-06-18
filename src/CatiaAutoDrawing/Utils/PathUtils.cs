using System.IO;

namespace CatiaAutoDrawing.Utils;

/// <summary>
/// Role: Central path helper methods.
/// TODO: Add project-root resolution for config, templates, output, and logs.
/// </summary>
public static class PathUtils
{
    public static string Normalize(string path) => Path.GetFullPath(path);
}
