namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Represents errors that occur during JSON transformation operations.
/// This exception is thrown when a transformation function fails to process JSON data
/// properly or produces an invalid result.
/// </summary>
public class JsonTransformationException : JsonLibException
{
    /// <summary>
    /// Initializes a new instance of the JsonTransformationException class.
    /// </summary>
    public JsonTransformationException() : base() { }
    
    /// <summary>
    /// Initializes a new instance of the JsonTransformationException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonTransformationException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the JsonTransformationException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonTransformationException(string message, Exception innerException) : base(message, innerException) { }
}