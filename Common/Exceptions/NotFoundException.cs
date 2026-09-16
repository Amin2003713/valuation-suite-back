namespace Common.Exceptions;

public class NotFoundException : ValuationException
{
    public NotFoundException(string message = "Resource not found.", object? additionalData = null)
        : base(
            message,
            HttpStatusCode.NotFound,
            additionalData: additionalData)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(
            message,
            HttpStatusCode.NotFound,
            innerException)
    {
    }
}
