namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Represents errors that occur during JSON compression or decompression operations.
/// This exception is thrown when compression or decompression processes fail due to
/// invalid data, unsupported algorithms, or other compression-related issues.
/// </summary>
public class JsonCompressionException : JsonLibException
{
    /// <summary>
    /// Initializes a new instance of the JsonCompressionException class.
    /// </summary>
    public JsonCompressionException()
    { }
    
    /// <summary>
    /// Initializes a new instance of the JsonCompressionException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonCompressionException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the JsonCompressionException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonCompressionException(string message, Exception innerException) : base(message, innerException) { }
}