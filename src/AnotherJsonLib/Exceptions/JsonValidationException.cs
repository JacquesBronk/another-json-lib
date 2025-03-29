namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Thrown when JSON validation against a schema fails.
/// </summary>
public class JsonValidationException : JsonLibException
{
    public JsonValidationException(string message) : base(message) { }
    public JsonValidationException(string message, Exception innerException) : base(message, innerException) { }
}