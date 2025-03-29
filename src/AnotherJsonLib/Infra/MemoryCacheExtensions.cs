using Microsoft.Extensions.Caching.Memory;

namespace AnotherJsonLib.Infra;

public static class MemoryCacheExtensions
{
    static readonly List<object> entries = new();

    /// <summary>  
    /// Tries to get the value from the cache and tracks the entry if it doesn't exist.
    /// </summary>  
    /// <param name="cache"></param>  
    /// <param name="key"></param>  
    /// <param name="value"></param>  
    /// <returns></returns>  
    public static bool TryGetValueExtension(this IMemoryCache cache, object key, out object value)
    {
        if (!cache.TryGetValue(key, out value))
        {
            entries.Add(key);
            return false;
        }
        return true;
    }

    /// <summary>  
    /// Removes all entries, added via the "TryGetValueExtension()" method  
    /// </summary>  
    /// <param name="cache"></param>  
    public static void Clear(this IMemoryCache cache)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            cache.Remove(entries[i]);
        }

        entries.Clear();
    }
}
