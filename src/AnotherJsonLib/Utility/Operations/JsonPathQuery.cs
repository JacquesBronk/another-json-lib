using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Operations;

/// <summary>
/// Provides powerful JSON Path querying capabilities for extracting and navigating data in JSON documents.
/// 
/// JSON Path is a query language for JSON, similar to XPath for XML, allowing you to select and extract 
/// data from complex JSON structures using path expressions. This utility simplifies working with deeply 
/// nested JSON data by providing:
/// 
/// - Efficient navigation through complex JSON structures
/// - Filtering and selecting specific elements using path expressions
/// - Support for wildcards and recursive descent operators
/// - Optimized performance through path expression caching
/// 
/// <example>
/// <code>
/// // Example JSON document
/// string json = @"{
///     ""store"": {
///         ""books"": [
///             {""category"": ""fiction"", ""title"": ""The Night Dragon"", ""price"": 19.99},
///             {""category"": ""fiction"", ""title"": ""Sword of Destiny"", ""price"": 15.99},
///             {""category"": ""non-fiction"", ""title"": ""The History of Computing"", ""price"": 29.99}
///         ],
///         ""location"": {""city"": ""New York"", ""zipcode"": ""10001""}
///     }
/// }";
/// 
/// using var document = JsonDocument.Parse(json);
/// 
/// // Get all book titles
/// var titles = document.QueryJsonElement("$.store.books[*].title");
/// // Results: "The Night Dragon", "Sword of Destiny", "The History of Computing"
/// 
/// // Get fiction books only
/// var fictionBooks = document.QueryJsonElement("$.store.books[?(@.category=='fiction')]");
/// 
/// // Get the store location
/// var location = document.QueryJsonElement("$.store.location");
/// </code>
/// </example>
/// </summary>
public static class JsonPathQuery
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonPathQuery));
    
    // Thread-safe cache for parsed JSONPath queries
    private static readonly ConcurrentDictionary<string, string[]> QueryCache = new();
    
    // Maximum cache size (can be adjusted with ConfigureCache)
    private static int _maxCacheSize = 1000;
    
    // Default cache expiration timespan
    private static TimeSpan _defaultCacheExpiration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Configures the cache options for storing parsed JSONPath expressions.
    /// 
    /// <example>
    /// <code>
    /// // Custom configuration for high-traffic applications
    /// JsonPathQuery.ConfigureCache(
    ///     maxCacheSize: 5000,  
    ///     cacheExpiration: TimeSpan.FromHours(1)
    /// );
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="maxCacheSize">Maximum number of cached path expressions (default: 1000).</param>
    /// <param name="cacheExpiration">Cache entry expiration timespan (default: 30 minutes).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid.</exception>
    public static void ConfigureCache(int maxCacheSize = 1000, TimeSpan? cacheExpiration = null)
    {
        using var performance = new PerformanceTracker(Logger, nameof(ConfigureCache));

        ExceptionHelpers.SafeExecute(() =>
            {
                if (maxCacheSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(maxCacheSize), "Cache size must be greater than zero");
                
                _maxCacheSize = maxCacheSize;
            
                if (cacheExpiration.HasValue)
                {
                    if (cacheExpiration.Value <= TimeSpan.Zero)
                        throw new ArgumentOutOfRangeException(nameof(cacheExpiration), "Cache expiration must be greater than zero");
                    
                    _defaultCacheExpiration = cacheExpiration.Value;
                }
            
                Logger.LogInformation("JSON Path query cache configured with size: {CacheSize}, expiration: {CacheExpiration}", 
                    _maxCacheSize, _defaultCacheExpiration);
                
                // Trim cache if needed
                if (QueryCache.Count > _maxCacheSize)
                {
                    TrimCache();
                }
            },
            (ex, msg) => new JsonPathException($"Failed to configure JSONPath query cache: {msg}", ex),
            "Error configuring JSONPath query cache");
    }

    /// <summary>
    /// Clears all cached JSONPath query tokens.
    /// </summary>
    public static void ClearCache()
    {
        using var performance = new PerformanceTracker(Logger, nameof(ClearCache));
        
        ExceptionHelpers.SafeExecute(() =>
        {
            QueryCache.Clear();
            Logger.LogDebug("JSON Path query cache cleared");
        },
        (ex, msg) => new JsonPathException($"Failed to clear JSONPath query cache: {msg}", ex),
        "Error clearing JSONPath query cache");
    }

    /// <summary>
    /// Removes the cached tokens for a specific JSONPath.
    /// </summary>
    /// <param name="jsonPath">The JSONPath string whose cache entry should be removed.</param>
    public static void RemoveCacheEntry(string jsonPath)
    {
        using var performance = new PerformanceTracker(Logger, nameof(RemoveCacheEntry));
        
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(jsonPath, nameof(jsonPath));
        
        ExceptionHelpers.SafeExecute(() =>
        {
            if (QueryCache.TryRemove(jsonPath, out _))
            {
                Logger.LogTrace("Removed cache entry for JSON path: {JsonPath}", jsonPath);
            }
            else
            {
                Logger.LogTrace("Cache entry not found for JSON path: {JsonPath}", jsonPath);
            }
        },
        (ex, msg) => new JsonPathException($"Failed to remove JSONPath cache entry: {msg}", ex),
        "Error removing JSONPath cache entry");
    }

    /// <summary>
    /// Trims the cache to stay within the maximum size by removing the oldest entries.
    /// </summary>
    private static void TrimCache()
    {
        // Simple approach: remove approximately half the entries when we exceed the limit
        // A more sophisticated implementation would use a time-based LRU cache
        int entriesToRemove = QueryCache.Count - _maxCacheSize;
        if (entriesToRemove <= 0) return;
        
        int countToRemove = Math.Min(entriesToRemove + (_maxCacheSize / 2), QueryCache.Count);
        
        var keysToRemove = QueryCache.Keys.Take(countToRemove).ToList();
        foreach (var key in keysToRemove)
        {
            QueryCache.TryRemove(key, out _);
        }
        
        Logger.LogDebug("Trimmed JSON Path query cache by removing {RemovedCount} entries", keysToRemove.Count);
    }

    /// <summary>
    /// Parses a JSONPath string into token segments for querying.
    /// </summary>
    /// <param name="jsonPath">The JSONPath string to parse.</param>
    /// <returns>An array of path segments.</returns>
    private static string[] ParseJsonPath(string jsonPath)
    {
        string trimmed = jsonPath.Trim();
        
        // Normalize the path by removing leading $ and dot if present
        if (trimmed.StartsWith("$"))
            trimmed = trimmed.Substring(1);
        if (trimmed.StartsWith("."))
            trimmed = trimmed.Substring(1);
            
        // Handle special case for empty path (root)
        if (string.IsNullOrEmpty(trimmed))
            return Array.Empty<string>();
            
        // Split by dots and filter out empty segments
        return trimmed.Split('.')
            .Where(part => !string.IsNullOrEmpty(part))
            .ToArray();
    }

    /// <summary>
    /// Queries a JsonDocument using a JSONPath expression and returns matching elements.
    /// Parsed expressions are cached for improved performance with repeated queries.
    /// 
    /// <para>
    /// Supported JSONPath features:
    /// - Root element: $ (optional)
    /// - Child operator: . (dot notation)
    /// - Array indexing: [0], [1,2], etc.
    /// - Wildcards: * (matches any property or array element)
    /// - Recursive descent: ## (searches through all descendants)
    /// </para>
    /// 
    /// <example>
    /// <code>
    /// string json = @"{
    ///     ""people"": [
    ///         {""name"": ""Alice"", ""age"": 25, ""role"": ""Developer""},
    ///         {""name"": ""Bob"", ""age"": 30, ""role"": ""Manager""},
    ///         {""name"": ""Charlie"", ""age"": 35, ""role"": ""Developer""}
    ///     ]
    /// }";
    /// 
    /// using var document = JsonDocument.Parse(json);
    /// 
    /// // Example 1: Get all people's names
    /// var names = document.QueryJsonElement("$.people[*].name")
    ///     .Select(el => el?.GetString())
    ///     .ToList();
    /// // Result: ["Alice", "Bob", "Charlie"]
    /// 
    /// // Example 2: Get person at index 1
    /// var secondPerson = document.QueryJsonElement("$.people[1]").FirstOrDefault();
    /// // Result: {"name": "Bob", "age": 30, "role": "Manager"}
    /// 
    /// // Example 3: Find all ages anywhere in the document
    /// var ages = document.QueryJsonElement("$##.age")
    ///     .Select(el => el?.GetInt32())
    ///     .ToList();
    /// // Result: [25, 30, 35]
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="jsonDocument">The JsonDocument to query.</param>
    /// <param name="jsonPath">The JSONPath query string.</param>
    /// <returns>An enumerable of matching JsonElements (or null if a match is JSON null).</returns>
    /// <exception cref="ArgumentNullException">Thrown when jsonDocument or jsonPath is null.</exception>
    /// <exception cref="JsonPathException">Thrown when the JSONPath syntax is invalid.</exception>
    public static IEnumerable<JsonElement?> QueryJsonElement(this JsonDocument jsonDocument, string jsonPath)
    {
        using var performance = new PerformanceTracker(Logger, nameof(QueryJsonElement));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNull(jsonDocument, nameof(jsonDocument));
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(jsonPath, nameof(jsonPath));

        return ExceptionHelpers.SafeExecute(() =>
            {
                Logger.LogTrace("Querying document with JSON path: {JsonPath}", jsonPath);

                // Get or parse path tokens
                string[] tokens = QueryCache.GetOrAdd(jsonPath, path => 
                {
                    Logger.LogTrace("Cache miss - parsing JSON path: {JsonPath}", path);
                    return ParseJsonPath(path);
                });

                var results = ExecuteJsonPathQuery(jsonDocument, tokens);

                // Trim cache if it exceeds the maximum size
                if (QueryCache.Count > _maxCacheSize)
                {
                    TrimCache();
                }

                return results;
            },
            (ex, msg) => new JsonPathException($"Failed to execute JSONPath query '{jsonPath}': {msg}", ex),
            $"Error executing JSONPath query '{jsonPath}'") ?? Array.Empty<JsonElement?>();
    }

    /// <summary>
    /// Executes a JSON path query using the parsed tokens.
    /// </summary>
    private static IEnumerable<JsonElement?> ExecuteJsonPathQuery(JsonDocument jsonDocument, string[] tokens)
    {
        if (tokens.Length == 0)
        {
            // Return the root element for empty paths
            yield return jsonDocument.RootElement;
            yield break;
        }

        // Execute the query starting from the root
        foreach (var result in QueryJsonElement(jsonDocument.RootElement, tokens, 0))
        {
            yield return result;
        }
    }

    /// <summary>
    /// Recursively evaluates a JSONPath query against a JsonElement.
    /// </summary>
    /// <param name="element">The current JsonElement to query.</param>
    /// <param name="tokens">The array of JSONPath tokens.</param>
    /// <param name="index">The current position in the tokens array.</param>
    /// <returns>An enumerable of matching JsonElements.</returns>
    private static IEnumerable<JsonElement?> QueryJsonElement(JsonElement element, string[] tokens, int index)
    {
        if (index >= tokens.Length)
        {
            yield return element;
            yield break;
        }

        string token = tokens[index];

        // Handle special operators
        if (token == "##")
        {
            // Recursive descent: traverse all descendants
            foreach (var descendant in DescendantsOrSelf(element))
            {
                foreach (var match in QueryJsonElement(descendant, tokens, index + 1))
                {
                    yield return match;
                }
            }
        }
        else if (token == "*")
        {
            // Wildcard: match all properties or array elements
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    foreach (var match in QueryJsonElement(property.Value, tokens, index + 1))
                    {
                        yield return match;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var match in QueryJsonElement(item, tokens, index + 1))
                    {
                        yield return match;
                    }
                }
            }
        }
        else if (token.EndsWith("]"))
        {
            // Handle array indexing: property[indices] or direct array access [indices]
            if (TryHandleArrayIndexing(element, token, tokens, index, out var results))
            {
                foreach (var result in results)
                {
                    yield return result;
                }
            }
        }
        else
        {
            // Regular property access
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(token, out var childElement))
            {
                foreach (var match in QueryJsonElement(childElement, tokens, index + 1))
                {
                    yield return match;
                }
            }
        }
    }

    /// <summary>
    /// Handles array indexing operations in JSONPath, supporting both property[indices] and direct [indices] syntax.
    /// </summary>
    /// <param name="element">The current JSON element being processed.</param>
    /// <param name="token">The current token containing array indexing.</param>
    /// <param name="tokens">All path tokens.</param>
    /// <param name="index">Current position in the tokens array.</param>
    /// <param name="results">Output enumerable of matching elements.</param>
    /// <returns>True if array indexing was handled; otherwise, false.</returns>
    private static bool TryHandleArrayIndexing(
        JsonElement element,
        string token,
        string[] tokens,
        int index,
        out IEnumerable<JsonElement?> results)
    {
        // Initialize empty result
        results = Enumerable.Empty<JsonElement?>();
        
        // Match pattern "property[indices]" or direct "[indices]"
        var match = Regex.Match(token, @"^(?:(.*)\[|\[)(.+)\]$");
        if (!match.Success) return false;
        
        string? propertyName = match.Groups[1].Success ? match.Groups[1].Value : null;
        string indicesPart = match.Groups[2].Value;
        
        // Get the element to index (either a property or the element itself)
        JsonElement elementToIndex;
        
        if (propertyName != null)
        {
            // Format is property[indices]
            if (element.ValueKind != JsonValueKind.Object || 
                !element.TryGetProperty(propertyName, out elementToIndex))
            {
                return false;
            }
        }
        else
        {
            // Format is direct [indices]
            elementToIndex = element;
        }
        
        // Element must be an array for indexing
        if (elementToIndex.ValueKind != JsonValueKind.Array)
        {
            return false;
        }
        
        // Handle different index formats
        if (indicesPart == "*")
        {
            // All array elements
            var matches = new List<JsonElement?>();
            foreach (var item in elementToIndex.EnumerateArray())
            {
                foreach (var matched in QueryJsonElement(item, tokens, index + 1))
                {
                    matches.Add(matched);
                }
            }
            results = matches;
            return true;
        }
        
        // Handle comma-separated indices
        var indices = indicesPart.Split(',').Select(i => i.Trim());
        var arrayResults = new List<JsonElement?>();
        
        foreach (var indexStr in indices)
        {
            if (int.TryParse(indexStr, out int arrayIndex) && 
                arrayIndex >= 0 && 
                arrayIndex < elementToIndex.GetArrayLength())
            {
                var arrayItem = elementToIndex[arrayIndex];
                foreach (var matched in QueryJsonElement(arrayItem, tokens, index + 1))
                {
                    arrayResults.Add(matched);
                }
            }
        }
        
        results = arrayResults;
        return true;
    }

    /// <summary>
    /// Returns the element and all its descendants using a depth-first traversal.
    /// </summary>
    /// <param name="root">The root element to traverse.</param>
    /// <returns>An enumerable containing the element and all its descendants.</returns>
    private static IEnumerable<JsonElement> DescendantsOrSelf(JsonElement root)
    {
        var stack = new Stack<JsonElement>();
        stack.Push(root);
        
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            
            if (current.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in current.EnumerateObject())
                {
                    stack.Push(property.Value);
                }
            }
            else if (current.ValueKind == JsonValueKind.Array)
            {
                // Add array elements in reverse order to maintain expected traversal order
                var items = current.EnumerateArray().ToArray();
                for (int i = items.Length - 1; i >= 0; i--)
                {
                    stack.Push(items[i]);
                }
            }
        }
    }
    
    /// <summary>
    /// Attempts to query a JsonDocument using a JSONPath expression,
    /// returning a success indicator instead of throwing exceptions.
    /// </summary>
    /// <param name="jsonDocument">The JsonDocument to query.</param>
    /// <param name="jsonPath">The JSONPath query string.</param>
    /// <param name="results">When successful, contains the matching elements; otherwise, an empty collection.</param>
    /// <returns>True if the query was executed successfully; otherwise, false.</returns>
    public static bool TryQueryJsonElement(
        this JsonDocument? jsonDocument, 
        string jsonPath, 
        out IEnumerable<JsonElement?> results)
    {
        if (jsonDocument == null || string.IsNullOrWhiteSpace(jsonPath))
        {
            results = Enumerable.Empty<JsonElement?>();
            return false;
        }
        
        try
        {
            results = jsonDocument.QueryJsonElement(jsonPath).ToList();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error executing JSONPath query '{JsonPath}'", jsonPath);
            results = Enumerable.Empty<JsonElement?>();
            return false;
        }
    }

    /// <summary>
    /// Queries a JSON string directly using a JSONPath expression.
    /// 
    /// <example>
    /// <code>
    /// string json = @"{""users"":[{""name"":""Alice""},{""name"":""Bob""}]}";
    /// 
    /// var names = JsonPathQuery.QueryJson(json, "$.users[*].name")
    ///     .Select(el => el?.GetString())
    ///     .ToList();
    /// // Result: ["Alice", "Bob"]
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to query.</param>
    /// <param name="jsonPath">The JSONPath query string.</param>
    /// <returns>An enumerable of matching JsonElements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json or jsonPath is null.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonPathException">Thrown when the query execution fails.</exception>
    public static IEnumerable<JsonElement?> QueryJson(string json, string jsonPath)
    {
        using var performance = new PerformanceTracker(Logger, nameof(QueryJson));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(jsonPath, nameof(jsonPath));

        return ExceptionHelpers.SafeExecute(() =>
            {
                Logger.LogDebug("Querying JSON string with JSON path: {JsonPath}", jsonPath);

                using var document = JsonDocument.Parse(json);
                var results = document.QueryJsonElement(jsonPath).ToList();

                Logger.LogDebug("JSON path query returned {Count} results", results.Count);
                return results;
            },
            (ex, msg) =>
            {
                if (ex is JsonException)
                    return new JsonParsingException("Invalid JSON format for JSONPath query", ex);
                return new JsonPathException($"Failed to execute JSONPath query: {msg}", ex);
            },
            $"Error executing JSONPath query '{jsonPath}' on JSON string") ?? new List<JsonElement?>();
    }
    
    /// <summary>
    /// Attempts to query a JSON string using a JSONPath expression,
    /// returning a success indicator instead of throwing exceptions.
    /// </summary>
    /// <param name="json">The JSON string to query.</param>
    /// <param name="jsonPath">The JSONPath query string.</param>
    /// <param name="results">When successful, contains the matching elements; otherwise, an empty collection.</param>
    /// <returns>True if the query was executed successfully; otherwise, false.</returns>
    public static bool TryQueryJson(
        string json, 
        string jsonPath, 
        out IEnumerable<JsonElement?> results)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(jsonPath))
        {
            results = Enumerable.Empty<JsonElement?>();
            return false;
        }
        
        try
        {
            results = QueryJson(json, jsonPath).ToList();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error executing JSONPath query '{JsonPath}' on JSON string", jsonPath);
            results = Enumerable.Empty<JsonElement?>();
            return false;
        }
    }
}