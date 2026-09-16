namespace Common.Exceptions;

public class ValuationException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ValuationException" /> class with specified parameters.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="httpStatusCode">The HTTP status code.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="additionalData">Any additional data related to the exception.</param>
    public ValuationException(
        string? message = null,
        HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        object? additionalData = null)
        : base(message, innerException)
    {
        HttpStatusCode = httpStatusCode;
        AdditionalData = additionalData;
    }

    public HttpStatusCode HttpStatusCode { get; }
    public object? AdditionalData { get; }

    // Static factory methods for common scenarios

    public static ValuationException Create(string message)
        => new(message);

    public static ValuationException Create(string message, object additionalData)
        => new(message, additionalData: additionalData);

    public static ValuationException Create(HttpStatusCode httpStatusCode, string message)
        => new(httpStatusCode: httpStatusCode, message: message);

    public static ValuationException Create(
        string message,
        HttpStatusCode httpStatusCode,
        Exception innerException,
        object additionalData)
        => new(message, httpStatusCode, innerException, additionalData);

    #region Predefined API Exceptions

    /// <summary>
    ///     Creates a NotFound exception (HTTP 404).
    /// </summary>
    public static NotFoundException NotFound(string message = "Resource not found.", object? additionalData = null)
        => additionalData is null ? new NotFoundException(message) : new NotFoundException(message, additionalData);

    /// <summary>
    ///     Creates an Unauthorized exception (HTTP 401).
    /// </summary>
    public static UnauthorizedAccessException Unauthorized(
        string message = "Unauthorized access.",
        object? additionalData = null)
        => new(message + "\n" + additionalData);

    /// <summary>
    ///     Creates a BadRequest exception (HTTP 400).
    /// </summary>
    public static ValuationException BadRequest(string message = "Bad request.", object? additionalData = null)
        => new(message, HttpStatusCode.BadRequest, null, additionalData);

    /// <summary>
    ///     Creates a Validation exception (HTTP 400) with validation errors.
    /// </summary>
    public static ValuationException Validation(string message = "Validation failed.", object? additionalData = null)
        => new(message, HttpStatusCode.BadRequest, null, additionalData);

    /// <summary>
    ///     Creates a Conflict exception (HTTP 409).
    /// </summary>
    public static ConflictException Conflict(string message = "Conflict occurred.", object? additionalData = null)
        => new(message, additionalData);

    /// <summary>
    ///     Creates an InternalServerError exception (HTTP 500).
    /// </summary>
    public static ValuationException InternalServerError(
        string message = "An unexpected error occurred.",
        object? additionalData = null)
        => new(message, HttpStatusCode.InternalServerError, null, additionalData);

    public static ForbiddenException Forbidden(string message, object? additionalData = null)
        => new(message, null, additionalData);

    #endregion
}
