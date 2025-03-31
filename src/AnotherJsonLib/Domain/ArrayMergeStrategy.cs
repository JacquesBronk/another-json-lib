namespace AnotherJsonLib.Domain;

/// <summary>
/// Defines strategies for merging JSON arrays.
/// </summary>
public enum ArrayMergeStrategy
{
    /// <summary>
    /// Concatenate arrays from source and patch.
    /// </summary>
    Concat,
    
    /// <summary>
    /// Replace source array with patch array.
    /// </summary>
    Replace,
    
    /// <summary>
    /// Merge arrays by position (when same length) or concatenate (when different lengths).
    /// </summary>
    Merge
}

