namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Thrown when an invalid argument is provided to a JSON operation.
/// </summary>
public class JsonArgumentException : JsonLibException
{
    public JsonArgumentException(string message) : base(message) { }
    public JsonArgumentException(string message, Exception innerException) : base(message, innerException) { }
}