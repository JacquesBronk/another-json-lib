using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Helper;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Transformation;

/// <summary>
/// Provides methods for transforming property names in JSON documents.
/// </summary>
/// <remarks>
/// JsonPropertyTransformer allows for the systematic renaming of properties in JSON structures
/// while preserving the values and overall structure. This is useful for:
/// 
/// - Converting between different naming conventions (camelCase, PascalCase, snake_case)
/// - Mapping between different API formats
/// - Anonymizing or obfuscating data
/// - Implementing custom transformation rules
/// </remarks>
public static partial class JsonPropertyTransformer
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonPropertyTransformer));
    
    /// <summary>
    /// Recursively transforms property names in the JSON using the provided function.
    /// </summary>
    /// <param name="json">The input JSON string.</param>
    /// <param name="propertyTransform">A function to transform property names.</param>
    /// <returns>A new JSON string with transformed property names.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json or propertyTransform is null.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonTransformationException">Thrown when the transformation fails.</exception>
    /// <example>
    /// <code>
    /// // Convert property names from camelCase to PascalCase
    /// string camelCaseJson = @"{""firstName"":""John"",""lastName"":""Doe"",""age"":30}";
    /// 
    /// string pascalCaseJson = JsonPropertyTransformer.TransformPropertyNames(
    ///     camelCaseJson,
    ///     propertyName => char.ToUpper(propertyName[0]) + propertyName.Substring(1)
    /// );
    /// 
    /// // Result: {"FirstName":"John","LastName":"Doe","Age":30}
    /// </code>
    /// </example>
    public static string TransformPropertyNames(string json, Func<string, string> propertyTransform)
    {
        using var performance = new PerformanceTracker(Logger, nameof(TransformPropertyNames));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            // Validate inputs
            ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
            ExceptionHelpers.ThrowIfNull(propertyTransform, nameof(propertyTransform));
            
            Logger.LogDebug("Transforming property names in JSON of length {Length}", json.Length);
            
            using var document = JsonDocument.Parse(json);
            object? transformed = TransformPropertyNames(document.RootElement, propertyTransform);
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            string result = JsonSerializer.Serialize(transformed, options);
            Logger.LogDebug("Successfully transformed property names: {OriginalLength} chars to {ResultLength} chars", 
                json.Length, result.Length);
                
            return result;
        },
        (ex, msg) => {
            if (ex is JsonException)
                return new JsonParsingException("Invalid JSON format in property name transformation", ex);
                
            return new JsonTransformationException($"Failed to transform property names: {msg}", ex);
        },
        "Failed to transform JSON property names") ?? string.Empty;
    }
    
    /// <summary>
    /// Attempts to transform property names in a JSON string without throwing exceptions.
    /// </summary>
    /// <param name="json">The input JSON string.</param>
    /// <param name="propertyTransform">A function to transform property names.</param>
    /// <param name="result">When successful, contains the transformed JSON; otherwise, empty string.</param>
    /// <returns>True if transformation was successful; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// // Safe property transformation that won't throw exceptions
    /// if (JsonPropertyTransformer.TryTransformPropertyNames(
    ///     inputJson,
    ///     name => name.ToUpperInvariant(),
    ///     out string transformedJson))
    /// {
    ///     Console.WriteLine("Transformation successful: " + transformedJson);
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Transformation failed");
    /// }
    /// </code>
    /// </example>
    public static bool TryTransformPropertyNames(string json, Func<string, string> propertyTransform, out string result)
    {
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => TransformPropertyNames(json, propertyTransform),
            string.Empty,
            "Failed to transform JSON property names"
        ) ?? string.Empty;
        
        return !string.IsNullOrEmpty(result);
    }

    /// <summary>
    /// Internal recursive method that transforms property names in a JsonElement.
    /// </summary>
    /// <param name="element">The JsonElement to transform.</param>
    /// <param name="propertyTransform">A function to transform property names.</param>
    /// <returns>A transformed object with new property names.</returns>
    /// <exception cref="JsonTransformationException">Thrown when transformation of an element fails.</exception>
    private static object? TransformPropertyNames(JsonElement element, Func<string, string> propertyTransform)
    {
        return ExceptionHelpers.SafeExecute<object?>(() => {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var property in element.EnumerateObject())
                    {
                        string newKey = propertyTransform(property.Name);
                        // If newKey is null or empty, skip adding this property.
                        if (string.IsNullOrWhiteSpace(newKey))
                            continue;
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
                    Logger.LogWarning("Unexpected JsonValueKind: {ValueKind}", element.ValueKind);
                    return element.ToString();
            }
        },
        (ex, msg) => new JsonTransformationException($"Failed to transform element of type {element.ValueKind}: {msg}", ex),
        $"Error transforming element of type {element.ValueKind}");
    }
}