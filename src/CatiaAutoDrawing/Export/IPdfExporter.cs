namespace CatiaAutoDrawing.Export;

/// <summary>
/// Role: Defines CATDrawing to PDF export contract.
/// TODO: Add Result return type and export options after save workflow is defined.
/// </summary>
public interface IPdfExporter
{
    void Export(object catDrawing, string outputPath);
}
