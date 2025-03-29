using System.Text.Json;
using AnotherJsonLib.Infra;

namespace AnotherJsonLib.Utility;

public static class JsonValueTransformer
{
    /// <summary>
    /// Recursively transforms all string values in the JSON using the provided function.
    /// </summary>
    /// <param name="json">The input JSON string.</param>
    /// <param name="valueTransform">A function to transform string values.</param>
    /// <returns>A new JSON string with transformed string values.</returns>
    public static string TransformStringValues(string json, Func<string, string> valueTransform)
    {
        using var document = JsonDocument.Parse(json);
        object? transformed = TransformStringValues(document.RootElement, valueTransform);
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        return JsonSerializer.Serialize(transformed, options);
    }

    private static object? TransformStringValues(JsonElement element, Func<string, string> valueTransform)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    dict[property.Name] = TransformStringValues(property.Value, valueTransform);
                }

                return dict;
            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(TransformStringValues(item, valueTransform));
                }

                return list;
            case JsonValueKind.String:
                return valueTransform(element.GetString()!);
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