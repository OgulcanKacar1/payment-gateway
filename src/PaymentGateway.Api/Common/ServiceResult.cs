namespace PaymentGateway.Api.Common;

public class ServiceResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public ServiceErrorType ErrorType { get; init; } = ServiceErrorType.None;
    
    public static ServiceResult<T> Success(T data) =>
        new() {IsSuccess = true, Data = data};
    
    public static ServiceResult<T> Failure(string errorMessage, ServiceErrorType errorType) =>
        new() {IsSuccess = false, ErrorMessage = errorMessage, ErrorType = errorType};
}