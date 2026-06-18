namespace CatiaAutoDrawing.TitleBlockWriter;

/// <summary>
/// Role: Defines title block writing contract.
/// TODO: Add CATDrawing abstraction after template handling is verified.
/// </summary>
public interface ITitleBlockWriter
{
    void Write(object catDrawing, TitleBlockData data);
}
