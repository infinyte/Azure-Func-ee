using System.Text.Json.Serialization;

namespace AzureFunctions.Shared.Models;

/// <summary>
/// A generic result wrapper that encapsulates either a successful outcome with data
/// or a failure with a structured error response.
/// </summary>
/// <typeparam name="T">The type of data returned on success.</typeparam>
public record OperationResult<T>
{
    /// <summary>
    /// Indicates whether the operation completed successfully.
    /// </summary>
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The result data, available when <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; init; }

    /// <summary>
    /// The structured error details, available when <see cref="IsSuccess"/> is <c>false</c>.
    /// </summary>
    [JsonPropertyName("error")]
    public ErrorResponse? Error { get; init; }

    /// <summary>
    /// An optional human-readable message providing context about the operation outcome.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// Creates a successful result containing the specified data.
    /// </summary>
    /// <param name="data">The result data.</param>
    /// <param name="message">An optional success message.</param>
    /// <returns>A successful <see cref="OperationResult{T}"/>.</returns>
    public static OperationResult<T> Success(T data, string? message = null) =>
        new()
        {
            IsSuccess = true,
            Data = data,
            Message = message
        };

    /// <summary>
    /// Creates a failure result with the specified error information.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="errorCode">A machine-readable error code.</param>
    /// <param name="detail">Optional additional error detail.</param>
    /// <returns>A failed <see cref="OperationResult{T}"/>.</returns>
    public static OperationResult<T> Failure(string message, string errorCode, string? detail = null) =>
        new()
        {
            IsSuccess = false,
            Error = new ErrorResponse
            {
                Message = message,
                ErrorCode = errorCode,
                Detail = detail
            }
        };
}
