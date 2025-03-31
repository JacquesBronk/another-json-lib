namespace AnotherJsonLib.Utility.Operations;

/// <summary>
/// Helpers for working with JsonDocument objects.
/// </summary>
internal static class JsonDocumentHelpers
{
    /// <summary>
    /// Determines if a JSON string is formatted with indentation.
    /// </summary>
    /// <param name="json">The JSON string to check.</param>
    /// <returns>True if the JSON is indented; otherwise, false.</returns>
    public static bool IsIndented(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;
            
        // Look for a newline followed by whitespace, which indicates indentation
        return json.Contains("\n ") || json.Contains("\n\t") || json.Contains("\r\n ");
    }
}