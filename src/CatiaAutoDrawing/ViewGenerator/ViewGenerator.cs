using System;

namespace CatiaAutoDrawing.ViewGenerator;

/// <summary>
/// Role: Creates Front, Projection, Detail, and Section Views and calculates view placement.
/// TODO: Verify CATIA V5 R35 DrawingView API before implementation.
/// TODO: Keep NotImplementedException until the Front View MVP extension starts.
/// </summary>
public sealed class ViewGenerator : IViewGenerator
{
    public ViewLayoutPlan CreateLayoutPlan(object catiaDocument)
    {
        throw new NotImplementedException("View generation is not implemented yet.");
    }
}
