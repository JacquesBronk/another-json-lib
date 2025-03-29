using System.Text.Encodings.Web;
using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility;

/// <summary>
/// Provides functionality for sorting JSON object properties consistently to create normalized representations.
/// 
/// JSON property ordering is not significant according to the JSON specification, but many use cases
/// benefit from consistent ordering, such as:
/// 
/// - Creating canonical representations for cryptographic operations
/// - Enabling meaningful comparisons between JSON objects
/// - Generating consistent hash values from JSON data
/// - Improving readability of JSON data for debugging or documentation
/// - Ensuring deterministic serialization for testing and verification
/// 
/// <example>
/// <code>
/// // Input JSON with arbitrary property order
/// string json = @"{
///   ""zip"": ""10001"",
///   ""city"": ""New York"",
///   ""name"": ""John Smith"",
///   ""address"": {
///     ""number"": 123,
///     ""street"": ""Broadway""
///   }
/// }";
/// 
/// // Sort the JSON properties lexicographically
/// string sorted = JsonSorter.SortJson(json);
/// 
/// // Result:
/// // {"address":{"number":123,"street":"Broadway"},"city":"New York","name":"John Smith","zip":"10001"}
/// </code>
/// </example>
/// </summary>
public static class JsonSorter
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonSorter));

    /// <summary>
    /// Returns a normalized JSON string with all object properties sorted in lexicographical order.
    /// The JSON structure remains the same, but property order is standardized throughout the entire document.
    /// 
    /// <example>
    /// <code>
    /// // Unsorted JSON
    /// string json = @"{""c"":3,""a"":1,""b"":{""z"":26,""y"":25,""x"":24}}";
    /// 
    /// // Sort properties
    /// string sorted = JsonSorter.SortJson(json);
    /// 
    /// // Result: {"a":1,"b":{"x":24,"y":25,"z":26},"c":3}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The input JSON string.</param>
    /// <param name="indented">Whether to format the output with indentation (default is false).</param>
    /// <returns>A normalized JSON string with sorted properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the json parameter is null.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonSortingException">Thrown when an error occurs during the sorting process.</exception>
    public static string SortJson(string json, bool indented = false)
    {
        using var performance = new PerformanceTracker(Logger, nameof(SortJson));
        
        // Validate input
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        
        return ExceptionHelpers.SafeExecute(() =>
            {
                Logger.LogDebug("Sorting JSON properties in {Length} character string", json.Length);
            
                using var document = JsonDocument.Parse(json);
                var sortedObject = NormalizeValue(document.RootElement);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = indented,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
            
                string result = JsonSerializer.Serialize(sortedObject, options);
                Logger.LogDebug("Successfully sorted JSON from {OriginalLength} to {ResultLength} characters", 
                    json.Length, result.Length);
                
                return result;
            },
            (ex, msg) => {
                if (ex is JsonException)
                    return new JsonParsingException("Failed to sort JSON: Invalid JSON format", ex);
                return new JsonSortingException($"Failed to sort JSON: {msg}", ex);
            },
            "Failed to sort JSON properties") ?? string.Empty;
    }
    
    /// <summary>
    /// Sorts JSON object properties using a custom comparer for property names.
    /// This allows for specialized sorting such as case-insensitive comparison or custom ordering rules.
    /// 
    /// <example>
    /// <code>
    /// // Unsorted JSON
    /// string json = @"{""Z"":3,""a"":1,""B"":2}";
    /// 
    /// // Sort case-insensitively
    /// string sorted = JsonSorter.SortJsonWithComparer(json, StringComparer.OrdinalIgnoreCase);
    /// 
    /// // Result: {"a":1,"B":2,"Z":3}  (sorted alphabetically regardless of case)
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The input JSON string.</param>
    /// <param name="comparer">The string comparer to use for property name comparison.</param>
    /// <param name="indented">Whether to format the output with indentation (default is false).</param>
    /// <returns>A normalized JSON string with custom-sorted properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonSortingException">Thrown when an error occurs during the sorting process.</exception>
    public static string SortJsonWithComparer(string json, IComparer<string> comparer, bool indented = false)
    {
        using var performance = new PerformanceTracker(Logger, nameof(SortJsonWithComparer));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(comparer, nameof(comparer));
        
        return ExceptionHelpers.SafeExecute(() =>
            {
                Logger.LogDebug("Sorting JSON properties with custom comparer in {Length} character string", json.Length);
            
                using var document = JsonDocument.Parse(json);
                var sortedObject = NormalizeValueWithComparer(document.RootElement, comparer);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = indented,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
            
                string result = JsonSerializer.Serialize(sortedObject, options);
                Logger.LogDebug("Successfully sorted JSON with custom comparer from {OriginalLength} to {ResultLength} characters", 
                    json.Length, result.Length);
                
                return result;
            },
            (ex, msg) => {
                if (ex is JsonException)
                    return new JsonParsingException("Failed to sort JSON with custom comparer: Invalid JSON format", ex);
                return new JsonSortingException($"Failed to sort JSON with custom comparer: {msg}", ex);
            },
            "Failed to sort JSON properties with custom comparer") ?? string.Empty;
    }
    
    /// <summary>
    /// Sorts JSON array elements and object properties recursively to create a fully normalized representation.
    /// This is useful for creating truly canonical forms where both property order and array element order are deterministic.
    /// 
    /// <example>
    /// <code>
    /// // JSON with unsorted arrays and properties
    /// string json = @"{
    ///   ""people"": [
    ///     {""name"": ""Zack"", ""id"": 3},
    ///     {""name"": ""Alice"", ""id"": 1},
    ///     {""name"": ""Bob"", ""id"": 2}
    ///   ],
    ///   ""version"": 1,
    ///   ""created"": ""2023-01-01""
    /// }";
    /// 
    /// // Sort everything using a field selector for array elements
    /// string sorted = JsonSorter.SortJsonDeep(json, "name");
    /// 
    /// // Result will have properties sorted AND people array sorted by name:
    /// // {"created":"2023-01-01","people":[{"id":1,"name":"Alice"},{"id":2,"name":"Bob"},{"id":3,"name":"Zack"}],"version":1}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The input JSON string.</param>
    /// <param name="arrayElementSortProperty">The property name to use for sorting array elements (if they are objects).</param>
    /// <param name="indented">Whether to format the output with indentation (default is false).</param>
    /// <returns>A deeply normalized JSON string with sorted properties and arrays.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the json parameter is null.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonSortingException">Thrown when an error occurs during the sorting process.</exception>
    public static string SortJsonDeep(string json, string arrayElementSortProperty, bool indented = false)
    {
        using var performance = new PerformanceTracker(Logger, nameof(SortJsonDeep));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(arrayElementSortProperty, nameof(arrayElementSortProperty));
        
        return ExceptionHelpers.SafeExecute(() =>
            {
                Logger.LogDebug("Deep sorting JSON properties and arrays in {Length} character string", json.Length);
            
                using var document = JsonDocument.Parse(json);
                var sortedObject = NormalizeValueDeep(document.RootElement, arrayElementSortProperty);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = indented,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
            
                string result = JsonSerializer.Serialize(sortedObject, options);
                Logger.LogDebug("Successfully deep-sorted JSON from {OriginalLength} to {ResultLength} characters", 
                    json.Length, result.Length);
                
                return result;
            },
            (ex, msg) => {
                if (ex is JsonException)
                    return new JsonParsingException("Failed to deep-sort JSON: Invalid JSON format", ex);
                return new JsonSortingException($"Failed to deep-sort JSON: {msg}", ex);
            },
            "Failed to deep-sort JSON properties and arrays") ?? string.Empty;
    }
    
    /// <summary>
    /// Sorts only the top-level properties of a JSON object, leaving nested structures unchanged.
    /// This is useful when you only need to standardize the order of root properties.
    /// 
    /// <example>
    /// <code>
    /// // Unsorted JSON
    /// string json = @"{
    ///   ""z"": [3, 2, 1],
    ///   ""a"": {""c"": 3, ""b"": 2, ""a"": 1},
    ///   ""m"": 13
    /// }";
    /// 
    /// // Sort only top-level properties
    /// string sorted = JsonSorter.SortJsonShallow(json);
    /// 
    /// // Result:
    /// // {"a":{"c":3,"b":2,"a":1},"m":13,"z":[3,2,1]}
    /// // Note that nested objects and arrays remain unsorted
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The input JSON string.</param>
    /// <param name="indented">Whether to format the output with indentation (default is false).</param>
    /// <returns>A JSON string with sorted top-level properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the json parameter is null.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonSortingException">Thrown when an error occurs during the sorting process.</exception>
    public static string SortJsonShallow(string json, bool indented = false)
    {
        using var performance = new PerformanceTracker(Logger, nameof(SortJsonShallow));
        
        // Validate input
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        
        return ExceptionHelpers.SafeExecute(() =>
            {
                Logger.LogDebug("Shallow sorting top-level JSON properties in {Length} character string", json.Length);
            
                using var document = JsonDocument.Parse(json);
            
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    // If root is not an object, just return the original JSON
                    Logger.LogDebug("JSON root is not an object, returning original");
                    return json;
                }
            
                // Create a sorted dictionary for top-level properties
                var sortedDict = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
            
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    sortedDict[property.Name] = property.Value.Clone();
                }
            
                var options = new JsonSerializerOptions
                {
                    WriteIndented = indented,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
            
                string result = JsonSerializer.Serialize(sortedDict, options);
                Logger.LogDebug("Successfully shallow-sorted JSON from {OriginalLength} to {ResultLength} characters", 
                    json.Length, result.Length);
                
                return result;
            },
            (ex, msg) => {
                if (ex is JsonException)
                    return new JsonParsingException("Failed to shallow-sort JSON: Invalid JSON format", ex);
                return new JsonSortingException($"Failed to shallow-sort JSON: {msg}", ex);
            },
            "Failed to shallow-sort JSON properties") ?? string.Empty;
    }
    
    /// <summary>
    /// Attempts to sort a JSON string's properties without throwing exceptions.
    /// </summary>
    /// <param name="json">The input JSON string.</param>
    /// <param name="result">When successful, contains the sorted JSON; otherwise, returns the original JSON or empty string.</param>
    /// <param name="indented">Whether to format the output with indentation.</param>
    /// <returns>True if sorting was successful; otherwise, false.</returns>
    public static bool TrySortJson(string json, out string result, bool indented = false)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            result = string.Empty;
            return false;
        }
        
        try
        {
            result = SortJson(json, indented);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error sorting JSON");
            result = json;  // Return original on error
            return false;
        }
    }
    
    /// <summary>
    /// Attempts to deeply sort a JSON string's properties and arrays without throwing exceptions.
    /// </summary>
    /// <param name="json">The input JSON string.</param>
    /// <param name="arrayElementSortProperty">The property name to use for sorting array elements.</param>
    /// <param name="result">When successful, contains the sorted JSON; otherwise, returns the original JSON or empty string.</param>
    /// <param name="indented">Whether to format the output with indentation.</param>
    /// <returns>True if sorting was successful; otherwise, false.</returns>
    public static bool TrySortJsonDeep(string json, string arrayElementSortProperty, out string result, bool indented = false)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(arrayElementSortProperty))
        {
            result = string.Empty;
            return false;
        }
        
        try
        {
            result = SortJsonDeep(json, arrayElementSortProperty, indented);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error deep-sorting JSON");
            result = json;  // Return original on error
            return false;
        }
    }

    /// <summary>
    /// Recursively normalizes a JsonElement to a .NET object with sorted properties.
    /// </summary>
    /// <param name="element">The JsonElement to normalize.</param>
    /// <returns>A normalized object with sorted properties.</returns>
    private static object? NormalizeValue(JsonElement element)
    {
        return ExceptionHelpers.SafeExecuteWithDefault<object?>(
            () =>
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.Object:
                        // SortedDictionary ensures keys are in order
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
                        // Preserve the number by converting using the raw text
                        return element.GetRawText();
                        
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return element.GetBoolean();
                        
                    case JsonValueKind.Null:
                        return null;
                        
                    default:
                        return element.ToString();
                }
            },
            null,
            $"Error normalizing JSON element of type {element.ValueKind}");
    }
    
    /// <summary>
    /// Recursively normalizes a JsonElement to a .NET object with properties sorted using a custom comparer.
    /// </summary>
    /// <param name="element">The JsonElement to normalize.</param>
    /// <param name="comparer">The string comparer to use for property name comparison.</param>
    /// <returns>A normalized object with custom-sorted properties.</returns>
    private static object? NormalizeValueWithComparer(JsonElement element, IComparer<string> comparer)
    {
        return ExceptionHelpers.SafeExecuteWithDefault<object?>(
            () =>
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.Object:
                        // Use SortedDictionary with the provided comparer
                        var sortedDict = new SortedDictionary<string, object?>(comparer);
                        foreach (var property in element.EnumerateObject())
                        {
                            sortedDict[property.Name] = NormalizeValueWithComparer(property.Value, comparer);
                        }
                        return sortedDict;
                        
                    case JsonValueKind.Array:
                        var list = new List<object?>();
                        foreach (var item in element.EnumerateArray())
                        {
                            list.Add(NormalizeValueWithComparer(item, comparer));
                        }
                        return list;
                        
                    case JsonValueKind.String:
                        return element.GetString();
                        
                    case JsonValueKind.Number:
                        return element.GetRawText();
                        
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return element.GetBoolean();
                        
                    case JsonValueKind.Null:
                        return null;
                        
                    default:
                        return element.ToString();
                }
            },
            null,
            "Error normalizing JSON element with custom comparer");
    }
    
    /// <summary>
    /// Recursively normalizes a JsonElement to a .NET object with sorted properties and sorted arrays.
    /// </summary>
    /// <param name="element">The JsonElement to normalize.</param>
    /// <param name="arraySortProperty">The property name to use for sorting array elements.</param>
    /// <returns>A deeply normalized object with sorted properties and arrays.</returns>
    private static object? NormalizeValueDeep(JsonElement element, string arraySortProperty)
    {
        return ExceptionHelpers.SafeExecuteWithDefault<object?>(
            () =>
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.Object:
                        var sortedDict = new SortedDictionary<string, object?>(StringComparer.Ordinal);
                        foreach (var property in element.EnumerateObject())
                        {
                            sortedDict[property.Name] = NormalizeValueDeep(property.Value, arraySortProperty);
                        }
                        return sortedDict;
                        
                    case JsonValueKind.Array:
                        var list = new List<object?>();
                        
                        // First normalize each array element
                        foreach (var item in element.EnumerateArray())
                        {
                            list.Add(NormalizeValueDeep(item, arraySortProperty));
                        }
                        
                        // If array elements are objects, try to sort them by the specified property
                        if (list.Count > 0 && list[0] is SortedDictionary<string, object?> && !string.IsNullOrEmpty(arraySortProperty))
                        {
                            // Sort the list by the specified property
                            list.Sort((a, b) => 
                            {
                                if (a is not SortedDictionary<string, object?> dictA || b is not SortedDictionary<string, object?> dictB)
                                    return 0;
                                    
                                // Try to get the sort property from both dictionaries
                                if (!dictA.TryGetValue(arraySortProperty, out var valueA) || 
                                    !dictB.TryGetValue(arraySortProperty, out var valueB))
                                    return 0;
                                    
                                // Compare values
                                return Comparer<object?>.Default.Compare(valueA, valueB);
                            });
                        }
                        
                        return list;
                        
                    case JsonValueKind.String:
                        return element.GetString();
                        
                    case JsonValueKind.Number:
                        return element.GetRawText();
                        
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return element.GetBoolean();
                        
                    case JsonValueKind.Null:
                        return null;
                        
                    default:
                        return element.ToString();
                }
            },
            null,
            "Error deep normalizing JSON element");
    }
}