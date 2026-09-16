namespace Common.Exceptions;

public class ForbiddenException : ValuationException
{
    public ForbiddenException(
        string message,
        Exception? innerException = null,
        object? additionalData = null)
        : base(
            message,
            HttpStatusCode.Forbidden,
            innerException,
            additionalData)
    {
    }
}
