namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Thrown when JSON parsing or serialization fails.
/// </summary>
public class JsonParsingException : JsonLibException
{
    /// <summary>
    /// Exception thrown when there is a failure during JSON parsing or serialization.
    /// </summary>
    public JsonParsingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Exception thrown when an error occurs specifically during the parsing of JSON data.
    /// </summary>
    public JsonParsingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}