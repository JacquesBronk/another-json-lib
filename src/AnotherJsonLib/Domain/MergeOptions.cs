namespace AnotherJsonLib.Domain;

/// <summary>
/// Options for controlling JSON merge behavior.
/// </summary>
public class MergeOptions
{
    /// <summary>
    /// Gets or sets the strategy for merging arrays.
    /// </summary>
    public ArrayMergeStrategy ArrayMergeStrategy { get; set; } = ArrayMergeStrategy.Concat;
    
    /// <summary>
    /// Gets or sets whether null values in the patch should override non-null values in the source.
    /// </summary>
    public bool NullOverridesValue { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to preserve formatting (indentation) when merging JSON.
    /// </summary>
    public bool PreserveFormatting { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether to enable deep merging of arrays when the strategy is Merge.
    /// </summary>
    public bool EnableDeepArrayMerge { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether to remove properties that don't exist in the patch.
    /// If true, only properties present in the patch document will be included.
    /// </summary>
    public bool RemoveUnmatchedProperties { get; set; } = false;
}