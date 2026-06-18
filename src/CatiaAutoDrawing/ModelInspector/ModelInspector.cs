using System;
using CatiaAutoDrawing.Core;

namespace CatiaAutoDrawing.ModelInspector;

/// <summary>
/// Role: Inspects CATPart/CATProduct drawing markers such as GS_DRAWING_INFO.
/// TODO: Implement GS_DRAWING_INFO search.
/// TODO: Implement MAIN_VIEW_PLANE search.
/// TODO: Implement TOP_DIRECTION search.
/// </summary>
public sealed class ModelInspector : IModelInspector
{
    public Result<InspectionReport> Inspect(object catiaDocument)
    {
        throw new NotImplementedException("Model inspection is not implemented yet.");
    }
}
