using System.Collections.Generic;

namespace CatiaAutoDrawing.ViewGenerator;

/// <summary>
/// Role: Describes planned drawing view positions before CATIA view creation.
/// TODO: Add sheet size, scale, and collision checks.
/// </summary>
public sealed class ViewLayoutPlan
{
    public List<string> PlannedViews { get; } = new();
}
