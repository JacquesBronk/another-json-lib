using System.Text.Encodings.Web;
using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Helper;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Formatting;

/// <summary>
/// Provides functionality to canonicalize JSON strings to ensure consistent formatting and ordering.
/// 
/// JSON canonicalization creates a standardized representation of JSON data where:
/// - Object properties are sorted lexicographically
/// - Whitespace is normalized
/// - Numeric values are consistently formatted
/// - String escaping follows consistent rules
/// 
/// This is particularly useful for:
/// - Digital signatures and verification where byte-exact representation matters
/// - Hash generation for JSON data
/// - Equality comparisons of JSON data with different formatting
/// 
/// <example>
/// <code>
/// // Original JSON with arbitrary formatting and property order
/// string originalJson = @"{
///   ""c"": ""value"",
///   ""a"": 123,
///   ""b"": [3, 2, 1]
/// }";
/// 
/// // Convert to canonical form
/// string canonicalJson = JsonCanonicalizer.Canonicalize(originalJson);
/// 
/// // Result: {"a":123,"b":[3,2,1],"c":"value"}
/// </code>
/// </example>
/// </summary>
public static class JsonCanonicalizer
{
    // Logger for this class
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonCanonicalizer));

    /// <summary>
    /// Returns a canonical JSON string for the input JSON.
    /// Object properties are sorted lexicographically to ensure that semantically equivalent JSON
    /// produce the same output.
    /// 
    /// The canonicalization process:
    /// 1. Parses the input JSON into a document object model
    /// 2. Normalizes the structure (sorting object properties, standardizing numeric formats)
    /// 3. Serializes back to a string with consistent formatting rules
    /// 
    /// <example>
    /// <code>
    /// // Two semantically equivalent but differently formatted JSON strings
    /// string json1 = @"{ ""name"": ""John"", ""age"": 30 }";
    /// string json2 = @"{""age"":30,""name"":""John""}";
    /// 
    /// // Both will produce identical canonical output
    /// string canonical1 = JsonCanonicalizer.Canonicalize(json1);
    /// string canonical2 = JsonCanonicalizer.Canonicalize(json2);
    /// 
    /// // Result: {"age":30,"name":"John"}
    /// Console.WriteLine(canonical1 == canonical2); // True
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The input JSON string.</param>
    /// <returns>The canonicalized JSON string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the input JSON string is null.</exception>
    /// <exception cref="JsonCanonicalizationException">Thrown when JSON parsing or serialization fails.</exception>
    public static string Canonicalize(string json)
    {
        ExceptionHelpers.ThrowIfNull(json, nameof(json));
        
        Logger.LogTrace("Canonicalizing JSON: {JsonPrefix}", 
                         json.Length > 100 ? json.Substring(0, 100) + "..." : json);
        using var performance = new PerformanceTracker(Logger, nameof(Canonicalize));
        return ExceptionHelpers.SafeExecute(
            () => {
                using var document = JsonDocument.Parse(json);
                object? normalized = NormalizeElement(document.RootElement);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    // Use relaxed escaping for better readability.
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                string result = JsonSerializer.Serialize(normalized, options);
                Logger.LogTrace("Canonicalization successful. Result: {ResultPrefix}", 
                    result.Length > 100 ? result.Substring(0, 100) + "..." : result);
                
                return result;
            },
            (ex, msg) => {
                if (ex is JsonException jsonEx)
                {
                    return new JsonCanonicalizationException($"Invalid JSON format: {jsonEx.Message}", ex);
                }
                return new JsonCanonicalizationException($"Failed to canonicalize JSON: {msg}", ex);
            },
            "Error during JSON canonicalization"
        ) ?? string.Empty;
    }

    /// <summary>
/// Recursively normalizes a JsonElement:
/// - Objects are converted to a SortedDictionary (keys sorted lexicographically).
/// - Arrays are normalized element-by-element.
/// - Primitives are returned as-is, with numbers parsed to decimal if possible.
/// </summary>
/// <param name="element">The JsonElement to normalize.</param>
/// <returns>A normalized .NET object.</returns>
/// <exception cref="JsonCanonicalizationException">Thrown when normalization of an element fails.</exception>
private static object? NormalizeElement(JsonElement element)
{
    return ExceptionHelpers.SafeExecute<object>(
        () => JsonElementUtils.Normalize(element),
        (ex, msg) => new JsonCanonicalizationException(
            $"Failed to normalize JSON element of type {element.ValueKind}: {msg}", ex),
        $"Error normalizing JSON element of type {element.ValueKind}"
    ) ?? null;
}
    
    /// <summary>
    /// Canonicalizes a JsonDocument directly.
    /// Useful when you already have a parsed JsonDocument and want to avoid reparsing.
    /// </summary>
    /// <param name="document">The JsonDocument to canonicalize.</param>
    /// <returns>The canonicalized JSON string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the document is null.</exception>
    /// <exception cref="JsonCanonicalizationException">Thrown when document processing or serialization fails.</exception>
    public static string Canonicalize(JsonDocument document)
    {
        ExceptionHelpers.ThrowIfNull(document, nameof(document));
        
        return ExceptionHelpers.SafeExecute(
            () => {
                object? normalized = NormalizeElement(document.RootElement);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                return JsonSerializer.Serialize(normalized, options);
            },
            (ex, msg) => new JsonCanonicalizationException($"Failed to canonicalize JSON document: {msg}", ex),
            "Error during JSON document canonicalization"
        ) ?? string.Empty;
    }
    
    /// <summary>
    /// Determines if two JSON strings are canonically equivalent.
    /// Two JSON strings are canonically equivalent if their canonical representations are identical.
    /// </summary>
    /// <param name="json1">The first JSON string.</param>
    /// <param name="json2">The second JSON string.</param>
    /// <returns>True if the JSON strings are canonically equivalent; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either JSON string is null.</exception>
    /// <exception cref="JsonCanonicalizationException">Thrown when canonicalization fails.</exception>
    public static bool AreEquivalent(string json1, string json2)
    {
        ExceptionHelpers.ThrowIfNull(json1, nameof(json1));
        ExceptionHelpers.ThrowIfNull(json2, nameof(json2));
        
        return ExceptionHelpers.SafeExecute(
            () => {
                string canonical1 = Canonicalize(json1);
                string canonical2 = Canonicalize(json2);
                return canonical1 == canonical2;
            },
            (ex, msg) => new JsonCanonicalizationException($"Failed to compare JSON equivalence: {msg}", ex),
            "Error comparing JSON canonical equivalence"
        );
    }
}