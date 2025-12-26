using _3DApi.Infrastructure.Errors;

namespace _3DApi.Infrastructure;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Errors = [error];
    }
    
    protected Result(bool isSuccess, IEnumerable<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; protected set; }

    public IEnumerable<Error> Errors { get; protected set; }

    public static Result Success()
    {
        return new Result(true, (Error) null);
    }

    public static Result Failure(Error error)
    {
        return new Result(false, error);
    }
    
    public static Result Failure(IEnumerable<Error> errors)
    {
        return new Result(false, errors);
    }
}

public class Result<T> : Result
{
    public T Value { get; private set; }

    private Result(bool isSuccess, T value, Error error)
        : base(isSuccess, error)
    {
        Value = value;
    }
    
    private Result(bool isSuccess, T value, IEnumerable<Error> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value, string message = "success")
    {
        return new Result<T>(true, value, (Error) null);
    }

    public static Result<T> Failure(Error error, T value = default)
    {
        return new Result<T>(false, value, error);
    }
    
    public static Result<T> Failure(IEnumerable<Error> errors, T value = default)
    {
        return new Result<T>(false, value, errors);
    }
}