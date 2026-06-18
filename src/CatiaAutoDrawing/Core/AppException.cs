using System;

namespace CatiaAutoDrawing.Core;

/// <summary>
/// Role: Base application exception for expected workflow failures.
/// TODO: Add specialized exception types only when repeated error patterns appear.
/// </summary>
public class AppException : Exception
{
    public AppException(string message) : base(message)
    {
    }

    public AppException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
