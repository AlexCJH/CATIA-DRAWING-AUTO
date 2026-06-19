namespace CatiaAutoDrawing.DrawingGenerator;

/// <summary>
/// Role: Carries inputs required for a drawing generation run.
/// TODO: Replace object references with verified CATIA abstractions.
/// </summary>
public sealed class DrawingGenerationContext
{
    public object? CatiaDocument { get; set; }
    public string TemplatePath { get; set; } = string.Empty;
    public string OutputFolder { get; set; } = string.Empty;
    public string DrawingSize { get; set; } = "A3";
    public IReadOnlyDictionary<string, string> DrawingTemplates { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public bool EnablePdfExport { get; set; }
}
