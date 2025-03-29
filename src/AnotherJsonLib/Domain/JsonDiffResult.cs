namespace AnotherJsonLib.Domain;

/// <summary>
/// Represents the result of a JSON comparison, identifying added, removed, and modified values between two JSON structures.
/// </summary>
public class JsonDiffResult
{
    /// <summary>
    /// Gets or sets the dictionary containing the keys and corresponding values that were added
    /// when comparing two JSON structures.
    /// </summary>
    /// <remarks>
    /// The <c>Added</c> property includes key-value pairs that exist in the second JSON
    /// structure but are absent in the first one. The property is populated during the
    /// computation of a JSON difference using the <c>JsonDiffer</c>.
    /// </remarks>
    public Dictionary<string, object> Added { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the dictionary containing the keys and corresponding values that were removed
    /// when comparing two JSON structures.
    /// </summary>
    /// <remarks>
    /// The <c>Removed</c> property includes key-value pairs that exist in the first JSON
    /// structure but are absent in the second one. The property is populated during the
    /// computation of a JSON difference using the <c>JsonDiffer</c>.
    /// </remarks>
    public Dictionary<string, object> Removed { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the dictionary containing the keys and corresponding difference entries for values
    /// that were modified when comparing two JSON structures.
    /// </summary>
    /// <remarks>
    /// The <c>Modified</c> property includes keys whose associated values differ between the two JSON structures.
    /// Each entry in the dictionary provides details about the changes, including the old value, the new value,
    /// and any nested differences, represented by the <c>DiffEntry</c> type.
    /// This property is populated during the JSON comparison process performed by the <c>JsonDiffer</c>.
    /// </remarks>
    public Dictionary<string, DiffEntry> Modified { get; set; } = new Dictionary<string, DiffEntry>();
}