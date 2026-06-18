using CatiaAutoDrawing.Core;

namespace CatiaAutoDrawing.CatiaConnection;

/// <summary>
/// Role: Defines CATIA connection and active document queries.
/// TODO: Add methods only as MVP steps require them.
/// </summary>
public interface ICatiaConnectionService
{
    Result CheckConnection();
    Result<CatiaDocumentInfo> GetActiveDocumentInfo();
}
