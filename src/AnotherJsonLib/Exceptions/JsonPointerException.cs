﻿namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Thrown when an error occurs during JSON Pointer (RFC6901) evaluation operations.
/// This exception indicates issues such as invalid pointer syntax or pointer resolution failures.
/// </summary>
public class JsonPointerException : JsonLibException
{
    /// <summary>
    /// Initializes a new instance of the JsonPointerException class.
    /// </summary>
    public JsonPointerException() : base() { }
    
    /// <summary>
    /// Initializes a new instance of the JsonPointerException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonPointerException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the JsonPointerException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonPointerException(string message, Exception innerException) : base(message, innerException) { }
}