namespace AnotherJsonLib.Exceptions;

/// <summary>
/// Thrown when JSON canonicalization process fails.
/// </summary>
public class JsonCanonicalizationException : JsonLibException
{
    public JsonCanonicalizationException(string message) : base(message) { }
    public JsonCanonicalizationException(string message, Exception innerException) : base(message, innerException) { }
}