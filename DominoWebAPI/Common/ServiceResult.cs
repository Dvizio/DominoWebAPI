namespace DominoWebAPI.Common;

public enum ServiceErrorType
{
    None = 0,
    BadRequest,
    NotFound,
    Unauthorized,
    Forbidden,
    Conflict
}

public class ServiceResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public ServiceErrorType ErrorType { get; }

    protected ServiceResult(bool isSuccess, string? errorMessage, ServiceErrorType errorType)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorType = errorType;
    }

    public static ServiceResult Success() =>
        new(true, null, ServiceErrorType.None);

    public static ServiceResult Failure(string errorMessage, ServiceErrorType errorType = ServiceErrorType.BadRequest) =>
        new(false, errorMessage, errorType);

    public static ServiceResult BadRequest(string errorMessage) =>
        Failure(errorMessage, ServiceErrorType.BadRequest);

    public static ServiceResult NotFound(string errorMessage = "Resource not found.") =>
        Failure(errorMessage, ServiceErrorType.NotFound);

    public static ServiceResult Unauthorized(string errorMessage = "Unauthorized.") =>
        Failure(errorMessage, ServiceErrorType.Unauthorized);

    public static ServiceResult Conflict(string errorMessage) =>
        Failure(errorMessage, ServiceErrorType.Conflict);
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; }

    private ServiceResult(T data)
        : base(true, null, ServiceErrorType.None)
    {
        Data = data;
    }

    private ServiceResult(string errorMessage, ServiceErrorType errorType)
        : base(false, errorMessage, errorType)
    {
        Data = default;
    }

    public static ServiceResult<T> Success(T data) =>
        new(data);

    public static new ServiceResult<T> Failure(string errorMessage, ServiceErrorType errorType = ServiceErrorType.BadRequest) =>
        new(errorMessage, errorType);

    public static new ServiceResult<T> BadRequest(string errorMessage) =>
        new(errorMessage, ServiceErrorType.BadRequest);

    public static new ServiceResult<T> NotFound(string errorMessage = "Resource not found.") =>
        new(errorMessage, ServiceErrorType.NotFound);

    public static new ServiceResult<T> Unauthorized(string errorMessage = "Unauthorized.") =>
        new(errorMessage, ServiceErrorType.Unauthorized);

    public static new ServiceResult<T> Conflict(string errorMessage) =>
        new(errorMessage, ServiceErrorType.Conflict);
}

