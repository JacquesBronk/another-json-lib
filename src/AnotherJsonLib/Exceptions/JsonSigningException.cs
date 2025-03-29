namespace AnotherJsonLib.Exceptions;

public class JsonSigningException : JsonLibException
{
    public JsonSigningException(string message) : base(message) { }
    public JsonSigningException(string message, Exception innerException) : base(message, innerException) { }
}