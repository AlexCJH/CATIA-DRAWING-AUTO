using System;

namespace CatiaAutoDrawing.TitleBlockWriter;

/// <summary>
/// Role: Writes part parameters such as drawing number, part name, material, revision, and designer to title block.
/// TODO: Implement after MVP.
/// TODO: Keep NotImplementedException until template field mapping is confirmed.
/// </summary>
public sealed class TitleBlockWriter : ITitleBlockWriter
{
    public void Write(object catDrawing, TitleBlockData data)
    {
        throw new NotImplementedException("Title block writing is not implemented yet.");
    }
}
