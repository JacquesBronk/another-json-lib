using System.Text.Json;

namespace AnotherJsonLib.Utility;

/// <summary>
/// Provides an extension method to evaluate a JSON Pointer (RFC6901) against a JsonDocument.
/// </summary>
public static class JsonPointer
{
    /// <summary>
    /// Evaluates a JSON Pointer against a JsonDocument.
    /// The empty string ("") references the entire document.
    /// The pointer must start with "/" if it is non-empty.
    /// </summary>
    /// <param name="document">The JsonDocument to evaluate.</param>
    /// <param name="pointer">A JSON Pointer string per RFC6901.</param>
    /// <returns>
    /// The JsonElement referenced by the pointer, or null if the pointer does not resolve to any value.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if document or pointer is null.</exception>
    /// <exception cref="ArgumentException">Thrown if a non-empty pointer does not start with "/".</exception>
    public static JsonElement? EvaluatePointer(this JsonDocument document, string pointer)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));
        if (pointer == null)
            throw new ArgumentNullException(nameof(pointer));

        // The empty string references the entire document.
        if (pointer == "")
            return document.RootElement;

        if (!pointer.StartsWith("/"))
            throw new ArgumentException("A non-empty JSON Pointer must start with '/'", nameof(pointer));

        // Split the pointer into tokens.
        // The first token is always empty because the pointer starts with '/'
        var tokens = pointer.Split('/');

        JsonElement current = document.RootElement;
        // Process tokens from index 1 onward.
        for (int i = 1; i < tokens.Length; i++)
        {
            // Decode per RFC6901: "~1" becomes "/" and "~0" becomes "~"
            string token = tokens[i].Replace("~1", "/").Replace("~0", "~");

            if (current.ValueKind == JsonValueKind.Object)
            {
                if (current.TryGetProperty(token, out JsonElement property))
                    current = property;
                else
                    return null; // Property not found.
            }
            else if (current.ValueKind == JsonValueKind.Array)
            {
                if (int.TryParse(token, out int index))
                {
                    if (index >= 0 && index < current.GetArrayLength())
                        current = current[index];
                    else
                        return null; // Array index out of bounds.
                }
                else
                {
                    return null; // Invalid array index.
                }
            }
            else
            {
                // Cannot traverse further if the current element is a primitive.
                return null;
            }
        }

        return current;
    }
}