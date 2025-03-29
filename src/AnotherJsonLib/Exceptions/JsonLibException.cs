namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Base exception for all AnotherJsonLib exceptions.
/// </summary>
public class JsonLibException : Exception
{
    /// <summary>
    /// Represents the base exception type for all exceptions in the AnotherJsonLib library.
    /// </summary>
    protected JsonLibException()
    {
    }

    /// <summary>
    /// Serves as the base exception for all exceptions within the AnotherJsonLib library.
    /// </summary>
    protected JsonLibException(string message) : base(message)
    {
    }

    /// <summary>
    /// Represents the base exception type for exceptions thrown in the AnotherJsonLib library.
    /// </summary>
    public JsonLibException(string message, Exception innerException) : base(message, innerException)
    {
    }
}