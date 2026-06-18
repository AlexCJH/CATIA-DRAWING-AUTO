using CatiaAutoDrawing.Core;

namespace CatiaAutoDrawing.DrawingGenerator;

/// <summary>
/// Role: Defines the full drawing generation workflow contract.
/// TODO: Add separate workflow steps only after STEP 3 is validated.
/// </summary>
public interface IDrawingGenerator
{
    Result<string> Generate(DrawingGenerationContext context);
}
