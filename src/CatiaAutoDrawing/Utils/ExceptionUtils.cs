using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CatiaAutoDrawing.Utils;

/// <summary>
/// Role: Formats reflection and COM exceptions without hiding the root CATIA error.
/// </summary>
public static class ExceptionUtils
{
    public static Exception GetRootCause(Exception exception)
    {
        var current = exception;
        while (current is TargetInvocationException && current.InnerException is not null)
        {
            current = current.InnerException;
        }

        return current;
    }

    public static string? GetComErrorCode(Exception exception)
    {
        var rootCause = GetRootCause(exception);
        if (rootCause is COMException comException)
        {
            return $"0x{comException.ErrorCode:X8}";
        }

        return exception is COMException directComException
            ? $"0x{directComException.ErrorCode:X8}"
            : null;
    }

    public static string GetDetailedMessage(Exception exception)
    {
        var rootCause = GetRootCause(exception);
        var comErrorCode = GetComErrorCode(exception);

        var message = $"Exception: {exception.GetType().FullName}: {exception.Message}{Environment.NewLine}" +
                      $"Root cause: {rootCause.GetType().FullName}: {rootCause.Message}";

        if (!string.IsNullOrWhiteSpace(comErrorCode))
        {
            message += $"{Environment.NewLine}Root COM error: {comErrorCode}";
        }

        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            message += $"{Environment.NewLine}StackTrace:{Environment.NewLine}{exception.StackTrace}";
        }

        if (!ReferenceEquals(exception, rootCause) && !string.IsNullOrWhiteSpace(rootCause.StackTrace))
        {
            message += $"{Environment.NewLine}Root StackTrace:{Environment.NewLine}{rootCause.StackTrace}";
        }

        return message;
    }
}
