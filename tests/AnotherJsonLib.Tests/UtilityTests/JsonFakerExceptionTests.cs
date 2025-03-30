using System.Text.Json;
using System.Text.Json.Nodes;
using AnotherJsonLib.Tests.Utility;

namespace AnotherJsonLib.Tests.UtilityTests;

public class JsonFakerExceptionTests
{
    [Fact]
    public void GenerateInvalidJson_CreatesUnparsableJson()
    {
        // Arrange
        var faker = new JsonFaker(42);

        // Test all invalid JSON types
        foreach (InvalidJsonType invalidType in Enum.GetValues(typeof(InvalidJsonType)))
        {
            // Act
            var invalidJson = faker.GenerateInvalidJson(invalidType);

            // Assert
            Assert.NotNull(invalidJson);
            Assert.ThrowsAny<JsonException>(() => JsonNode.Parse(invalidJson));
        }
    }
}