using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Transformation;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonValueTransformerTests
    {
        [Fact]
        public void TransformStringValues_ToUppercase_ShouldConvertAllStringValues()
        {
            // Arrange
            string json = "{\"greeting\":\"hello\",\"data\": {\"note\":\"welcome\"}, \"number\": 42}";
            
            // Act: Transform string values to uppercase.
            string result = JsonValueTransformer.TransformStringValues(json, s => s.ToUpperInvariant());
            
            // Assert: All string values should be uppercase.
            result.ShouldContain("\"greeting\":\"HELLO\"");
            result.ShouldContain("\"note\":\"WELCOME\"");
            // Non-string values (like 42) should remain unchanged.
            result.ShouldContain("\"number\":42");
        }

        [Fact]
        public void TryTransformStringValues_ValidInput_ShouldReturnTrueAndResult()
        {
            // Arrange
            string json = "{\"text\":\"sample\"}";
            
            // Act
            bool success = JsonValueTransformer.TryTransformStringValues(json, s => s.ToUpperInvariant(), out string result);
            
            // Assert
            success.ShouldBeTrue();
            result.ShouldContain("\"text\":\"SAMPLE\"");
        }

        [Fact]
        public void TransformStringValues_NonStringValues_Unchanged()
        {
            // Arrange
            string json = "{\"num\":123, \"bool\":true, \"nullVal\":null}";
            
            // Act
            string result = JsonValueTransformer.TransformStringValues(json, s => s.ToUpperInvariant());
            
            // Assert: Non-string values should remain unchanged.
            result.ShouldContain("\"num\":123");
            result.ShouldContain("\"bool\":true");
            result.ShouldContain("\"nullVal\":null");
        }

        [Fact]
        public void TransformStringValues_InvalidJson_ShouldThrowJsonParsingException()
        {
            // Arrange: invalid JSON string.
            string invalidJson = "{\"text\": \"sample\""; // missing closing brace
            
            // Act & Assert
            Should.Throw<JsonParsingException>(() => 
                JsonValueTransformer.TransformStringValues(invalidJson, s => s.ToUpperInvariant()));
        }
    }