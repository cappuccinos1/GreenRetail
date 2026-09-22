namespace GreenRetail.Core.Results;

public enum ResultErrorCode
{
    None,
    Validation,
    Authorization,
    NotFound,
    Conflict,
    BusinessRule,
    Concurrency,
    Infrastructure,
    Unexpected
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ResultErrorCode ErrorCode { get; }

    protected Result(bool isSuccess, string? error, ResultErrorCode errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Ok() => new(true, null, ResultErrorCode.None);
    public static Result Fail(string error, ResultErrorCode errorCode = ResultErrorCode.BusinessRule)
        => new(false, error, errorCode);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, string? error, ResultErrorCode errorCode)
        : base(isSuccess, error, errorCode) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value when result is not successful.");

    public static Result<T> Ok(T value) => new(value, true, null, ResultErrorCode.None);
    public new static Result<T> Fail(string error) => new(default, false, error, ResultErrorCode.BusinessRule);
    public static Result<T> Fail(string error, ResultErrorCode errorCode)
        => new(default, false, error, errorCode);
}
