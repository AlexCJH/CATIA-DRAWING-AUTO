using System;

namespace CatiaAutoDrawing.Export;

/// <summary>
/// Role: Exports CATDrawing to PDF.
/// TODO: Implement after MVP.
/// TODO: Keep NotImplementedException until CATIA PDF export API is verified.
/// </summary>
public sealed class PdfExporter : IPdfExporter
{
    public void Export(object catDrawing, string outputPath)
    {
        throw new NotImplementedException("PDF export is not implemented yet.");
    }
}
