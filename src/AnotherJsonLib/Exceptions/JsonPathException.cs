namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Represents errors that occur during JSON Path query operations.
/// This exception is thrown when a JSON Path query fails to execute properly
/// due to invalid syntax or other query-related issues.
/// </summary>
public class JsonPathException : JsonLibException
{
    /// <summary>
    /// Initializes a new instance of the JsonPathException class.
    /// </summary>
    public JsonPathException() { }
    
    /// <summary>
    /// Initializes a new instance of the JsonPathException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonPathException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the JsonPathException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonPathException(string message, Exception innerException) : base(message, innerException) { }
}