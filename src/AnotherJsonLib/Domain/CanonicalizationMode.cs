namespace AnotherJsonLib.Domain;

/// <summary>
/// Specifies the canonicalization mode.
/// </summary>
public enum CanonicalizationMode
{
    /// <summary>
    /// Use the basic, recursive canonicalization.
    /// </summary>
    Normal,
    /// <summary>
    /// Use a cached version; repeated JSON strings will be canonicalized only once.
    /// </summary>
    Cached,
    /// <summary>
    /// Process object properties and array items in parallel.
    /// </summary>
    Parallel,
    /// <summary>
    /// Streaming canonicalization (placeholder—currently falls back to Normal).
    /// </summary>
    Streaming
}