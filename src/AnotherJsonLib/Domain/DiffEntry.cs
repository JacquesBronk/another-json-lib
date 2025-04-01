namespace AnotherJsonLib.Domain;

/// <summary>
/// Represents a difference entry in a JSON diff.
/// </summary>
public class DiffEntry
{
    /// <summary>
    /// Gets or sets the previous value of the JSON property that was modified.
    /// </summary>
    /// <remarks>
    /// This property holds the original value before the modification,
    /// typically used as part of a diff comparison result to represent changes between
    /// two JSON objects. It is used in conjunction with the <c>NewValue</c> property
    /// to identify what changed.
    /// </remarks>
    /// <value>
    /// The original value of the property, represented as an <c>object</c>.
    /// </value>
    public required object OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new value of the JSON property after it has been modified.
    /// </summary>
    /// <remarks>
    /// This property contains the updated value following a change in the JSON object.
    /// It is typically used alongside the <c>OldValue</c> property to represent the difference
    /// between two versions of a JSON object. This property helps identify what the new state is
    /// after the modification.
    /// </remarks>
    /// <value>
    /// The updated value of the property, represented as an <c>object</c>.
    /// </value>
    public required object NewValue { get; set; }

    /// <summary>
    /// Gets or sets the nested differences within a JSON object or structure.
    /// </summary>
    /// <remarks>
    /// This property represents a comprehensive collection of nested differences
    /// found during the JSON comparison process. It typically captures changes within
    /// nested objects or arrays and provides detailed information about additions,
    /// removals, and modifications at deeper levels of the JSON structure. It is most
    /// commonly used in conjunction with high-level diff properties such as
    /// <c>OldValue</c> and <c>NewValue</c>.
    /// </remarks>
    /// <value>
    /// An instance of <c>JsonDiffResult</c> containing the structured details of
    /// nested changes.
    /// </value>
    public JsonDiffResult? NestedDiff { get; set; }
}