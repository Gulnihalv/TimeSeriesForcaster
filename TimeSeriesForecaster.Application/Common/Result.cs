namespace TimeSeriesForecaster.Application.Common;

public enum ResultErrorType
{
    NotFound,
    Forbidden,
    ValidationError,
    Unexpected,
    BadRequest,
    Unauthorized,
    InternalServerError
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ResultErrorType? ErrorType { get; }

    protected Result(bool isSuccess, string? error, ResultErrorType? errorType)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(ResultErrorType errorType, string error) => new(false, error, errorType);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(ResultErrorType errorType, string error) => Result<T>.Failure(errorType, error);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value, string? error, ResultErrorType? errorType)
        : base(isSuccess, error, errorType)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);
    public static new Result<T> Failure(ResultErrorType errorType, string error) => new(false, default, error, errorType);
}