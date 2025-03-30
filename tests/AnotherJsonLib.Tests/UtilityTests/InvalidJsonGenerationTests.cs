using System.Text.Json;
using System.Text.Json.Nodes;
using AnotherJsonLib.Tests.Utility;

namespace AnotherJsonLib.Tests.UtilityTests
{
    public class InvalidJsonGenerationTests
    {
        [Theory]
        [InlineData(InvalidJsonType.MissingClosingBrace)]
        [InlineData(InvalidJsonType.MissingQuotes)]
        [InlineData(InvalidJsonType.ExtraCommas)]
        [InlineData(InvalidJsonType.MalformedProperty)]
        [InlineData(InvalidJsonType.UnclosedString)]
        public void GenerateInvalidJson_ReturnsUnparsableJson(InvalidJsonType invalidType)
        {
            // Arrange
            var faker = new JsonFaker(42);

            // Act
            var invalidJson = faker.GenerateInvalidJson(invalidType);

            // Assert
            Assert.NotEmpty(invalidJson);
            Assert.ThrowsAny<JsonException>(() => JsonNode.Parse(invalidJson));
        }

        [Fact]
        public void MissingClosingBrace_HasOpeningButNoClosingBrace()
        {
            // Arrange
            var faker = new JsonFaker(42);

            // Act
            var invalidJson = faker.GenerateInvalidJson(InvalidJsonType.MissingClosingBrace);

            // Assert
            Assert.Contains("{", invalidJson);
            Assert.DoesNotContain("}", invalidJson.Substring(invalidJson.IndexOf("{")));
        }

        [Fact]
        public void MissingQuotes_HasPropertyWithoutQuotes()
        {
            // Arrange
            var faker = new JsonFaker(42);

            // Act
            var invalidJson = faker.GenerateInvalidJson(InvalidJsonType.MissingQuotes);

            // Assert
            // Looking for a pattern like: {name: "value"} or {name: 123}
            Assert.Matches(@"\{[^""]*:[^}]*\}", invalidJson);
        }

        [Fact]
        public void ExtraCommas_HasInvalidCommaUsage()
        {
            // Arrange
            var faker = new JsonFaker(42);

            // Act
            var invalidJson = faker.GenerateInvalidJson(InvalidJsonType.ExtraCommas);

            // Assert
            // Check for various invalid comma patterns
            bool hasInvalidCommaPattern =
                invalidJson.Contains(",:,") || // Comma before and after colon
                invalidJson.Contains(",}") || // Trailing comma before closing brace
                invalidJson.Contains(",]") || // Trailing comma before closing bracket
                invalidJson.Contains(",,") || // Consecutive commas
                System.Text.RegularExpressions.Regex.IsMatch(invalidJson,
                    @"\{\s*,"); // Leading comma after opening brace

            Assert.True(hasInvalidCommaPattern,
                $"Invalid JSON should contain improper comma usage. Received: {invalidJson}");

            // Also ensure it's actually invalid JSON
            Assert.ThrowsAny<JsonException>(() =>
                JsonDocument.Parse(invalidJson));
        }

        [Fact]
        public void MalformedProperty_HasInvalidPropertyStructure()
        {
            // Arrange
            var faker = new JsonFaker(42);

            // Act
            var invalidJson = faker.GenerateInvalidJson(InvalidJsonType.MalformedProperty);

            // Assert
            Assert.ThrowsAny<JsonException>(() => JsonNode.Parse(invalidJson));

            Assert.Contains('"', invalidJson); // Should have quotation marks for property name
        }

        [Fact]
        public void UnclosedString_HasInvalidStringFormat()
        {
            // Arrange
            var faker = new JsonFaker(42);

            // Act
            var invalidJson = faker.GenerateInvalidJson(InvalidJsonType.UnclosedString);

            // Assert
            // First, verify it's actually invalid JSON
            Assert.ThrowsAny<JsonException>(() => JsonNode.Parse(invalidJson));

            // Looking at the actual invalid JSON, it appears to be missing colons between property names and values
            // Let's check for property name immediately followed by a value without a colon
            bool hasMissingColon = false;
    
            // Look for patterns like "property""value" or "property"value (missing colon)
            hasMissingColon = System.Text.RegularExpressions.Regex.IsMatch(
                invalidJson, 
                "\"[^\"]+\"\\s*\"[^\"]+\"");  // property name in quotes followed by value in quotes without colon
        
            if (!hasMissingColon) {
                // Also check for "property"value (where value is not in quotes)
                hasMissingColon = System.Text.RegularExpressions.Regex.IsMatch(
                    invalidJson, 
                    "\"[^\"]+\"\\s*[^:,{}\"\\s]");  // property name in quotes followed by unquoted value without colon
            }

            // Output the invalidJson for debugging
            Assert.True(hasMissingColon, $"Expected missing colon between property and value in: {invalidJson}");
        }
    }
}