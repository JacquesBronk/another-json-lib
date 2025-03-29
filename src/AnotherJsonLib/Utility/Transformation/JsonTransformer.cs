using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Transformation;

/// <summary>
/// Provides functionality for transforming JSON elements recursively with customizable transformation functions.
/// 
/// This utility enables powerful manipulation of JSON data by applying custom transformations to 
/// JSON elements while preserving the original structure. It can be used to:
/// 
/// - Modify specific values while keeping the JSON structure intact
/// - Filter out values based on custom criteria
/// - Perform calculations or string manipulations on JSON values
/// - Apply conditional transformations based on path or value
/// - Convert between different data representations
/// 
/// <example>
/// <code>
/// // Example: Multiply all numeric values by 2
/// string json = @"{""values"": [1, 2, 3], ""nested"": {""value"": 4}}";
/// using var doc = JsonDocument.Parse(json);
/// 
/// // Define a transform function that doubles numbers
/// object? DoubleNumbers(object? value)
/// {
///     if (value is long longValue)
///         return longValue * 2;
///     if (value is decimal decimalValue)
///         return decimalValue * 2;
///     return value; // Return other types unchanged
/// }
/// 
/// // Apply the transformation
/// var result = JsonTransformer.Transform(doc.RootElement, DoubleNumbers);
/// // Result: {"values": [2, 4, 6], "nested": {"value": 8}}
/// </code>
/// </example>
/// </summary>
public static class JsonTransformer
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonTransformer));

    /// <summary>
    /// Recursively transforms a JsonElement into a native .NET object, applying a custom transformation function 
    /// after processing all children. The transformation function receives the transformed value of the current node 
    /// (which may be a primitive, dictionary, or list) and returns the new value.
    /// 
    /// If the transformation function is the identity (returns its input), this method effectively clones the JSON.
    /// 
    /// <example>
    /// <code>
    /// // Transform all strings to uppercase
    /// string json = @"{""name"": ""john"", ""items"": [""apple"", ""banana""]}";
    /// using var doc = JsonDocument.Parse(json);
    /// 
    /// object? UppercaseStrings(object? value)
    /// {
    ///     if (value is string strValue)
    ///         return strValue.ToUpper();
    ///     return value;
    /// }
    /// 
    /// var transformed = JsonTransformer.Transform(doc.RootElement, UppercaseStrings);
    /// // Result: {"name": "JOHN", "items": ["APPLE", "BANANA"]}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="element">The source JsonElement.</param>
    /// <param name="transformFunc">
    /// A function that takes the current transformed node (object, list, or primitive) and returns a new value.
    /// </param>
    /// <returns>A native .NET object representing the transformed JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when transformFunc is null.</exception>
    /// <exception cref="JsonTransformationException">Thrown when the transformation fails.</exception>
    public static object? Transform(JsonElement element, Func<object?, object?> transformFunc)
    {
        using var performance = new PerformanceTracker(Logger, nameof(Transform));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNull(transformFunc, nameof(transformFunc));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            Logger.LogTrace("Transforming JSON element of type {ValueKind}", element.ValueKind);
            
            // Recursively transform children first.
            object? transformedValue;
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var property in element.EnumerateObject())
                    {
                        dict[property.Name] = Transform(property.Value, transformFunc);
                    }

                    transformedValue = dict;
                    break;

                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(Transform(item, transformFunc));
                    }

                    transformedValue = list;
                    break;

                default:
                    transformedValue = element.CloneValue();
                    break;
            }

            // Apply the transformation function to the current node.
            var result = transformFunc(transformedValue);
            Logger.LogTrace("Transformed JSON element from {OriginalType} to {ResultType}", 
                transformedValue?.GetType().Name ?? "null", 
                result?.GetType().Name ?? "null");
                
            return result;
        }, 
        (ex, msg) => new JsonTransformationException($"Failed to transform JSON element: {msg}", ex),
        "JSON transformation error occurred");
    }
    
    /// <summary>
    /// Recursively transforms a JsonElement with a transformation function that has access to the current JSON path.
    /// This allows for targeted transformations based on element location within the document.
    /// 
    /// <example>
    /// <code>
    /// // Transform only values in the "sensitive" path
    /// string json = @"{""public"": ""visible"", ""sensitive"": {""ssn"": ""123-45-6789"", ""pin"": ""1234""}}";
    /// using var doc = JsonDocument.Parse(json);
    /// 
    /// object? MaskSensitiveData(string path, object? value)
    /// {
    ///     if (path.StartsWith("/sensitive/") && value is string)
    ///         return "********";
    ///     return value;
    /// }
    /// 
    /// var transformed = JsonTransformer.TransformWithPath(doc.RootElement, MaskSensitiveData);
    /// // Result: {"public": "visible", "sensitive": {"ssn": "********", "pin": "********"}}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="element">The source JsonElement.</param>
    /// <param name="transformFunc">
    /// A function that takes the current JSON path and node value and returns a new value.
    /// </param>
    /// <returns>A native .NET object representing the transformed JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when transformFunc is null.</exception>
    /// <exception cref="JsonTransformationException">Thrown when the transformation fails.</exception>
    public static object? TransformWithPath(JsonElement element, Func<string, object?, object?> transformFunc)
    {
        using var performance = new PerformanceTracker(Logger, nameof(TransformWithPath));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNull(transformFunc, nameof(transformFunc));
        
        return TransformWithPathInternal(element, transformFunc, "");
    }
    
    private static object? TransformWithPathInternal(JsonElement element, Func<string, object?, object?> transformFunc, string currentPath)
    {
        return ExceptionHelpers.SafeExecute(() => 
        {
            Logger.LogTrace("Transforming JSON element at path {Path} of type {ValueKind}", 
                currentPath, element.ValueKind);
            
            // Recursively transform children first.
            object? transformedValue;
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var property in element.EnumerateObject())
                    {
                        string propertyPath = string.IsNullOrEmpty(currentPath) 
                            ? $"/{property.Name}" 
                            : $"{currentPath}/{property.Name}";
                            
                        dict[property.Name] = TransformWithPathInternal(
                            property.Value, transformFunc, propertyPath);
                    }

                    transformedValue = dict;
                    break;

                case JsonValueKind.Array:
                    var list = new List<object?>();
                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        string itemPath = $"{currentPath}/{index}";
                        list.Add(TransformWithPathInternal(item, transformFunc, itemPath));
                        index++;
                    }

                    transformedValue = list;
                    break;

                default:
                    transformedValue = element.CloneValue();
                    break;
            }

            // Apply the transformation function to the current node.
            var result = transformFunc(currentPath, transformedValue);
            Logger.LogTrace("Transformed JSON element at path {Path} from {OriginalType} to {ResultType}", 
                currentPath,
                transformedValue?.GetType().Name ?? "null", 
                result?.GetType().Name ?? "null");
                
            return result;
        }, 
        (ex, msg) => new JsonTransformationException($"Failed to transform JSON element at path '{currentPath}': {msg}", ex),
        $"JSON transformation error occurred at path '{currentPath}'");
    }
    
    /// <summary>
    /// Conditionally transforms a JsonElement based on a predicate function.
    /// Only applies the transformation when the predicate returns true.
    /// 
    /// <example>
    /// <code>
    /// // Only transform numeric values greater than 10
    /// string json = @"{""values"": [5, 15, 25], ""threshold"": 10}";
    /// using var doc = JsonDocument.Parse(json);
    /// 
    /// bool IsLargeNumber(object? value)
    /// {
    ///     return value is long num && num > 10;
    /// }
    /// 
    /// object? HalfValue(object? value)
    /// {
    ///     if (value is long num)
    ///         return num / 2;
    ///     return value;
    /// }
    /// 
    /// var transformed = JsonTransformer.TransformConditional(doc.RootElement, IsLargeNumber, HalfValue);
    /// // Result: {"values": [5, 7, 12], "threshold": 10}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="element">The source JsonElement.</param>
    /// <param name="predicate">A function that determines whether to apply the transformation.</param>
    /// <param name="transformFunc">A function that transforms the value if the predicate is true.</param>
    /// <returns>A native .NET object representing the transformed JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when predicate or transformFunc is null.</exception>
    /// <exception cref="JsonTransformationException">Thrown when the transformation fails.</exception>
    public static object? TransformConditional(
        JsonElement element, 
        Func<object?, bool> predicate, 
        Func<object?, object?> transformFunc)
    {
        using var performance = new PerformanceTracker(Logger, nameof(TransformConditional));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNull(predicate, nameof(predicate));
        ExceptionHelpers.ThrowIfNull(transformFunc, nameof(transformFunc));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            return Transform(element, value => 
            {
                if (predicate(value))
                {
                    Logger.LogTrace("Condition matched, applying transformation to value of type {ValueType}", 
                        value?.GetType().Name ?? "null");
                    return transformFunc(value);
                }
                
                return value;
            });
        },
        (ex, msg) => new JsonTransformationException($"Failed to conditionally transform JSON element: {msg}", ex),
        "Conditional JSON transformation error occurred");
    }
    
    /// <summary>
    /// Transforms a JSON string by applying the specified transformation function.
    /// 
    /// <example>
    /// <code>
    /// // Add 10 to all numeric values
    /// string json = @"{""values"": [1, 2, 3]}";
    /// 
    /// object? AddTen(object? value)
    /// {
    ///     if (value is long num)
    ///         return num + 10;
    ///     return value;
    /// }
    /// 
    /// string result = JsonTransformer.TransformJson(json, AddTen);
    /// // Result: {"values": [11, 12, 13]}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The source JSON string.</param>
    /// <param name="transformFunc">The transformation function to apply.</param>
    /// <returns>A JSON string containing the transformed data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json or transformFunc is null.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonTransformationException">Thrown when the transformation fails.</exception>
    public static string TransformJson(string json, Func<object?, object?> transformFunc)
    {
        using var performance = new PerformanceTracker(Logger, nameof(TransformJson));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(transformFunc, nameof(transformFunc));
        
        return ExceptionHelpers.SafeExecute(() => 
            {
                Logger.LogDebug("Transforming JSON string of length {Length}", json.Length);
            
                // Parse the JSON
                using var document = JsonDocument.Parse(json);
            
                // Transform the root element
                var transformed = Transform(document.RootElement, transformFunc);
            
                // Serialize back to JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
            
                string result = JsonSerializer.Serialize(transformed, options);
                Logger.LogDebug("Successfully transformed JSON from {OriginalLength} to {ResultLength} characters", 
                    json.Length, result.Length);
                
                return result;
            },
            (ex, msg) => {
                if (ex is JsonException)
                    return new JsonParsingException("Invalid JSON format in transformation", ex);
                return new JsonTransformationException($"Failed to transform JSON string: {msg}", ex);
            },
            "Failed to transform JSON string") ?? string.Empty;
    }
    
    /// <summary>
    /// Attempts to transform a JsonElement, returning a success indicator instead of throwing exceptions.
    /// </summary>
    /// <param name="element">The source JsonElement.</param>
    /// <param name="transformFunc">The transformation function to apply.</param>
    /// <param name="result">When successful, contains the transformed object; otherwise, null.</param>
    /// <returns>True if the transformation was successful; otherwise, false.</returns>
    public static bool TryTransform(JsonElement element, Func<object?, object?>? transformFunc, out object? result)
    {
        if (transformFunc == null)
        {
            result = null;
            return false;
        }
        
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => Transform(element, transformFunc),
            null,
            "Failed to transform JSON element"
        );
        
        return result != null;
    }
    
    /// <summary>
    /// Attempts to transform a JSON string, returning a success indicator instead of throwing exceptions.
    /// </summary>
    /// <param name="json">The source JSON string.</param>
    /// <param name="transformFunc">The transformation function to apply.</param>
    /// <param name="result">When successful, contains the transformed JSON string; otherwise, empty.</param>
    /// <returns>True if the transformation was successful; otherwise, false.</returns>
    public static bool TryTransformJson(string json, Func<object?, object?>? transformFunc, out string result)
    {
        if (string.IsNullOrWhiteSpace(json) || transformFunc == null)
        {
            result = string.Empty;
            return false;
        }
        
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => TransformJson(json, transformFunc),
            string.Empty,
            "Failed to transform JSON string"
        ) ?? string.Empty;
        
        return !string.IsNullOrEmpty(result);
    }
}