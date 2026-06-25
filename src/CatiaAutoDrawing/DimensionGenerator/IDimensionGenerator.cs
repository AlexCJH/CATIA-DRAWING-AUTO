using CatiaAutoDrawing.Core;

namespace CatiaAutoDrawing.DimensionGenerator;

/// <summary>
/// Role: Defines dimension target detection and future dimension generation contract.
/// </summary>
public interface IDimensionGenerator
{
    DimensionPlan CreateDimensionPlan(object catiaDocument);
    Result DetectColorBasedTargets(object sourceDocument);
}