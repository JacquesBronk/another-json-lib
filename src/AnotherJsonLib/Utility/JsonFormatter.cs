using System.Text.Encodings.Web;
using System.Text.Json;
using AnotherJsonLib.Exceptions;
using Microsoft.Extensions.Logging;
using AnotherJsonLib.Infra;

namespace AnotherJsonLib.Utility;

/// <summary>
/// Provides methods to format JSON strings, including minification and prettification.
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
    /// Minifies the JSON string by removing unnecessary whitespace.
    /// </summary>
    /// <param name="json">The JSON string to minify.</param>
    /// <returns>A minified JSON string.</returns>
    /// <exception cref="JsonArgumentException">Thrown when the input string is null or empty.</exception>
    /// <exception cref="JsonOperationException">Thrown when minification fails.</exception>
    /// <example>
    /// <code>
    /// string json = "{ \"name\": \"John\", \"age\": 30 }";
    /// string minifiedJson = json.Minify();
    /// // Result: {"name":"John","age":30}
    /// </code>
    /// </example>
    public static string Minify(this string json)
    {
        using var performance = new PerformanceTracker(Logger, nameof(Minify));
        
        // Validate input
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            using var doc = JsonDocument.Parse(json);
            string result = JsonSerializer.Serialize(doc.RootElement, MinifyOptions);
            
            Logger.LogDebug("Successfully minified JSON from {OriginalLength} to {MinifiedLength} characters",
                json.Length, result.Length);
                
            return result;
        }, (ex, msg) => 
        {
            if (ex is JsonException jsonEx)
                return new JsonParsingException("Failed to minify JSON: Invalid JSON format", jsonEx);
                
            return new JsonOperationException("Failed to minify JSON: " + msg, ex);
        }, "Failed to minify JSON") ?? json;
    }

    /// <summary>
    /// Prettifies an object into an indented JSON string.
    /// </summary>
    /// <typeparam name="T">The type of object to prettify.</typeparam>
    /// <param name="data">The object to prettify.</param>
    /// <returns>A prettified JSON string with proper indentation.</returns>
    /// <exception cref="JsonOperationException">Thrown when prettification fails.</exception>
    /// <example>
    /// <code>
    /// var obj = new { Name = "John", Age = 30 };
    /// string prettyJson = obj.Prettify();
    /// // Result:
    /// // {
    /// //   "Name": "John",
    /// //   "Age": 30
    /// // }
    /// </code>
    /// </example>
    public static string Prettify<T>(this T data)
    {
        using var performance = new PerformanceTracker(Logger, nameof(Prettify));
        
        // Note: not validating data as null because for JSON serialization,
        // null is a valid value that will render as "null"
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            string result = JsonSerializer.Serialize(data, PrettifyOptions);
            
            Logger.LogDebug("Successfully prettified object of type {Type} to JSON ({Length} characters)",
                typeof(T).Name, result.Length);
                
            return result;
        }, (ex, msg) => new JsonOperationException("Failed to prettify object to JSON: " + msg, ex), "Failed to prettify object to JSON") ?? (data == null ? "null" : "{}");
    }
    
    /// <summary>
    /// Prettifies a JSON string with proper indentation.
    /// </summary>
    /// <param name="json">The JSON string to prettify.</param>
    /// <returns>A prettified JSON string with proper indentation.</returns>
    /// <exception cref="JsonArgumentException">Thrown when the input string is null or empty.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonOperationException">Thrown when prettification fails.</exception>
    /// <example>
    /// <code>
    /// string json = "{\"name\":\"John\",\"age\":30}";
    /// string prettyJson = json.PrettifyJson();
    /// // Result:
    /// // {
    /// //   "name": "John",
    /// //   "age": 30
    /// // }
    /// </code>
    /// </example>
    public static string PrettifyJson(this string json)
    {
        using var performance = new PerformanceTracker(Logger, nameof(PrettifyJson));
        
        // Validate input
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            using var doc = JsonDocument.Parse(json);
            string result = JsonSerializer.Serialize(doc.RootElement, PrettifyOptions);
            
            Logger.LogDebug("Successfully prettified JSON string from {OriginalLength} to {PrettifiedLength} characters",
                json.Length, result.Length);
                
            return result;
        }, (ex, msg) => 
        {
            if (ex is JsonException jsonEx)
                return new JsonParsingException("Failed to prettify JSON: Invalid JSON format", jsonEx);
                
            return new JsonOperationException("Failed to prettify JSON: " + msg, ex);
        }, "Failed to prettify JSON") ?? json;
    }
    
    /// <summary>
    /// Attempts to minify a JSON string by removing unnecessary whitespace.
    /// </summary>
    /// <param name="json">The JSON string to minify.</param>
    /// <param name="result">When successful, contains the minified JSON; otherwise, null.</param>
    /// <returns>True if minification was successful; otherwise, false.</returns>
    public static bool TryMinify(this string json, out string? result)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            result = null;
            return false;
        }
        
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => Minify(json),
            null,
            "Failed to minify JSON"
        );
        
        return result != null;
    }
    
    /// <summary>
    /// Attempts to prettify an object into an indented JSON string.
    /// </summary>
    /// <typeparam name="T">The type of object to prettify.</typeparam>
    /// <param name="data">The object to prettify.</param>
    /// <param name="result">When successful, contains the prettified JSON; otherwise, null.</param>
    /// <returns>True if prettification was successful; otherwise, false.</returns>
    public static bool TryPrettify<T>(this T data, out string? result)
    {
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => Prettify(data),
            null,
            "Failed to prettify object to JSON"
        );
        
        return result != null;
    }
    
    /// <summary>
    /// Attempts to prettify a JSON string with proper indentation.
    /// </summary>
    /// <param name="json">The JSON string to prettify.</param>
    /// <param name="result">When successful, contains the prettified JSON; otherwise, null.</param>
    /// <returns>True if prettification was successful; otherwise, false.</returns>
    public static bool TryPrettifyJson(this string json, out string? result)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            result = null;
            return false;
        }
        
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => PrettifyJson(json),
            null,
            "Failed to prettify JSON"
        );
        
        return result != null;
    }
}