using System.Text.Json;
using AnotherJsonLib.Infra;

namespace AnotherJsonLib.Utility.Transformation;

 public static class JsonPropertyTransformer
    {
        /// <summary>
        /// Recursively transforms property names in the JSON using the provided function.
        /// </summary>
        /// <param name="json">The input JSON string.</param>
        /// <param name="propertyTransform">A function to transform property names.</param>
        /// <returns>A new JSON string with transformed property names.</returns>
        public static string TransformPropertyNames(string json, Func<string, string> propertyTransform)
        {
            using var document = JsonDocument.Parse(json);
            object? transformed = TransformPropertyNames(document.RootElement, propertyTransform);
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(transformed, options);
        }

        private static object? TransformPropertyNames(JsonElement element, Func<string, string> propertyTransform)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var property in element.EnumerateObject())
                    {
                        string newKey = propertyTransform(property.Name);
                        dict[newKey] = TransformPropertyNames(property.Value, propertyTransform);
                    }
                    return dict;
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(TransformPropertyNames(item, propertyTransform));
                    }
                    return list;
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    return element.CloneValue();
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