namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Thrown when JSON parsing or serialization fails.
/// </summary>
public class JsonParsingException : JsonLibException
{
    public JsonParsingException(string message) : base(message) { }
    public JsonParsingException(string message, Exception innerException) : base(message, innerException) { }
}