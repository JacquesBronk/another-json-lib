namespace AnotherJsonLib.Domain;

/// <summary>
/// Options to control the behavior of the JSON patch generator.
/// </summary>
public class PatchGeneratorOptions
{
    /// <summary>
    /// Gets or sets whether to optimize the generated patch operations.
    /// </summary>
    public bool OptimizePatch { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to use the array diffing algorithm for arrays.
    /// If false, arrays are simply replaced entirely.
    /// </summary>
    public bool UseArrayDiffAlgorithm { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to ignore property removals in objects.
    /// </summary>
    public bool IgnoreRemovals { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether to format the output JSON with indentation.
    /// </summary>
    public bool FormatOutput { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the maximum array size for which to use the LCS algorithm.
    /// Larger arrays use a simpler but less optimal algorithm.
    /// </summary>
    public int MaxArraySizeForLcs { get; set; } = 100;
    
    /// <summary>
    /// Gets or sets whether to use positional array patching for arrays of the same length.
    /// </summary>
    public bool UsePositionalArrayPatching { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to perform deep comparison of objects.
    /// </summary>
    public bool DeepCompareObjects { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether to perform deep comparison of arrays.
    /// </summary>
    public bool DeepCompareArrays { get; set; } = false;
}
