using CatiaAutoDrawing.Core;

namespace CatiaAutoDrawing.ModelInspector;

/// <summary>
/// Role: Defines model marker inspection contract.
/// TODO: Add typed CATIA document abstraction when connection layer is implemented.
/// </summary>
public interface IModelInspector
{
    Result<InspectionReport> Inspect(object catiaDocument);
}
