using System.Text.Json;
using AnotherJsonLib.Utility.Transformation;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonTransformerTests
{
    [Fact]
    public void Transform_IdentityFunction_ShouldReturnEquivalentObject()
    {
        // Arrange
        string json = "{\"a\":1,\"b\":\"text\",\"c\":true}";
        using var document = JsonDocument.Parse(json);

        // Act: use identity transform.
        object? transformed = JsonTransformer.Transform(document.RootElement, value => value);
        string resultJson = JsonSerializer.Serialize(transformed);

        // Assert: result should be equivalent to original JSON (ignoring whitespace)
        resultJson.ShouldBe(JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(json)));
    }

    [Fact]
    public void Transform_UppercaseStrings_ShouldConvertAllStringValues()
    {
        // Arrange
        string json = "{\"name\":\"john\",\"details\":{\"city\":\"london\",\"info\":123}}";
        using var document = JsonDocument.Parse(json);
        // Transform: if a value is a string, convert to uppercase.
        object? transformed = JsonTransformer.Transform(document.RootElement, value =>
        {
            if (value is string s)
                return s.ToUpperInvariant();
            return value;
        });
        string resultJson = JsonSerializer.Serialize(transformed);

        // Assert: strings should be uppercase; non-strings unchanged.
        resultJson.ShouldContain("\"NAME\":\"JOHN\"");
        resultJson.ShouldContain("\"CITY\":\"LONDON\"");
        resultJson.ShouldContain("\"info\":123");
    }

    [Fact]
    public void TransformWithPath_MaskSensitiveData_ShouldMaskValuesAtSensitivePath()
    {
        // Arrange
        string json = "{\"public\":\"visible\",\"sensitive\":{\"ssn\":\"123-45-6789\",\"pin\":\"9876\"}}";
        using var document = JsonDocument.Parse(json);
        // Transformation: mask values in any path starting with "/sensitive/"
        object? transformed = JsonTransformer.TransformWithPath(document.RootElement, (path, value) =>
        {
            if (path.StartsWith("/sensitive/") && value is string)
                return "********";
            return value;
        });
        string resultJson = JsonSerializer.Serialize(transformed);

        // Assert: sensitive values are masked.
        resultJson.ShouldContain("\"ssn\":\"********\"");
        resultJson.ShouldContain("\"pin\":\"********\"");
        resultJson.ShouldContain("\"public\":\"visible\"");
    }

    [Fact]
    public void TryTransform_ValidTransform_ShouldReturnTrueAndResult()
    {
        // Arrange
        string json = "{\"key\":\"value\"}";
        using var document = JsonDocument.Parse(json);

        bool success = JsonTransformer.TryTransform(document.RootElement, value =>
        {
            if (value is string s)
                return s.ToUpperInvariant();
            return value;
        }, out object? result);

        // Assert
        success.ShouldBeTrue();
        // The transformation should have converted "value" to "VALUE"
        string serialized = JsonSerializer.Serialize(result);
        serialized.ShouldContain("\"key\":\"VALUE\"");
    }

    [Fact]
    public void TryTransformJson_InvalidJson_ShouldReturnFalse()
    {
        // Arrange: invalid JSON string.
        string invalidJson = "{\"key\": \"value\""; // missing closing brace

        bool success = JsonTransformer.TryTransformJson(invalidJson, value => value, out string result);

        // Assert: transformation should fail.
        success.ShouldBeFalse();
        result.ShouldBeEmpty();
    }
}