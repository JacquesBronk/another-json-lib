namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Thrown when JSON manipulation operations (merge, diff, etc.) encounter an error.
/// </summary>
public class JsonOperationException : JsonLibException
{
    public JsonOperationException(string message) : base(message) { }
    public JsonOperationException(string message, Exception innerException) : base(message, innerException) { }
}