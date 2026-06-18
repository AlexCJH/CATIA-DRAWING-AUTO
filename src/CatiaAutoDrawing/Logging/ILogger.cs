namespace CatiaAutoDrawing.Logging;

/// <summary>
/// Role: Defines application logging methods.
/// TODO: Add structured log level enum if TXT logs become hard to parse.
/// </summary>
public interface ILogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}
