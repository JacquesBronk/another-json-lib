using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility;

/// <summary>
/// Provides functionality for working with JSON Pointers as defined in RFC6901.
/// 
/// JSON Pointer is a string syntax for identifying a specific value within a JSON document.
/// It uses a path-like notation with '/' as separator and special encoding for '/' and '~' characters.
/// 
/// Use this class when you need to:
/// - Reference a specific location in a JSON document
/// - Extract a deeply nested value without parsing the entire document
/// - Create APIs that support JSON Pointer for resource addressing
/// 
/// <example>
/// <code>
/// // Example JSON document
/// string json = @"{
///     ""person"": {
///         ""name"": ""John Doe"",
///         ""email"": ""john@example.com"",
///         ""addresses"": [
///             {
///                 ""type"": ""home"",
///                 ""street"": ""123 Main St""
///             },
///             {
///                 ""type"": ""work"",
///                 ""street"": ""456 Market St""
///             }
///         ]
///     }
/// }";
/// 
/// using var doc = JsonDocument.Parse(json);
/// 
/// // Get a simple property
/// var name = doc.EvaluatePointer("/person/name");  // "John Doe"
/// 
/// // Get an array element
/// var workAddress = doc.EvaluatePointer("/person/addresses/1");  // The work address object
/// 
/// // Get a property from an array element
/// var workStreet = doc.EvaluatePointer("/person/addresses/1/street");  // "456 Market St"
/// </code>
/// </example>
/// </summary>
public static class JsonPointer
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonPointer));
    
    /// <summary>
    /// Evaluates a JSON Pointer against a JsonDocument and returns the referenced JsonElement.
    /// 
    /// This method follows the RFC6901 specification for JSON Pointer resolution:
    /// - The empty string ("") references the entire document root
    /// - Path segments are separated by "/"
    /// - Special characters are escaped: "~0" for "~" and "~1" for "/"
    /// - Numeric indices are used for array elements
    /// 
    /// <example>
    /// <code>
    /// string json = @"{""users"":[{""name"":""Alice"",""id"":1},{""name"":""Bob"",""id"":2}]}";
    /// using var doc = JsonDocument.Parse(json);
    /// 
    /// // Reference the entire document
    /// var root = doc.EvaluatePointer("");
    /// 
    /// // Get a property
    /// var users = doc.EvaluatePointer("/users");
    /// 
    /// // Get an array element
    /// var bob = doc.EvaluatePointer("/users/1");
    /// 
    /// // Get a property from an array element
    /// var bobName = doc.EvaluatePointer("/users/1/name");  // "Bob"
    /// 
    /// // Handle special characters
    /// var json2 = @"{""a/b"": {""c~d"": 42}}";
    /// using var doc2 = JsonDocument.Parse(json2);
    /// var value = doc2.EvaluatePointer("/a~1b/c~0d");  // 42
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="document">The JsonDocument to evaluate the pointer against.</param>
    /// <param name="pointer">A RFC6901 JSON Pointer string.</param>
    /// <returns>The JsonElement referenced by the pointer, or null if the pointer does not resolve to any value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the document or pointer is null.</exception>
    /// <exception cref="JsonPointerException">Thrown if a non-empty pointer does not start with "/" or has invalid syntax.</exception>
    public static JsonElement? EvaluatePointer(this JsonDocument document, string pointer)
    {
        using var performance = new PerformanceTracker(Logger, nameof(EvaluatePointer));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNull(document, nameof(document));
        ExceptionHelpers.ThrowIfNull(pointer, nameof(pointer));
        
        return ExceptionHelpers.SafeExecuteWithDefault<JsonElement?>(
            () => {
                Logger.LogTrace("Evaluating JSON pointer: '{Pointer}'", pointer);
                
                // The empty string references the entire document.
                if (pointer == "")
                    return document.RootElement;

                if (!pointer.StartsWith("/"))
                    throw new JsonPointerException($"Invalid JSON Pointer format: '{pointer}'. A non-empty JSON Pointer must start with '/'");

                // Split the pointer into tokens.
                // The first token is always empty because the pointer starts with '/'
                var tokens = pointer.Split('/');

                JsonElement current = document.RootElement;
                // Process tokens from index 1 onward.
                for (int i = 1; i < tokens.Length; i++)
                {
                    // Decode per RFC6901: "~1" becomes "/" and "~0" becomes "~"
                    string token = tokens[i].Replace("~1", "/").Replace("~0", "~");
                    Logger.LogTrace("Processing token: '{Token}' at position {Position}", token, i);

                    if (current.ValueKind == JsonValueKind.Object)
                    {
                        if (current.TryGetProperty(token, out JsonElement property))
                            current = property;
                        else
                        {
                            Logger.LogDebug("Property '{Token}' not found in object at position {Position}", token, i);
                            return null; // Property not found.
                        }
                    }
                    else if (current.ValueKind == JsonValueKind.Array)
                    {
                        if (int.TryParse(token, out int index))
                        {
                            if (index >= 0 && index < current.GetArrayLength())
                                current = current[index];
                            else
                            {
                                Logger.LogDebug("Array index {Index} out of bounds at position {Position}. Array length: {Length}", 
                                    index, i, current.GetArrayLength());
                                return null; // Array index out of bounds.
                            }
                        }
                        else
                        {
                            Logger.LogDebug("Invalid array index '{Token}' at position {Position}", token, i);
                            return null; // Invalid array index.
                        }
                    }
                    else
                    {
                        // Cannot traverse further if the current element is a primitive.
                        Logger.LogDebug("Cannot traverse beyond primitive value of type {ValueKind} at position {Position}", 
                            current.ValueKind, i);
                        return null;
                    }
                }

                Logger.LogTrace("Successfully resolved JSON pointer to element of type {ValueKind}", current.ValueKind);
                return current;
            },
            null,
            $"Failed to evaluate JSON pointer '{pointer}'"
        );
    }
    
    /// <summary>
    /// Attempts to evaluate a JSON Pointer against a JsonDocument.
    /// Returns false if the pointer cannot be resolved instead of throwing an exception.
    /// 
    /// <example>
    /// <code>
    /// string json = @"{""users"":[{""name"":""Alice"",""id"":1},{""name"":""Bob"",""id"":2}]}";
    /// using var doc = JsonDocument.Parse(json);
    /// 
    /// // Try to get a valid element
    /// if (doc.TryEvaluatePointer("/users/1/name", out var name))
    /// {
    ///     // Use name.Value.GetString() which will be "Bob"
    /// }
    /// 
    /// // Try to get a non-existent element
    /// if (!doc.TryEvaluatePointer("/users/5", out var nonExistent))
    /// {
    ///     // Handle the case where the pointer doesn't resolve
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="document">The JsonDocument to evaluate the pointer against.</param>
    /// <param name="pointer">A RFC6901 JSON Pointer string.</param>
    /// <param name="result">When this method returns, contains the referenced JsonElement if successful; otherwise, null.</param>
    /// <returns>True if the pointer was successfully evaluated; otherwise, false.</returns>
    public static bool TryEvaluatePointer(this JsonDocument? document, string pointer, out JsonElement? result)
    {
        if (document == null || string.IsNullOrWhiteSpace(pointer))
        {
            result = null;
            return false;
        }
        
        try
        {
            result = EvaluatePointer(document, pointer);
            return result.HasValue;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error evaluating JSON pointer '{Pointer}'", pointer);
            result = null;
            return false;
        }
    }
    
    /// <summary>
    /// Creates a JSON Pointer string from path segments, properly escaping special characters.
    /// 
    /// <example>
    /// <code>
    /// // Create a pointer to a nested property with special characters
    /// string pointer = JsonPointer.Create("users", "0", "user/name");
    /// // Result: "/users/0/user~1name"
    /// 
    /// // Use the created pointer
    /// var element = document.EvaluatePointer(pointer);
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="segments">The path segments that make up the JSON Pointer.</param>
    /// <returns>A RFC6901-compliant JSON Pointer string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the segments array is null.</exception>
    public static string Create(params string[] segments)
    {
        ExceptionHelpers.ThrowIfNull(segments, nameof(segments));
        
        using var performance = new PerformanceTracker(Logger, nameof(Create));
        
        return ExceptionHelpers.SafeExecute(() => 
            {
                var escapedSegments = segments.Select(segment => 
                {
                    ExceptionHelpers.ThrowIfNull(segment, "pointer segment");
                    // Escape special characters according to RFC6901
                    return segment.Replace("~", "~0").Replace("/", "~1");
                });
            
                return "/" + string.Join("/", escapedSegments);
            },
            (ex, msg) => new JsonPointerException($"Failed to create JSON pointer: {msg}", ex),
            "Failed to create JSON pointer") ?? string.Empty;
    }
    
    /// <summary>
    /// Appends a segment to an existing JSON Pointer string, properly escaping special characters.
    /// 
    /// <example>
    /// <code>
    /// string basePointer = "/users/0";
    /// string newPointer = JsonPointer.Append(basePointer, "user/name");
    /// // Result: "/users/0/user~1name"
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="pointer">The base JSON Pointer to append to.</param>
    /// <param name="segment">The segment to append.</param>
    /// <returns>A new JSON Pointer with the segment appended.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the pointer or segment is null.</exception>
    public static string Append(string pointer, string segment)
    {
        ExceptionHelpers.ThrowIfNull(pointer, nameof(pointer));
        ExceptionHelpers.ThrowIfNull(segment, nameof(segment));
        
        using var performance = new PerformanceTracker(Logger, nameof(Append));
        
        return ExceptionHelpers.SafeExecute(() => 
            {
                // Escape the segment according to RFC6901
                string escapedSegment = segment.Replace("~", "~0").Replace("/", "~1");
            
                // Handle the case when pointer is empty or just "/"
                if (string.IsNullOrEmpty(pointer) || pointer == "/")
                    return "/" + escapedSegment;
                
                // Ensure the pointer starts with a "/"
                if (!pointer.StartsWith("/"))
                    pointer = "/" + pointer;
                
                // Add the segment
                return pointer + "/" + escapedSegment;
            },
            (ex, msg) => new JsonPointerException($"Failed to append to JSON pointer: {msg}", ex),
            "Failed to append to JSON pointer") ?? string.Empty;
    }
}