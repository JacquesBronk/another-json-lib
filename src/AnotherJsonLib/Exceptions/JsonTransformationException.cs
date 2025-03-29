namespace AnotherJsonLib.Exceptions;

public class JsonTransformationException : JsonLibException
{
    public JsonTransformationException(string message) : base(message) { }
    public JsonTransformationException(string message, Exception innerException) : base(message, innerException) { }
}
