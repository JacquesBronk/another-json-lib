namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Thrown when JSON validation against a schema fails.
/// </summary>
public class JsonValidationException : JsonLibException
{
    /// <summary>
    /// Exception thrown when a JSON validation operation fails due to non-compliance with a given schema.
    /// </summary>
    public JsonValidationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Exception thrown when a JSON validation operation fails due to non-compliance with a given schema.
    /// </summary>
    public JsonValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}