using System.Text.Json;
using System.Text.RegularExpressions;
using AnotherJsonLib.Infra;
using Microsoft.Extensions.Caching.Memory;

namespace AnotherJsonLib.Utility;

public static class JsonPathQuery
{
    // The cache is held in a thread-safe MemoryCache.
    private static IMemoryCache _queryCache = new MemoryCache(new MemoryCacheOptions());

    /// <summary>
    /// Configures the cache options for caching parsed JSONPath queries.
    /// Call this early in application initialization to override defaults.
    /// </summary>
    /// <param name="options">Custom MemoryCacheOptions.</param>
    public static void ConfigureCache(MemoryCacheOptions options)
    {
        // Dispose the current cache and create a new one with the provided options.
        _queryCache.Dispose();
        _queryCache = new MemoryCache(options);
    }

    /// <summary>
    /// Clears all cached JSONPath query tokens.
    /// </summary>
    public static void ClearCache() => _queryCache.Clear();

    /// <summary>
    /// Removes the cached tokens for a specific JSONPath.
    /// </summary>
    /// <param name="jsonPath">The JSONPath string whose cache entry should be removed.</param>
    public static void RemoveCacheEntry(string jsonPath)
    {
        _queryCache.Remove(jsonPath);
    }

    // Parses the JSONPath query string into tokens.
    private static string[] ParseJsonPath(string jsonPath)
    {
        string trimmed = jsonPath.Trim();
        if (trimmed.StartsWith("$"))
            trimmed = trimmed.Substring(1);
        if (trimmed.StartsWith("."))
            trimmed = trimmed.Substring(1);
        return trimmed.Split('.').Where(part => !string.IsNullOrEmpty(part)).ToArray();
    }

    /// <summary>
    /// Queries the JsonDocument using the provided JSONPath.
    /// Parsed tokens are cached for improved performance.
    /// </summary>
    /// <param name="jsonDocument">The JsonDocument to query.</param>
    /// <param name="jsonPath">The JSONPath query string.</param>
    /// <returns>An enumerable of matching JsonElements (or null if a match is JSON null).</returns>
    public static IEnumerable<JsonElement?> QueryJsonElement(this JsonDocument jsonDocument, string jsonPath)
    {
        if (jsonDocument == null)
            throw new ArgumentNullException(nameof(jsonDocument));
        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new ArgumentException("jsonPath cannot be null or whitespace", nameof(jsonPath));

        // Try to get parsed tokens from cache; if not present, parse and cache with a default sliding expiration.
        var tokens = _queryCache.GetOrCreate(jsonPath, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(10);
            return ParseJsonPath(jsonPath);
        });

        return QueryJsonElement(jsonDocument.RootElement, tokens, 0);
    }

    private static IEnumerable<JsonElement?> QueryJsonElement(JsonElement element, string[] tokens, int index)
    {
        if (index >= tokens.Length)
        {
            yield return element;
            yield break;
        }

        string token = tokens[index];

        if (token == "##")
        {
            // Descendant operator: traverse all descendants.
            foreach (var descendant in DescendantsOrSelf(element))
            {
                foreach (var match in QueryJsonElement(descendant, tokens, index + 1))
                    yield return match;
            }
        }
        else if (token == "*")
        {
            // Wildcard for all properties.
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    foreach (var match in QueryJsonElement(property.Value, tokens, index + 1))
                        yield return match;
                }
            }
        }
        else if (token.EndsWith("]"))
        {
            // Handle property with array indices, e.g. "items[1,2]"
            var match = Regex.Match(token, @"^(.*)\[(.+)\]$");
            if (match.Success)
            {
                string propertyName = match.Groups[1].Value;
                string indicesPart = match.Groups[2].Value;
                var indices = indicesPart.Split(',').Select(int.Parse);
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName,
                                                                  out var childElement)
                                                              && childElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var idx in indices)
                    {
                        if (idx >= 0 && idx < childElement.GetArrayLength())
                            yield return childElement[idx];
                    }
                }
            }
        }
        else
        {
            // Regular property access.
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(token, out var child))
            {
                foreach (var m in QueryJsonElement(child, tokens, index + 1))
                    yield return m;
            }
        }
    }

    // Returns the element and all its descendants using a depth-first traversal.
    private static IEnumerable<JsonElement> DescendantsOrSelf(JsonElement root)
    {
        var stack = new Stack<JsonElement>();
        stack.Push(root);
        while (stack.Any())
        {
            var current = stack.Pop();
            yield return current;
            if (current.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in current.EnumerateObject())
                    stack.Push(property.Value);
            }
            else if (current.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in current.EnumerateArray())
                    stack.Push(item);
            }
        }
    }
}