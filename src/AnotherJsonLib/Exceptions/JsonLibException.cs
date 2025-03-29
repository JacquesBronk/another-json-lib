namespace AnotherJsonLib.Exceptions;

/// <summary>
    /// Base exception for all AnotherJsonLib exceptions.
    /// </summary>
    public class JsonLibException : Exception
    {
        public JsonLibException(): base() { }
        public JsonLibException(string message) : base(message) { }
        public JsonLibException(string message, Exception innerException) : base(message, innerException) { }
    }