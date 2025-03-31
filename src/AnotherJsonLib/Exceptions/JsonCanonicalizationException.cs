namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Thrown when JSON canonicalization process fails.
/// </summary>
public class JsonCanonicalizationException : JsonLibException
{
    /// <summary>
    /// Represents an exception that is thrown during the JSON canonicalization process when an error occurs.
    /// </summary>
    public JsonCanonicalizationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Represents an exception that is thrown during the JSON canonicalization process
    /// when an error occurs due to a specific issue or failure in the operation.
    /// </summary>
    public JsonCanonicalizationException(string message, Exception innerException) : base(message, innerException) { }
}