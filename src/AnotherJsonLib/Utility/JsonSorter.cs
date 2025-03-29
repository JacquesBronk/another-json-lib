using System.Text.Encodings.Web;
using System.Text.Json;

namespace AnotherJsonLib.Utility;

 public static class JsonSorter
    {
        /// <summary>
        /// Returns a normalized JSON string with all object keys sorted in lexicographical order.
        /// This is useful for creating a canonical representation of JSON data.
        /// </summary>
        /// <param name="json">The input JSON string.</param>
        /// <returns>A normalized JSON string with sorted keys.</returns>
        public static string SortJson(string json)
        {
            using var document = JsonDocument.Parse(json);
            var normalizedObject = NormalizeValue(document.RootElement);
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                // Use an encoder that minimizes escaping for performance and readability.
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(normalizedObject, options);
        }

        /// <summary>
        /// Recursively normalizes a JsonElement to a .NET object:
        /// - Objects are converted to SortedDictionary to sort keys.
        /// - Arrays are normalized element-by-element.
        /// - Other primitives are returned as-is.
        /// </summary>
        private static object? NormalizeValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    // SortedDictionary ensures keys are in order.
                    var sortedDict = new SortedDictionary<string, object?>(StringComparer.Ordinal);
                    foreach (var property in element.EnumerateObject())
                    {
                        sortedDict[property.Name] = NormalizeValue(property.Value);
                    }
                    return sortedDict;
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(NormalizeValue(item));
                    }
                    return list;
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    // Preserve the number by converting using the raw text.
                    // Alternatively, you might parse to a numeric type if needed.
                    return element.GetRawText();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();
                case JsonValueKind.Null:
                    return null;
                default:
                    return element.ToString();
            }
        }
    }