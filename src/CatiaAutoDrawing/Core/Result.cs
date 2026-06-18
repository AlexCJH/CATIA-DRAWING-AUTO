namespace CatiaAutoDrawing.Core;

/// <summary>
/// Role: Lightweight success/failure result type for service boundaries.
/// TODO: Extend with error codes when workflow errors become stable.
/// </summary>
public sealed class Result
{
    private Result(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(string errorMessage) => new(false, errorMessage);
}

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
}
