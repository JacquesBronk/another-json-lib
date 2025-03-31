using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace AnotherJsonLib.Helper;

/// <summary>
/// Provides thread-safe extension methods for IMemoryCache.
/// </summary>
public static class MemoryCacheExtensions
{
    // Thread-safe collection to track cache entries
    private static ConcurrentBag<object> _entries = new();

    /// <summary>  
    /// Tries to get the value from the cache and tracks the entry if it doesn't exist.
    /// This method is thread-safe and can be used in concurrent environments.
    /// </summary>  
    /// <param name="cache">The memory cache instance.</param>  
    /// <param name="key">The key to look up in the cache.</param>  
    /// <param name="value">When this method returns, contains the value associated with the key, if found; otherwise, the default value.</param>  
    /// <returns>true if the key was found in the cache; otherwise, false.</returns>  
    public static bool TryGetValueExtension(this IMemoryCache cache, object key, out object? value)
    {
        if (!cache.TryGetValue(key, out value))
        {
            _entries.Add(key);
            return false;
        }
        return true;
    }

    /// <summary>  
    /// Removes all entries that were added via the "TryGetValueExtension()" method.
    /// This method is thread-safe but will only remove entries that were added before this method was called.
    /// </summary>  
    /// <param name="cache">The memory cache instance to clear.</param>  
    public static void Clear(this IMemoryCache cache)
    {
        // Create a new empty bag to replace the current one
        var oldEntries = Interlocked.Exchange(ref _entries, new ConcurrentBag<object>());
        
        // Remove each entry from the cache
        foreach (var entry in oldEntries)
        {
            cache.Remove(entry);
        }
    }
}