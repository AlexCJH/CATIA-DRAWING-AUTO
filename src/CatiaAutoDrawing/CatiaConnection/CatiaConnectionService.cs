using CatiaAutoDrawing.Core;
using CatiaAutoDrawing.Logging;

namespace CatiaAutoDrawing.CatiaConnection;

/// <summary>
/// Role: Connects to a running CATIA Application and reads ActiveDocument metadata.
/// TODO: Implement COM object connection after CATIA V5 R35 automation reference strategy is confirmed.
/// TODO: Return an error when CATIA is not running.
/// TODO: Return an error when ActiveDocument does not exist.
/// </summary>
public sealed class CatiaConnectionService : ICatiaConnectionService
{
    private readonly ILogger _logger;

    public CatiaConnectionService(ILogger logger)
    {
        _logger = logger;
    }

    public Result CheckConnection()
    {
        _logger.Info("CATIA connection check started.");
        return Result.Failure("TODO: CATIA COM connection is not implemented yet.");
    }

    public Result<CatiaDocumentInfo> GetActiveDocumentInfo()
    {
        _logger.Info("ActiveDocument read started.");
        return Result<CatiaDocumentInfo>.Failure("TODO: ActiveDocument read is not implemented yet.");
    }
}
