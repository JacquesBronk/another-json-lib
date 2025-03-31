using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Helper;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Operations;

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
            (ex, msg) =>
            {
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

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return json.FromJson<JsonElement>().ToJson(new JsonSerializerOptions
                    {
                        WriteIndented = indented,
                        NumberHandling = JsonNumberHandling.Strict
                    });
                }

                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented });

                writer.WriteStartObject();

                // Get all properties and sort them using the provided comparer
                var sortedProperties = document.RootElement.EnumerateObject()
                    .OrderBy(p => p.Name, comparer)
                    .ToList();

                // Write sorted properties
                foreach (var property in sortedProperties)
                {
                    writer.WritePropertyName(property.Name);
                    WriteJsonElement(property.Value, writer);
                }

                writer.WriteEndObject();
                writer.Flush();

                var result = Encoding.UTF8.GetString(stream.ToArray());

                Logger.LogDebug("Successfully sorted JSON with custom comparer from {OriginalLength} to {ResultLength} characters",
                    json.Length, result.Length);

                return result;
            },
            (ex, msg) =>
            {
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
    public static string SortJsonDeep(string json, string? arrayElementSortProperty = null, bool indented = false)
    {
        using var performance = new PerformanceTracker(Logger, nameof(SortJsonDeep));

        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(arrayElementSortProperty, nameof(arrayElementSortProperty));

        return ExceptionHelpers.SafeExecute(() =>
            {
                Logger.LogDebug("Deep sorting JSON properties and arrays in {Length} character string", json.Length);

                using var document = JsonDocument.Parse(json);

                // Create a new JSON writer
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);

                // Process the root element (recursively)
                ProcessElement(document.RootElement, writer, arrayElementSortProperty);

                writer.Flush();

                var result = Encoding.UTF8.GetString(stream.ToArray());
                Logger.LogDebug("Successfully deep-sorted JSON from {OriginalLength} to {ResultLength} characters",
                    json.Length, result.Length);
                return result;
            },
            (ex, msg) =>
            {
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
                    // Option 1: Use a compact serialization
                    var compactOptions = new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        NumberHandling = JsonNumberHandling.Strict
                    };

                    return json.FromJson<JsonElement>().ToJson(compactOptions);
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
            (ex, msg) =>
            {
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
            result = json; // Return original on error
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
            result = json; // Return original on error
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
                        // Return an actual numeric value instead of a string
                        if (element.TryGetInt32(out int intValue))
                            return intValue;
                        else if (element.TryGetInt64(out long longValue))
                            return longValue;
                        else if (element.TryGetDouble(out double doubleValue))
                            return doubleValue;
                        else
                            return element.GetDecimal();

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
    /// Processes a JsonElement and writes it to a Utf8JsonWriter, sorting properties and arrays as needed.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="writer"></param>
    /// <param name="arraySortProperty"></param>
    private static void ProcessElement(JsonElement element, Utf8JsonWriter writer, string? arraySortProperty)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();

                // Sort properties by name
                var properties = element.EnumerateObject()
                    .OrderBy(p => p.Name)
                    .ToList();

                foreach (var property in properties)
                {
                    writer.WritePropertyName(property.Name);
                    ProcessElement(property.Value, writer, arraySortProperty);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();

                var items = element.EnumerateArray().ToList();

                // Sort array elements if they are objects and arraySortProperty is specified
                if (arraySortProperty != null && items.Count > 0 && items[0].ValueKind == JsonValueKind.Object)
                {
                    // Try to sort by the specified property
                    items = items.OrderBy(item =>
                    {
                        if (item.TryGetProperty(arraySortProperty, out var prop))
                            return prop.GetRawText();
                        return string.Empty;
                    }).ToList();
                }

                foreach (var item in items)
                {
                    ProcessElement(item, writer, arraySortProperty);
                }

                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                // Preserve the numeric type
                if (element.TryGetInt32(out int intValue))
                    writer.WriteNumberValue(intValue);
                else if (element.TryGetInt64(out long longValue))
                    writer.WriteNumberValue(longValue);
                else if (element.TryGetDouble(out double doubleValue))
                    writer.WriteNumberValue(doubleValue);
                else
                    writer.WriteRawValue(element.GetRawText());
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                writer.WriteRawValue(element.GetRawText());
                break;
        }
    }

    /// <summary>
    /// Writes a JsonElement to a Utf8JsonWriter, preserving its structure and types.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="writer"></param>
    private static void WriteJsonElement(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteJsonElement(property.Value, writer);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteJsonElement(item, writer);
                }

                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                // Preserve numeric types
                if (element.TryGetInt32(out int intValue))
                    writer.WriteNumberValue(intValue);
                else if (element.TryGetInt64(out long longValue))
                    writer.WriteNumberValue(longValue);
                else if (element.TryGetDouble(out double doubleValue))
                    writer.WriteNumberValue(doubleValue);
                else
                    writer.WriteRawValue(element.GetRawText());
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
        }
    }
}