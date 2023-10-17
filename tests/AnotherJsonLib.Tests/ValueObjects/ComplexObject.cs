using System.Text.Json.Serialization;

namespace AnotherJsonLib.Tests.ValueObjects;

public class ComplexObject
{
    [JsonConstructor]
    public ComplexObject()
    {
        
    }
    public ComplexObject(Guid id, string? name, int age, string? address)
    {
        Id = id;
        Name = name;
        Age = age;
        Address = address;
    }

    public Guid Id { get; set; }
    public string? Name { get; set; }
    public int Age { get; set; }
    public string? Address { get; set; }
}