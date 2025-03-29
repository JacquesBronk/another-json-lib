using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Transformation;

public static class JsonValueTransformer
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonValueTransformer));
    
    public static string TransformStringValues(string json, Func<string, string> valueTransform)
    {
        using var performance = new PerformanceTracker(Logger, nameof(TransformStringValues));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(valueTransform, nameof(valueTransform));
        
        return ExceptionHelpers.SafeExecute(() =>
        {
            using var document = JsonDocument.Parse(json);
            object? transformed = TransformStringValues(document.RootElement, valueTransform);
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(transformed, options);
        },
        (ex, msg) => {
            if (ex is JsonException)
                return new JsonParsingException("Invalid JSON format in string value transformation", ex);
            return new JsonTransformationException($"Failed to transform string values: {msg}", ex);
        },
        "Failed to transform JSON string values") ?? string.Empty;
    }
    
    // Add a "Try" variant
    public static bool TryTransformStringValues(string json, Func<string, string> valueTransform, out string result)
    {
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => TransformStringValues(json, valueTransform),
            string.Empty,
            "Failed to transform JSON string values"
        ) ?? string.Empty;
        
        return !string.IsNullOrEmpty(result);
    }
    
    // Update the private implementation method
    private static object? TransformStringValues(JsonElement element, Func<string, string> valueTransform)
    {
        return ExceptionHelpers.SafeExecute(() => {
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
                    string value = element.GetString() ?? string.Empty;
                    return valueTransform(value);
                    
                // Handle other primitive types as before
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
        },
        (ex, msg) => new JsonTransformationException($"Failed to transform element of type {element.ValueKind}: {msg}", ex),
        $"Error transforming element of type {element.ValueKind}");
    }
}