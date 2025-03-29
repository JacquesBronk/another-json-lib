namespace AnotherJsonLib.Domain;

/// <summary>
/// Specifies the mode to be used when computing the difference between two arrays.
/// </summary>
public enum ArrayDiffMode
{
    /// <summary>
    /// Performs a comprehensive element-by-element comparison to compute the difference between two arrays.
    /// This mode is more granular but may be slower for larger arrays.
    /// </summary>
    Full,

    /// <summary>
    /// Utilizes a quicker but less thorough comparison approach to compute the differences between two arrays.
    /// This mode prioritizes performance over detailed granularity, making it suitable for large datasets or scenarios where speed is critical.
    /// </summary>
    Fast
}