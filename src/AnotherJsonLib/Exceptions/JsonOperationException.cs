namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Thrown when JSON manipulation operations (merge, diff, etc.) encounter an error.
/// </summary>
public class JsonOperationException : JsonLibException
{
    /// <summary>
    /// Represents an exception that is thrown when a JSON operation, such as merge or diff, fails.
    /// </summary>
    public JsonOperationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Represents an exception that occurs during a JSON-related operation such as formatting, patch generation, or manipulation.
    /// </summary>
    public JsonOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}