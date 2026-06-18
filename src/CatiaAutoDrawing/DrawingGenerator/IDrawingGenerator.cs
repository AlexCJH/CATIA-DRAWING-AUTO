namespace CatiaAutoDrawing.DrawingGenerator;

/// <summary>
/// Role: Defines the full drawing generation workflow contract.
/// TODO: Return Result type after workflow errors are defined.
/// </summary>
public interface IDrawingGenerator
{
    void Generate(DrawingGenerationContext context);
}
