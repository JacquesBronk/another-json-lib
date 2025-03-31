namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Represents errors that occur during JSON signing or signature verification operations.
/// This exception is thrown when cryptographic operations on JSON data fail due to
/// invalid keys, incorrect algorithms, or other security-related issues.
/// </summary>
public class JsonSigningException : JsonLibException
{
    /// <summary>
    /// Initializes a new instance of the JsonSigningException class.
    /// </summary>
    public JsonSigningException()
    { }
    
    /// <summary>
    /// Initializes a new instance of the JsonSigningException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonSigningException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the JsonSigningException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonSigningException(string message, Exception innerException) : base(message, innerException) { }
}