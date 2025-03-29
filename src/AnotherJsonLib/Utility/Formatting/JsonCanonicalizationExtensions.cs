using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Helper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Formatting;

/// <summary>
/// Provides caching extensions for JSON canonicalization operations.
/// 
/// JSON canonicalization converts JSON to a standardized format with consistent property ordering,
/// whitespace, and formatting. This class adds caching capabilities to make repeated canonicalization
/// operations more efficient.
/// 
/// Use this class when:
/// - You need to compare the same JSON objects frequently
/// - You're generating digital signatures or hashes from JSON data
/// - You need to ensure consistent serialization across systems
/// 
/// <example>
/// <code>
/// // Configure the cache with custom settings (optional)
/// var cacheOptions = new MemoryCacheOptions 
/// { 
///     SizeLimit = 1024, 
///     ExpirationScanFrequency = TimeSpan.FromMinutes(5)
/// };
/// JsonCanonicalizationCacheExtensions.ConfigureCanonicalizationCache(cacheOptions);
/// 
/// // Sample JSON with inconsistent formatting
/// string json1 = @"{
///   ""name"": ""John"",
///   ""age"": 30
/// }";
/// 
/// string json2 = @"{ ""age"": 30, ""name"": ""John"" }";
/// 
/// // Use the cached canonicalization
/// string canonical1 = json1.CanonicalizeCached();
/// string canonical2 = json2.CanonicalizeCached();
/// 
/// // canonical1 and canonical2 will be identical:
/// // {"age":30,"name":"John"}
/// 
/// // Clear the cache when needed
/// JsonCanonicalizationCacheExtensions.ClearCanonicalizationCache();
/// </code>
/// </example>
/// </summary>
public static class JsonCanonicalizationCacheExtensions
{
    // The MemoryCache instance for caching canonicalized JSON strings.
    private static IMemoryCache _canonicalCache = new MemoryCache(new MemoryCacheOptions());
    
    // Logger for this class
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger("JsonCanonicalization");

    /// <summary>
    /// Configures the canonicalization cache with custom MemoryCacheOptions.
    /// Call this early during application initialization to override defaults.
    /// </summary>
    /// <param name="options">Custom MemoryCacheOptions to use.</param>
    /// <exception cref="JsonCanonicalizationException">Thrown when cache configuration fails</exception>
    public static void ConfigureCanonicalizationCache(MemoryCacheOptions options)
    {
        ExceptionHelpers.ThrowIfNull(options, nameof(options));
        
        ExceptionHelpers.SafeExecute(
            () => {
                var oldCache = _canonicalCache;
                _canonicalCache = new MemoryCache(options);
                oldCache.Dispose();
                Logger.LogInformation("Canonicalization cache reconfigured with custom options");
            },
            (ex, msg) => new JsonCanonicalizationException($"Failed to configure canonicalization cache: {msg}", ex),
            "Error configuring canonicalization cache with custom options"
        );
    }

    /// <summary>
    /// Clears all entries in the canonicalization cache.
    /// </summary>
    public static void ClearCanonicalizationCache()
    {
        ExceptionHelpers.SafeExecute(
            () => {
                _canonicalCache.Clear();
                Logger.LogDebug("Canonicalization cache cleared");
            },
            (ex, msg) => new JsonCanonicalizationException($"Failed to clear canonicalization cache: {msg}", ex),
            "Error clearing canonicalization cache"
        );
    }

    /// <summary>
    /// Canonicalizes a JSON string using caching.
    /// Repeated calls with the same input within the expiration window will return the cached result.
    /// 
    /// This method is particularly useful when you need to canonicalize the same JSON strings repeatedly,
    /// such as when validating multiple signatures against the same JSON payload or when comparing
    /// semantically equivalent JSON objects with different formatting.
    /// 
    /// <example>
    /// <code>
    /// // Original JSON with inconsistent formatting
    /// string originalJson = @"{
    ///   ""items"": [
    ///     { ""id"": 2, ""name"": ""Item 2"" },
    ///     { ""id"": 1, ""name"": ""Item 1"" }
    ///   ]
    /// }";
    /// 
    /// // First call computes and caches the canonical form
    /// string canonical1 = originalJson.CanonicalizeCached();
    /// 
    /// // Second call with the same JSON returns the cached result without reprocessing
    /// string canonical2 = originalJson.CanonicalizeCached();
    /// 
    /// // Result: {"items":[{"id":2,"name":"Item 2"},{"id":1,"name":"Item 1"}]}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The input JSON string.</param>
    /// <returns>The canonical JSON string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the input JSON string is null.</exception>
    /// <exception cref="JsonCanonicalizationException">Thrown when the JSON cannot be canonicalized due to formatting issues.</exception>
    public static string CanonicalizeCached(this string json)
    {
        ExceptionHelpers.ThrowIfNull(json, nameof(json));
        using var performance = new PerformanceTracker(Logger, nameof(CanonicalizeCached));
        return ExceptionHelpers.SafeExecute(
            () => {
                string? result = _canonicalCache.GetOrCreate(json, entry => 
                {
                    entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                    
                    Logger.LogTrace("Cache miss - canonicalizing JSON: {JsonPrefix}", 
                        json.Length > 50 ? json.Substring(0, 50) + "..." : json);
                    
                    // Use another SafeExecute for the inner operation
                    return ExceptionHelpers.SafeExecute(
                        () => JsonCanonicalizer.Canonicalize(json),
                        (ex, _) => new JsonCanonicalizationException("Failed to canonicalize JSON", ex),
                        "JSON canonicalization operation failed"
                    );
                });
                
                return result ?? string.Empty;
            },
            (ex, msg) => new JsonCanonicalizationException($"Error in JSON canonicalization: {msg}", ex),
            "Failed to canonicalize or retrieve cached JSON"
        ) ?? string.Empty;
    }
}