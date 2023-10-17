using System.Text.Json.Serialization;

namespace AnotherJsonLib.Tests.ValueObjects;

public class LargeObject
{
    [JsonConstructor]
    public LargeObject()
    {
        
    }
    public LargeObject(string? description, Guid id)
    {
        Description = description;
        Id = id;
    }

    public Guid Id { get; set; }
    public string? Description { get; set; }
}