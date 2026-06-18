namespace CatiaAutoDrawing.ModelInspector;

/// <summary>
/// Role: Describes one drawing marker found in GS_DRAWING_INFO.
/// TODO: Add CATIA reference path once marker retrieval is implemented.
/// </summary>
public sealed record DrawingMarkerInfo(string Name, string MarkerType, bool IsRequired);
