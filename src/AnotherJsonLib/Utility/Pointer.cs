using System.Text.Json;

namespace AnotherJsonLib.Utility;

    public static partial class JsonTools
    {
        /// <summary>
        /// Evaluates a JSON Pointer against a JSON document.
        /// </summary>
        /// <param name="document">The JSON document to evaluate.</param>
        /// <param name="pointer">The JSON Pointer string.</param>
        /// <returns>
        /// The resulting JsonElement based on the pointer's navigation, or null if the path is not found.
        /// </returns>
        /// <remarks>
        /// <para>
        /// JSON Pointer provides a mechanism to navigate through a JSON document and locate specific pieces of data.
        /// It's particularly useful in scenarios where we want to selectively extract or manipulate parts of a JSON structure 
        /// without fully deserializing the entire document. Common use-cases include configuration management, data patching, 
        /// and advanced search operations within complex JSON structures.
        /// </para>
        /// <para>
        /// The JSON Pointer string should follow the RFC 6901 specification (https://datatracker.ietf.org/doc/html/rfc6901).
        /// </para>
        /// </remarks>
        public static JsonElement? EvaluatePointer(this JsonDocument? document, string pointer)
        {
            if (document == null || string.IsNullOrEmpty(pointer))
                return null;

            if (pointer == "/")
                return document.RootElement;

            var tokens = pointer.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentElement = document.RootElement;

            foreach (var token in tokens)
            {
                var decodedToken = Uri.UnescapeDataString(token);
                if (currentElement.ValueKind == JsonValueKind.Array && int.TryParse(decodedToken, out int arrayIndex))
                {
                    if (arrayIndex >= 0 && arrayIndex < currentElement.GetArrayLength())
                    {
                        currentElement = currentElement[arrayIndex];
                    }
                    else
                    {
                        return null;  // Index out of bounds
                    }
                }
                else if (currentElement.ValueKind == JsonValueKind.Object)
                {
                    if (currentElement.TryGetProperty(decodedToken, out var property))
                    {
                        currentElement = property;
                    }
                    else
                    {
                        return null;  // Property not found
                    }
                }
                else
                {
                    return null;  // Can't navigate further
                }
            }

            return currentElement;
        }
    }

