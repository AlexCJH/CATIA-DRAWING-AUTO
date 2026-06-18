using System.Collections.Generic;

namespace CatiaAutoDrawing.DimensionGenerator;

/// <summary>
/// Role: Describes dimensions to create before applying them to a CATDrawing.
/// TODO: Add typed dimension target references after CATIA API validation.
/// </summary>
public sealed class DimensionPlan
{
    public List<string> PlannedDimensions { get; } = new();
}
