namespace CatiaAutoDrawing.CatiaConnection;

/// <summary>
/// Role: Data transfer object for active CATIA document metadata.
/// TODO: Add full file path and product/part-specific identifiers after CATIA document APIs are verified.
/// </summary>
public sealed record CatiaDocumentInfo(string Name, string DocumentType);
