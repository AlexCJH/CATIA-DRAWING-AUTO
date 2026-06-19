using CatiaAutoDrawing.Core;

namespace CatiaAutoDrawing.ViewGenerator;

/// <summary>
/// Role: Defines drawing view generation contract.
/// TODO: Split methods by Front, Projection, Detail, and Section only when each step is implemented.
/// </summary>
public interface IViewGenerator
{
    ViewLayoutPlan CreateLayoutPlan(object catiaDocument);
    Result GenerateFrontView(
        object drawingDocument,
        object sourceDocument,
        string frontViewDirection,
        string topDirection);
}
