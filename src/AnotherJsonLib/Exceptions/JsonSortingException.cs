namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Represents errors that occur during JSON property sorting operations.
/// This exception is thrown when the sorting process encounters issues such as
/// invalid property types or structural problems in the JSON.
/// </summary>
public class JsonSortingException : JsonLibException
{
    /// <summary>
    /// Initializes a new instance of the JsonSortingException class.
    /// </summary>
    public JsonSortingException()
    { }
    
    /// <summary>
    /// Initializes a new instance of the JsonSortingException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonSortingException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the JsonSortingException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonSortingException(string message, Exception innerException) : base(message, innerException) { }
}