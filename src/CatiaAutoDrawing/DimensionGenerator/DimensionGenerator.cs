using System;

namespace CatiaAutoDrawing.DimensionGenerator;

/// <summary>
/// Role: Generates outline, hole, reference-plane, and key point dimensions.
/// TODO: Implement after MVP.
/// TODO: Keep NotImplementedException until CATIA dimension APIs are verified.
/// </summary>
public sealed class DimensionGenerator : IDimensionGenerator
{
    public DimensionPlan CreateDimensionPlan(object catiaDocument)
    {
        throw new NotImplementedException("Dimension generation is not implemented yet.");
    }
}
