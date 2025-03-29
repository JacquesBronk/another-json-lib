namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Represents errors that occur during JSON formatting operations.
/// This exception is thrown when minification, prettification, or other
/// formatting operations fail due to invalid input or formatting errors.
/// </summary>
public class JsonFormattingException : JsonLibException
{
    /// <summary>
    /// Initializes a new instance of the JsonFormattingException class.
    /// </summary>
    public JsonFormattingException() : base() { }
    
    /// <summary>
    /// Initializes a new instance of the JsonFormattingException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonFormattingException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the JsonFormattingException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonFormattingException(string message, Exception innerException) : base(message, innerException) { }
}