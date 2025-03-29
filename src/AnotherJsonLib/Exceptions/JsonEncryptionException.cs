namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Represents errors that occur during JSON encryption or decryption operations.
/// </summary>
public class JsonEncryptionException : JsonLibException
{
    /// <summary>
    /// Initializes a new instance of the JsonEncryptionException class.
    /// </summary>
    public JsonEncryptionException() : base() { }
    
    /// <summary>
    /// Initializes a new instance of the JsonEncryptionException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonEncryptionException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the JsonEncryptionException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonEncryptionException(string message, Exception innerException) : base(message, innerException) { }
}
