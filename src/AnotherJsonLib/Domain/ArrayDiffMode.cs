namespace AnotherJsonLib.Domain;

/// <summary>
/// Specifies the array diffing mode.
/// </summary>
public enum ArrayDiffMode
{
    /// <summary>
    /// Computes a detailed diff using the Longest Common Subsequence (LCS) algorithm.
    /// This mode can detect moved elements.
    /// </summary>
    Full,
        
    /// <summary>
    /// Uses a simple linear comparison.
    /// This mode is faster but less granular.
    /// </summary>
    Fast
}