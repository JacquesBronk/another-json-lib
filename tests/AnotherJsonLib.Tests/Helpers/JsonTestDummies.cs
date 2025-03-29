using System.Text.Json;
using AnotherJsonLib.Helper;
using AnotherJsonLib.Tests.ValueObjects;
using Bogus;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Tests.Helpers;

public static class JsonTestDummies
{
    private static readonly Randomizer Randomizer = new Randomizer();

    public static SimpleObject CreateSimpleObject()
    {
        return new Faker<SimpleObject>()
            .RuleFor(o => o.Id, f => f.Random.Guid())
            .RuleFor(o => o.Name, f => f.Person.FirstName)
            .Generate();
    }

    public static ComplexObject CreateComplexObject()
    {
        return new Faker<ComplexObject>()
            .RuleFor(o => o.Id, f => f.Random.Guid())
            .RuleFor(o => o.Name, f => f.Person.FullName)
            .RuleFor(o => o.Age, f => f.Random.Int(18, 60))
            .RuleFor(o => o.Address, f => f.Address.FullAddress())
            .Generate();
    }

    public static LargeObject CreateLargeObject()
    {
        return new Faker<LargeObject>()
            .RuleFor(o => o.Id, f => f.Random.Guid())
            .RuleFor(o => o.Description, f => f.Lorem.Paragraphs(Randomizer.Int(3, 5)))
            .Generate();
    }
    
    public static void CreateLargeJsonFile(string filePath, int numberOfLargeObjects)
    {
        using var fileStream = File.Create(filePath);
        using var writer = new Utf8JsonWriter(fileStream);

        writer.WriteStartArray();

        for (int i = 0; i < numberOfLargeObjects; i++)
        {
            var largeObject = CreateLargeObject();
            JsonSerializer.Serialize(writer, largeObject);
        }

        writer.WriteEndArray();
        writer.Flush();
    }

    public static void DeleteJsonFile(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            // Handle any exceptions here
           var logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonTestDummies));
           logger.LogError(ex, "Error deleting file");
        }
    }
}