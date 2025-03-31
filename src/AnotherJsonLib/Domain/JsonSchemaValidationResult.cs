namespace AnotherJsonLib.Domain;

/// <summary>
/// Represents the result of a JSON Schema validation.
/// </summary>
public class JsonSchemaValidationResult
{
    /// <summary>
    /// Indicates whether the JSON instance is valid against the schema.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// A list of error messages describing validation failures.
    /// </summary>
    public List<string> Errors { get; } = new List<string>();
}