namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Represents errors that occur during JSON comparison operations.
/// This exception is thrown when comparing JSON elements fails due to invalid
/// inputs, comparison failures, or hash code generation issues.
/// </summary>
public class JsonComparisonException : JsonLibException
{
    /// <summary>
    /// Initializes a new instance of the JsonComparisonException class.
    /// </summary>
    public JsonComparisonException()
    { }
    
    /// <summary>
    /// Initializes a new instance of the JsonComparisonException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonComparisonException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the JsonComparisonException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonComparisonException(string message, Exception innerException) : base(message, innerException) { }
}