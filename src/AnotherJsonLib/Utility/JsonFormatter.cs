using System.Text.Encodings.Web;
using System.Text.Json;
using AnotherJsonLib.Exceptions;
using Microsoft.Extensions.Logging;
using AnotherJsonLib.Infra;

namespace AnotherJsonLib.Utility;

/// <summary>
/// Provides methods to format JSON strings, including minification and prettification.
/// 
/// JSON formatting optimizes JSON data for either human readability (prettification) or
/// size efficiency (minification). This utility helps with:
/// 
/// - Reducing JSON file sizes by removing unnecessary whitespace (minification)
/// - Making JSON data more readable for debugging and documentation (prettification)
/// - Ensuring consistent formatting across different JSON documents
/// - Converting between different formatting styles without altering the data
/// - Controlling indentation and spacing for presentation
/// 
/// <example>
/// <code>
/// // Example: Format JSON for better readability
/// string compactJson = @"{""person"":{""name"":""John Smith"",""age"":30,""address"":{""street"":""123 Main St"",""city"":""New York"",""zip"":""10001""},""phones"":[""212-555-1234"",""646-555-5678""]}}";
/// 
/// // Make it pretty for human reading
/// string prettyJson = compactJson.PrettifyJson();
/// 
/// // Result:
/// // {
/// //   "person": {
/// //     "name": "John Smith",
/// //     "age": 30,
/// //     "address": {
/// //       "street": "123 Main St",
/// //       "city": "New York",
/// //       "zip": "10001"
/// //     },
/// //     "phones": [
/// //       "212-555-1234",
/// //       "646-555-5678"
/// //     ]
/// //   }
/// // }
/// 
/// // Later, minify it for storage or transmission
/// string minifiedJson = prettyJson.Minify();
/// // Result: {"person":{"name":"John Smith","age":30,"address":{"street":"123 Main St","city":"New York","zip":"10001"},"phones":["212-555-1234","646-555-5678"]}}
/// </code>
/// </example>
/// </summary>
public static class JsonFormatter
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonFormatter));

    /// <summary>
    /// Options for minifying JSON strings.
    /// </summary>
    private static readonly JsonSerializerOptions MinifyOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Options for prettifying JSON strings.
    /// </summary>
    private static readonly JsonSerializerOptions PrettifyOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    
    /// <summary>
    /// Default indentation size for custom indentation.
    /// </summary>
    private const int DefaultIndentSize = 2;
    
    /// <summary>
    /// Maximum indentation size allowed.
    /// </summary>
    private const int MaxIndentSize = 8;

    /// <summary>
    /// Minifies the JSON string by removing unnecessary whitespace.
    /// This reduces the size of the JSON string without changing its meaning.
    /// 
    /// <para>
    /// Minification removes:
    /// - Indentation
    /// - Line breaks
    /// - Excess spaces
    /// - Optional whitespace around punctuation
    /// </para>
    /// 
    /// <example>
    /// <code>
    /// // Indented JSON with excess whitespace
    /// string prettyJson = @"{
    ///   ""name"": ""John"",
    ///   ""age"": 30,
    ///   ""isActive"": true
    /// }";
    /// 
    /// // Minify to compact form
    /// string minifiedJson = prettyJson.Minify();
    /// 
    /// // Result: {"name":"John","age":30,"isActive":true}
    /// 
    /// // Reduced size = (minifiedJson.Length / prettyJson.Length) * 100
    /// double percentReduction = (1 - ((double)minifiedJson.Length / prettyJson.Length)) * 100;
    /// Console.WriteLine($"Size reduced by {percentReduction:0.0}%");
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to minify.</param>
    /// <returns>A minified JSON string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the input string is null or empty.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonFormattingException">Thrown when minification fails.</exception>
    public static string Minify(this string json)
    {
        using var performance = new PerformanceTracker(Logger, nameof(Minify));
        
        // Validate input
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            int originalLength = json.Length;
            Logger.LogDebug("Minifying JSON string of length {Length}", originalLength);
            
            using var doc = JsonDocument.Parse(json);
            string result = JsonSerializer.Serialize(doc.RootElement, MinifyOptions);
            
            int newLength = result.Length;
            float compressionRatio = originalLength > 0 ? (float)newLength / originalLength : 1;
            
            Logger.LogDebug("Successfully minified JSON from {OriginalLength} to {MinifiedLength} characters " +
                           "(compression ratio: {CompressionRatio:P2})",
                originalLength, newLength, compressionRatio);
                
            return result;
        }, 
        (ex, msg) => {
            if (ex is JsonException jsonEx)
                return new JsonParsingException("Failed to minify JSON: Invalid JSON format", jsonEx);
                
            return new JsonFormattingException($"Failed to minify JSON: {msg}", ex);
        }, 
        "Failed to minify JSON") ?? json;
    }

    /// <summary>
    /// Prettifies an object into a human-readable, indented JSON string.
    /// 
    /// <para>
    /// Prettification adds:
    /// - Consistent indentation
    /// - Line breaks for better readability
    /// - Appropriate spacing around punctuation
    /// </para>
    /// 
    /// <example>
    /// <code>
    /// // Create a simple object
    /// var person = new 
    /// {
    ///     Name = "Jane Smith",
    ///     Age = 28,
    ///     Contact = new { Email = "jane@example.com", Phone = "555-1234" },
    ///     Skills = new[] { "coding", "design", "communication" }
    /// };
    /// 
    /// // Prettify to make it readable
    /// string prettyJson = person.Prettify();
    /// 
    /// // Result:
    /// // {
    /// //   "Name": "Jane Smith",
    /// //   "Age": 28,
    /// //   "Contact": {
    /// //     "Email": "jane@example.com",
    /// //     "Phone": "555-1234"
    /// //   },
    /// //   "Skills": [
    /// //     "coding",
    /// //     "design",
    /// //     "communication"
    /// //   ]
    /// // }
    /// </code>
    /// </example>
    /// </summary>
    /// <typeparam name="T">The type of object to prettify.</typeparam>
    /// <param name="data">The object to prettify.</param>
    /// <returns>A prettified JSON string with proper indentation.</returns>
    /// <exception cref="JsonFormattingException">Thrown when prettification fails.</exception>
    public static string Prettify<T>(this T data)
    {
        using var performance = new PerformanceTracker(Logger, nameof(Prettify));
        
        // Note: not validating data as null because for JSON serialization,
        // null is a valid value that will render as "null"
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            Type dataType = typeof(T);
            Logger.LogDebug("Prettifying object of type {Type}", dataType.Name);
            
            string result = JsonSerializer.Serialize(data, PrettifyOptions);
            
            Logger.LogDebug("Successfully prettified object to JSON ({Length} characters)", result.Length);
                
            return result;
        }, 
        (ex, msg) => new JsonFormattingException($"Failed to prettify object to JSON: {msg}", ex), 
        "Failed to prettify object to JSON") ?? (data == null ? "null" : "{}");
    }
    
    /// <summary>
    /// Prettifies a JSON string with proper indentation for human readability.
    /// This converts a compact JSON string into a well-formatted, indented representation.
    /// 
    /// <example>
    /// <code>
    /// // Compact, hard-to-read JSON
    /// string compactJson = "{\"employees\":[{\"id\":1,\"name\":\"Alice\",\"department\":\"Engineering\"},{\"id\":2,\"name\":\"Bob\",\"department\":\"HR\"}],\"company\":\"Acme Inc.\"}";
    /// 
    /// // Make it readable
    /// string readable = compactJson.PrettifyJson();
    /// 
    /// // This displays:
    /// // {
    /// //   "employees": [
    /// //     {
    /// //       "id": 1,
    /// //       "name": "Alice",
    /// //       "department": "Engineering"
    /// //     },
    /// //     {
    /// //       "id": 2,
    /// //       "name": "Bob",
    /// //       "department": "HR"
    /// //     }
    /// //   ],
    /// //   "company": "Acme Inc."
    /// // }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to prettify.</param>
    /// <returns>A prettified JSON string with proper indentation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the input string is null or empty.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonFormattingException">Thrown when prettification fails.</exception>
    public static string PrettifyJson(this string json)
    {
        using var performance = new PerformanceTracker(Logger, nameof(PrettifyJson));
        
        // Validate input
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            Logger.LogDebug("Prettifying JSON string of length {Length}", json.Length);
            
            using var doc = JsonDocument.Parse(json);
            string result = JsonSerializer.Serialize(doc.RootElement, PrettifyOptions);
            
            Logger.LogDebug("Successfully prettified JSON to {ResultLength} characters", result.Length);
                
            return result;
        }, 
        (ex, msg) => {
            if (ex is JsonException jsonEx)
                return new JsonParsingException("Failed to prettify JSON: Invalid JSON format", jsonEx);
                
            return new JsonFormattingException($"Failed to prettify JSON: {msg}", ex);
        }, 
        "Failed to prettify JSON") ?? json;
    }
    
    /// <summary>
    /// Prettifies a JSON string using custom indentation settings.
    /// This allows for precise control over how the formatted JSON looks.
    /// 
    /// <example>
    /// <code>
    /// // Compact JSON
    /// string json = "{\"menu\":{\"items\":[{\"id\":1,\"label\":\"Home\"},{\"id\":2,\"label\":\"Settings\"}]}}";
    /// 
    /// // Prettify with 4-space indentation and tab indentation character
    /// string indentedWithTabs = json.PrettifyJsonWithIndentation(indentSize: 4, useTabsForIndentation: true);
    /// 
    /// // Prettify with 3-space indentation
    /// string indentedWith3Spaces = json.PrettifyJsonWithIndentation(indentSize: 3);
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to prettify.</param>
    /// <param name="indentSize">Size of each indentation level (default: 2). Maximum value is 8.</param>
    /// <param name="useTabsForIndentation">Whether to use tabs instead of spaces for indentation (default: false).</param>
    /// <returns>A prettified JSON string with custom indentation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the input string is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when indentSize is less than 1 or greater than 8.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonFormattingException">Thrown when prettification fails.</exception>
    public static string PrettifyJsonWithIndentation(this string json, int indentSize = DefaultIndentSize, bool useTabsForIndentation = false)
    {
        using var performance = new PerformanceTracker(Logger, nameof(PrettifyJsonWithIndentation));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfFalse(indentSize >= 1 && indentSize <= MaxIndentSize, 
            $"Indent size must be between 1 and {MaxIndentSize}", nameof(indentSize));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            Logger.LogDebug("Prettifying JSON string of length {Length} with indentSize={IndentSize}, useTabs={UseTabs}", 
                json.Length, indentSize, useTabsForIndentation);
            
            using var doc = JsonDocument.Parse(json);
            
            // Create custom options with specified indentation
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            string result;
            
            // In .NET 6+, we can set JsonSerializerOptions.IndentSize and JsonSerializerOptions.IndentCharacter
            // But these properties may not be available in all versions, so we handle it differently if needed
            try
            {
                // Set the indent size and character using reflection to handle different .NET versions
                var indentSizeProperty = typeof(JsonSerializerOptions).GetProperty("IndentSize");
                if (indentSizeProperty != null)
                {
                    indentSizeProperty.SetValue(options, indentSize);
                    
                    // If we can set the indent size, we can also set the character
                    var indentCharProperty = typeof(JsonSerializerOptions).GetProperty("IndentCharacter");
                    if (indentCharProperty != null && useTabsForIndentation)
                    {
                        indentCharProperty.SetValue(options, '\t');
                    }
                    
                    result = JsonSerializer.Serialize(doc.RootElement, options);
                }
                else
                {
                    // Fall back to standard formatting and post-process
                    result = JsonSerializer.Serialize(doc.RootElement, PrettifyOptions);
                    
                    // Post-process the result to adjust indentation
                    if (indentSize != DefaultIndentSize || useTabsForIndentation)
                    {
                        result = AdjustIndentation(result, indentSize, useTabsForIndentation);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to set custom indentation options, falling back to post-processing");
                
                // Fall back to standard formatting and post-process
                result = JsonSerializer.Serialize(doc.RootElement, PrettifyOptions);
                result = AdjustIndentation(result, indentSize, useTabsForIndentation);
            }
            
            Logger.LogDebug("Successfully prettified JSON to {ResultLength} characters with custom indentation", 
                result.Length);
                
            return result;
        }, 
        (ex, msg) => {
            if (ex is JsonException jsonEx)
                return new JsonParsingException("Failed to prettify JSON with custom indentation: Invalid JSON format", jsonEx);
                
            return new JsonFormattingException($"Failed to prettify JSON with custom indentation: {msg}", ex);
        }, 
        "Failed to prettify JSON with custom indentation") ?? json;
    }
    
    /// <summary>
    /// Adjusts the indentation of a pre-formatted JSON string to use the specified indentation settings.
    /// </summary>
    /// <param name="json">The pre-formatted JSON string.</param>
    /// <param name="indentSize">The desired indentation size.</param>
    /// <param name="useTabsForIndentation">Whether to use tabs instead of spaces.</param>
    /// <returns>The JSON string with adjusted indentation.</returns>
    private static string AdjustIndentation(string json, int indentSize, bool useTabsForIndentation)
    {
        // Split the JSON string into lines
        string[] lines = json.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        
        // Process each line to adjust indentation
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;
                
            // Count the number of leading spaces
            int leadingSpaces = line.Length - line.TrimStart().Length;
            
            // Calculate the indentation level
            int indentLevel = leadingSpaces / 2; // Assuming default indent is 2 spaces
            
            // Create the new indentation
            string newIndent;
            if (useTabsForIndentation)
            {
                newIndent = new string('\t', indentLevel);
            }
            else
            {
                newIndent = new string(' ', indentLevel * indentSize);
            }
            
            // Replace the indentation
            lines[i] = newIndent + line.TrimStart();
        }
        
        // Join the lines back together
        return string.Join(Environment.NewLine, lines);
    }
    
    /// <summary>
    /// Attempts to minify a JSON string without throwing exceptions.
    /// </summary>
    /// <param name="json">The JSON string to minify.</param>
    /// <param name="result">When successful, contains the minified JSON; otherwise, the original JSON or empty string.</param>
    /// <returns>True if minification was successful; otherwise, false.</returns>
    public static bool TryMinify(this string json, out string result)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            result = string.Empty;
            return false;
        }
        
        try
        {
            result = Minify(json);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error minifying JSON");
            result = json;  // Return original on error
            return false;
        }
    }
    
    /// <summary>
    /// Attempts to prettify an object into an indented JSON string without throwing exceptions.
    /// </summary>
    /// <typeparam name="T">The type of object to prettify.</typeparam>
    /// <param name="data">The object to prettify.</param>
    /// <param name="result">When successful, contains the prettified JSON; otherwise, null.</param>
    /// <returns>True if prettification was successful; otherwise, false.</returns>
    public static bool TryPrettify<T>(this T data, out string? result)
    {
        try
        {
            result = Prettify(data);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error prettifying object to JSON");
            result = null;
            return false;
        }
    }
    
    /// <summary>
    /// Attempts to prettify a JSON string without throwing exceptions.
    /// </summary>
    /// <param name="json">The JSON string to prettify.</param>
    /// <param name="result">When successful, contains the prettified JSON; otherwise, the original JSON or empty string.</param>
    /// <returns>True if prettification was successful; otherwise, false.</returns>
    public static bool TryPrettifyJson(this string json, out string result)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            result = string.Empty;
            return false;
        }
        
        try
        {
            result = PrettifyJson(json);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error prettifying JSON");
            result = json;  // Return original on error
            return false;
        }
    }
    
    /// <summary>
    /// Checks if a JSON string is already minified (has no unnecessary whitespace).
    /// This can be useful to avoid redundant minification operations.
    /// 
    /// <example>
    /// <code>
    /// string json = "{\"name\":\"John\",\"age\":30}";
    /// 
    /// // Check if already minified before processing
    /// if (!json.IsMinified())
    /// {
    ///     json = json.Minify();
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to check.</param>
    /// <returns>True if the JSON string appears to be minified; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the input string is null or empty.</exception>
    public static bool IsMinified(this string json)
    {
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        
        // Check for common indicators of non-minified JSON
        return !json.Contains(" ") && 
               !json.Contains("\t") && 
               !json.Contains("\r") && 
               !json.Contains("\n");
    }
    
    /// <summary>
    /// Checks if a JSON string is already prettified (has consistent indentation).
    /// This can be useful to avoid redundant prettification operations.
    /// 
    /// <example>
    /// <code>
    /// string json = GetJsonFromSomeSource();
    /// 
    /// // Check if already prettified before processing
    /// if (!json.IsPrettified())
    /// {
    ///     json = json.PrettifyJson();
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to check.</param>
    /// <returns>True if the JSON string appears to be prettified; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the input string is null or empty.</exception>
    public static bool IsPrettified(this string json)
    {
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        
        // Look for common indicators of prettified JSON:
        // 1. Contains newlines
        // 2. Contains consistent indentation patterns
        
        if (!json.Contains("\n") && !json.Contains("\r"))
            return false;
            
        // Check for indentation pattern (e.g., 2 or 4 spaces after newline)
        return (json.Contains("\n  ") || json.Contains("\r\n  ") || 
                json.Contains("\n    ") || json.Contains("\r\n    ") ||
                json.Contains("\n\t") || json.Contains("\r\n\t"));
    }
}