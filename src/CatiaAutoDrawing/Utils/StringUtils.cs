namespace CatiaAutoDrawing.Utils;

/// <summary>
/// Role: Central string helper methods.
/// TODO: Add CATIA marker-name validation helpers.
/// </summary>
public static class StringUtils
{
    public static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);
}
