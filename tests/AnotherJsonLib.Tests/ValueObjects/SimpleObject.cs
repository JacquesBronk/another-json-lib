using System.Text.Json.Serialization;

namespace AnotherJsonLib.Tests.ValueObjects;


public class SimpleObject
{
    [JsonConstructor]
    public SimpleObject()
    {
        
    }
    
    public SimpleObject(Guid id, string? name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; set; }
    public string? Name { get; set; }
}
