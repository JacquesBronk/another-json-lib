using System.Text.Json;

namespace AnotherJsonLib.Domain;

/// <summary>
/// Represents a single JSON Patch operation.
/// </summary>
public class JsonPatchOperation
{
    /// <summary>
    /// The operation type ("add", "remove", "replace", "move").
    /// </summary>
    public required string Op { get; set; }
    /// <summary>
    /// The JSON Pointer path where the operation applies.
    /// </summary>
    public required string Path { get; set; }
    /// <summary>
    /// For "move" operations, the source location.
    /// </summary>
    public string? From { get; set; }
    /// <summary>
    /// The value to add or replace. Not used for "remove" or "move".
    /// </summary>
    public JsonElement? Value { get; set; }
}