namespace MiniCommerce.Shared.Common;

public class ServiceResult
{
    protected ServiceResult(
        bool isSuccess,
        Error error,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Successful result cannot contain an error.");
        }

        if (!isSuccess && error == Error.None && validationErrors is null)
        {
            throw new InvalidOperationException("Failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    public static ServiceResult Success()
        => new(true, Error.None);

    public static ServiceResult Fail(Error error)
        => new(false, error);

    public static ServiceResult ValidationFail(IReadOnlyDictionary<string, string[]> validationErrors)
        => new(false, Error.Validation(), validationErrors);
}

public class ServiceResult<T> : ServiceResult
{
    private ServiceResult(
        bool isSuccess,
        T? data,
        Error error,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
        : base(isSuccess, error, validationErrors)
    {
        Data = data;
    }

    public T? Data { get; }

    public static ServiceResult<T> Success(T data)
        => new(true, data, Error.None);

    public new static ServiceResult<T> Fail(Error error)
        => new(false, default, error);

    public new static ServiceResult<T> ValidationFail(IReadOnlyDictionary<string, string[]> validationErrors)
        => new(false, default, Error.Validation(), validationErrors);
}
