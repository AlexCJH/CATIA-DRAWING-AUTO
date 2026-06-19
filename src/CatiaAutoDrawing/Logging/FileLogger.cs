using System;
using System.IO;
using CatiaAutoDrawing.Utils;

namespace CatiaAutoDrawing.Logging;

/// <summary>
/// Role: Writes daily TXT logs and optionally forwards log lines to UI.
/// TODO: Add log retention policy.
/// TODO: Add exception overloads for Error.
/// </summary>
public sealed class FileLogger : ILogger
{
    private readonly string _logFolder;
    private readonly Action<string>? _sink;

    public FileLogger(string logFolder, Action<string>? sink = null)
    {
        _logFolder = logFolder;
        _sink = sink;
    }

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARNING", message);

    public void Error(string message) => Write("ERROR", message);

    public void Error(Exception exception, string message)
    {
        Write("ERROR", message);
        WriteFileOnly("ERROR", ExceptionUtils.GetDetailedMessage(exception));
    }

    private void Write(string level, string message)
    {
        Directory.CreateDirectory(_logFolder);

        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        var filePath = Path.Combine(_logFolder, $"{DateTime.Now:yyyyMMdd}.txt");

        File.AppendAllText(filePath, line + Environment.NewLine);
        _sink?.Invoke(line);
    }

    private void WriteFileOnly(string level, string message)
    {
        Directory.CreateDirectory(_logFolder);

        var filePath = Path.Combine(_logFolder, $"{DateTime.Now:yyyyMMdd}.txt");
        foreach (var detailLine in message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {detailLine}";
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }
}
