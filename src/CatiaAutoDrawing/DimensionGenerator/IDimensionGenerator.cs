namespace CatiaAutoDrawing.DimensionGenerator;

/// <summary>
/// Role: Defines dimension generation contract.
/// TODO: Add narrower methods only after first dimension scenario is validated.
/// </summary>
public interface IDimensionGenerator
{
    DimensionPlan CreateDimensionPlan(object catiaDocument);
}
