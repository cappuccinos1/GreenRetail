namespace GreenRetail.Core.Results;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Ok()
        => new(true, null);

    public static Result Fail(string error)
        => new(false, error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, string? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value when result is not successful.");

    public static Result<T> Ok(T value)
        => new(value, true, null);

    public new static Result<T> Fail(string error)
        => new(default, false, error);
}