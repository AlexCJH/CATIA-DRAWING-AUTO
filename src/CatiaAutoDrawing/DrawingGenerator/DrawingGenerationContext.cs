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
    public bool EnablePdfExport { get; set; }
}
