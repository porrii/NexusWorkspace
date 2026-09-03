namespace NexusWorkspace.Application.Common;

/// <summary>A machine-readable error code plus a user-facing message (Spanish).</summary>
public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

/// <summary>
/// Outcome of an application operation. Use for <em>expected</em> failures
/// (validation, not found, invalid transition). Reserve exceptions for the unexpected.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result Failure(string code, string message) => new(false, new Error(code, message));

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);

    public static Result<T> Failure<T>(Error error) => new(default, false, error);

    public static Result<T> Failure<T>(string code, string message) => new(default, false, new Error(code, message));
}

/// <inheritdoc cref="Result" />
public class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
        => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede leer Value de un Result fallido.");

    public static implicit operator Result<T>(T value) => Success(value);
}
