using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Transformation;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonPropertyTransformerTests
    {
        [Fact]
        public void TransformPropertyNames_CamelToPascal_ShouldConvertKeys()
        {
            // Arrange: a JSON with camelCase keys.
            string inputJson = "{\"firstName\":\"John\",\"lastName\":\"Doe\",\"age\":30}";
            // Transformation: convert first letter to uppercase.
            string expectedJson = "{\"FirstName\":\"John\",\"LastName\":\"Doe\",\"Age\":30}";

            // Act
            string result = JsonPropertyTransformer.TransformPropertyNames(inputJson, 
                propertyName => char.ToUpper(propertyName[0]) + propertyName.Substring(1));

            // Assert: the output should match the expected JSON.
            result.ShouldBe(expectedJson);
        }

        [Fact]
        public void TransformPropertyNames_RemoveSpecificProperties_ShouldOmitKeys()
        {
            // Arrange: JSON with keys "secret" and "internalInfo" that we want removed.
            string inputJson = "{\"id\":123,\"name\":\"Product\",\"secret\":\"confidential\",\"internalInfo\":\"hidden\"}";
            // Transformation: return empty string for keys "secret" or starting with "internal" so they are omitted.
            // Expected: the resulting JSON does not include those properties.
            string expectedJson = "{\"id\":123,\"name\":\"Product\"}";

            // Act
            string result = JsonPropertyTransformer.TransformPropertyNames(inputJson, 
                propertyName => (propertyName == "secret" || propertyName.StartsWith("internal"))
                                ? "" 
                                : propertyName);

            // Assert
            result.ShouldBe(expectedJson);
        }

        [Fact]
        public void TryTransformPropertyNames_ValidInput_ReturnsTrueAndTransformedJson()
        {
            // Arrange
            string inputJson = "{\"firstName\":\"Jane\",\"lastName\":\"Doe\"}";
            // Transformation: convert keys to uppercase.
            string expectedJson = "{\"FIRSTNAME\":\"Jane\",\"LASTNAME\":\"Doe\"}";
            
            // Act
            bool success = JsonPropertyTransformer.TryTransformPropertyNames(inputJson, 
                name => name.ToUpperInvariant(), out string result);
            
            // Assert
            success.ShouldBeTrue();
            result.ShouldBe(expectedJson);
        }

        [Fact]
        public void TransformPropertyNames_InvalidJson_ShouldThrowJsonParsingException()
        {
            // Arrange: invalid JSON (missing closing brace)
            string invalidJson = "{\"name\":\"John\"";
            
            // Act & Assert
            Should.Throw<JsonParsingException>(() => 
                JsonPropertyTransformer.TransformPropertyNames(invalidJson, name => name));
        }

        [Fact]
        public void TransformPropertyNames_NullInput_ShouldThrowArgumentNullException()
        {
            // Act & Assert: Passing null for json should throw.
            Should.Throw<JsonTransformationException>(() => 
                JsonPropertyTransformer.TransformPropertyNames(null!, name => name));

            // Also, passing null for transform function should throw.
            string validJson = "{\"key\":\"value\"}";
            Should.Throw<JsonTransformationException>(() => 
                JsonPropertyTransformer.TransformPropertyNames(validJson, null!));
        }
    }