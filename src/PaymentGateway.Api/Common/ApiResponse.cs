namespace PaymentGateway.Api.Common;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    
    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() {Success = true, Data = data, Message = message};
    
    public static ApiResponse<T> Fail(string message) =>
        new() {Success = false, Message = message};
    
}