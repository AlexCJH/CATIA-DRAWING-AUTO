using System.Collections.Generic;

namespace CatiaAutoDrawing.ModelInspector;

/// <summary>
/// Role: Summarizes drawing marker inspection results.
/// TODO: Add missing marker diagnostics and severity levels.
/// </summary>
public sealed class InspectionReport
{
    public bool HasRequiredGeometrySet { get; set; }
    public List<DrawingMarkerInfo> Markers { get; } = new();
    public List<string> Messages { get; } = new();
}
