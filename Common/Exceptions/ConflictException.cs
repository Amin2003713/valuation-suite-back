namespace Common.Exceptions;

public class ConflictException : ValuationException
{
    public ConflictException(
        string message = "Conflict occurred.",
        object? additionalData = null,
        Exception? innerException = null)
        : base(
            message,
            HttpStatusCode.Conflict,
            innerException,
            additionalData)
    {
    }
}
