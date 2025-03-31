namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Thrown when an invalid argument is provided to a JSON operation.
/// </summary>
public class JsonArgumentException : JsonLibException
{
    public string? ParamName { get; set; }
    
    /// <summary>
    /// Exception thrown when an invalid argument is provided in a JSON-related operation.
    /// Inherits from <see cref="JsonLibException"/>.
    /// </summary>
    public JsonArgumentException(string message) : base(message) { }

    /// <summary>
    /// Exception thrown when an invalid argument is provided during a JSON operation.
    /// Inherits from <see cref="JsonLibException"/>.
    /// </summary>
    public JsonArgumentException(string message, Exception innerException) : base(message, innerException)
    {
    }
    
    /// <summary>
    /// Exception thrown when an invalid argument is provided during a JSON operation.
    /// Inherits from <see cref="JsonLibException"/>.
    /// </summary>
    public JsonArgumentException(string message, string? paramName) : base(message)
    {
        ParamName = paramName;
    }
}