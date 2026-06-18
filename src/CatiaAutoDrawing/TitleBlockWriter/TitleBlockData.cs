namespace CatiaAutoDrawing.TitleBlockWriter;

/// <summary>
/// Role: Contains title block data extracted from part parameters or user input.
/// TODO: Align property names with company title block field names.
/// </summary>
public sealed class TitleBlockData
{
    public string PartNumber { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public string Designer { get; set; } = string.Empty;
}
