using System.Text.Json;
using AnotherJsonLib.Utility.Schema;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonSchemaValidatorTests
{
    [Fact]
    public void Validate_TypeValidation_String_ShouldPass()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{""type"": ""string""}").RootElement;
        var instance = JsonDocument.Parse(@"""test string""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_TypeValidation_String_ShouldFail()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{""type"": ""string""}").RootElement;
        var instance = JsonDocument.Parse(@"42").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldContain("Type mismatch");
    }

    [Fact]
    public void Validate_TypeValidation_Number_ShouldPass()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{""type"": ""number""}").RootElement;
        var instance = JsonDocument.Parse(@"42.5").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_TypeValidation_Integer_ShouldPass()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{""type"": ""integer""}").RootElement;
        var instance = JsonDocument.Parse(@"42").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_TypeValidation_Integer_ShouldFail_ForDecimal()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{""type"": ""integer""}").RootElement;
        var instance = JsonDocument.Parse(@"42.5").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_TypeValidation_Boolean_ShouldPass()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{""type"": ""boolean""}").RootElement;
        var instance = JsonDocument.Parse(@"true").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_TypeValidation_Null_ShouldPass()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{""type"": ""null""}").RootElement;
        var instance = JsonDocument.Parse(@"null").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }


    [Fact]
    public void Validate_Enum_ShouldPass_WhenValueInEnum()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{""enum"": [""red"", ""green"", ""blue""]}").RootElement;
        var instance = JsonDocument.Parse(@"""green""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Enum_ShouldFail_WhenValueNotInEnum()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{""enum"": [""red"", ""green"", ""blue""]}").RootElement;
        var instance = JsonDocument.Parse(@"""yellow""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldContain("not in the allowed enum list");
    }


    [Fact]
    public void Validate_RequiredProperties_ShouldPass_WhenAllPresent()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""object"",
                ""required"": [""name"", ""email""],
                ""properties"": {
                    ""name"": { ""type"": ""string"" },
                    ""email"": { ""type"": ""string"" }
                }
            }").RootElement;

        var instance = JsonDocument.Parse(@"{
                ""name"": ""John Doe"",
                ""email"": ""john@example.com""
            }").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_RequiredProperties_ShouldFail_WhenMissing()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""object"",
                ""required"": [""name"", ""email""],
                ""properties"": {
                    ""name"": { ""type"": ""string"" },
                    ""email"": { ""type"": ""string"" }
                }
            }").RootElement;

        var instance = JsonDocument.Parse(@"{
                ""name"": ""John Doe""
            }").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldContain("Missing required property 'email'");
    }

    [Fact]
    public void Validate_Properties_ShouldFail_WhenTypeIncorrect()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""age"": { ""type"": ""integer"" }
                }
            }").RootElement;

        var instance = JsonDocument.Parse(@"{
                ""age"": ""twenty-five""
            }").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("Type mismatch");
    }

    [Fact]
    public void Validate_AdditionalProperties_False_ShouldFail_WhenExtraPropertiesPresent()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" }
                },
                ""additionalProperties"": false
            }").RootElement;

        var instance = JsonDocument.Parse(@"{
                ""name"": ""John"",
                ""age"": 30
            }").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldContain("Additional property 'age' not allowed");
    }

    [Fact]
    public void Validate_AdditionalProperties_WithSchema_ShouldPass_WhenConforming()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" }
                },
                ""additionalProperties"": {
                    ""type"": ""integer""
                }
            }").RootElement;

        var instance = JsonDocument.Parse(@"{
                ""name"": ""John"",
                ""age"": 30,
                ""score"": 85
            }").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_AdditionalProperties_WithSchema_ShouldFail_WhenNotConforming()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" }
                },
                ""additionalProperties"": {
                    ""type"": ""integer""
                }
            }").RootElement;

        var instance = JsonDocument.Parse(@"{
                ""name"": ""John"",
                ""age"": ""thirty""
            }").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
    }


    [Fact]
    public void Validate_MinItems_ShouldPass_WhenEnoughItems()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""array"",
                ""minItems"": 2
            }").RootElement;

        var instance = JsonDocument.Parse(@"[1, 2, 3]").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MinItems_ShouldFail_WhenNotEnoughItems()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""array"",
                ""minItems"": 2
            }").RootElement;

        var instance = JsonDocument.Parse(@"[1]").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("minimum is 2");
    }

    [Fact]
    public void Validate_MaxItems_ShouldFail_WhenTooManyItems()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""array"",
                ""maxItems"": 2
            }").RootElement;

        var instance = JsonDocument.Parse(@"[1, 2, 3]").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("maximum is 2");
    }

    [Fact]
    public void Validate_UniqueItems_ShouldPass_WhenAllItemsUnique()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""array"",
                ""uniqueItems"": true
            }").RootElement;

        var instance = JsonDocument.Parse(@"[1, 2, 3]").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_UniqueItems_ShouldFail_WhenDuplicatesExist()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""array"",
                ""uniqueItems"": true
            }").RootElement;

        var instance = JsonDocument.Parse(@"[1, 2, 2, 3]").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("duplicate items");
    }

    [Fact]
    public void Validate_Items_ShouldValidateEachItem()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""array"",
                ""items"": { ""type"": ""integer"" }
            }").RootElement;

        var instance = JsonDocument.Parse(@"[1, 2, 3]").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Items_ShouldFail_WhenItemsDoNotConform()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""array"",
                ""items"": { ""type"": ""integer"" }
            }").RootElement;

        var instance = JsonDocument.Parse(@"[1, ""two"", 3]").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("Type mismatch");
    }


    [Fact]
    public void Validate_MinLength_ShouldPass_WhenStringLongEnough()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""string"",
                ""minLength"": 3
            }").RootElement;

        var instance = JsonDocument.Parse(@"""test""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MinLength_ShouldFail_WhenStringTooShort()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""string"",
                ""minLength"": 3
            }").RootElement;

        var instance = JsonDocument.Parse(@"""hi""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("less than minimum");
    }

    [Fact]
    public void Validate_MaxLength_ShouldFail_WhenStringTooLong()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""string"",
                ""maxLength"": 5
            }").RootElement;

        var instance = JsonDocument.Parse(@"""too long""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("exceeds maximum");
    }

    [Fact]
    public void Validate_Pattern_ShouldPass_WhenMatchingPattern()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""string"",
                ""pattern"": ""^[A-Z][a-z]+$""
            }").RootElement;

        var instance = JsonDocument.Parse(@"""Hello""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Pattern_ShouldFail_WhenNotMatchingPattern()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""string"",
                ""pattern"": ""^[A-Z][a-z]+$""
            }").RootElement;

        var instance = JsonDocument.Parse(@"""hello""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("does not match pattern");
    }

    [Fact]
    public void Validate_Format_Email_ShouldPass_WhenValidEmail()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""string"",
                ""format"": ""email""
            }").RootElement;

        var instance = JsonDocument.Parse(@"""test@example.com""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Format_Email_ShouldFail_WhenInvalidEmail()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""string"",
                ""format"": ""email""
            }").RootElement;

        var instance = JsonDocument.Parse(@"""not-an-email""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("not a valid email");
    }

    [Fact]
    public void Validate_Format_DateTime_ShouldValidateDateTimeFormat()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""string"",
                ""format"": ""date-time""
            }").RootElement;

        var instance = JsonDocument.Parse(@"""2023-01-15T14:30:00Z""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Format_Uri_ShouldValidateUriFormat()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""string"",
                ""format"": ""uri""
            }").RootElement;

        var instance = JsonDocument.Parse(@"""https://example.com/path""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Minimum_ShouldPass_WhenNumberHighEnough()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""number"",
                ""minimum"": 5
            }").RootElement;

        var instance = JsonDocument.Parse(@"10").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Minimum_ShouldFail_WhenNumberTooSmall()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""number"",
                ""minimum"": 5
            }").RootElement;

        var instance = JsonDocument.Parse(@"3").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("less than minimum");
    }

    [Fact]
    public void Validate_Maximum_ShouldFail_WhenNumberTooLarge()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""number"",
                ""maximum"": 10
            }").RootElement;

        var instance = JsonDocument.Parse(@"15").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("exceeds maximum");
    }

    [Fact]
    public void Validate_MultipleOf_ShouldPass_WhenDivisible()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""number"",
                ""multipleOf"": 2
            }").RootElement;

        var instance = JsonDocument.Parse(@"10").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MultipleOf_ShouldFail_WhenNotDivisible()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""number"",
                ""multipleOf"": 2
            }").RootElement;

        var instance = JsonDocument.Parse(@"11").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("not a multiple of");
    }

    [Fact]
    public void Validate_MultipleOf_ShouldFail_WhenZero()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""number"",
                ""multipleOf"": 0
            }").RootElement;

        var instance = JsonDocument.Parse(@"10").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("multipleOf 0, which is invalid");
    }

    [Fact]
    public void Validate_AllOf_ShouldPass_WhenMatchingAllSchemas()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""allOf"": [
                    { ""type"": ""string"" },
                    { ""minLength"": 3 }
                ]
            }").RootElement;

        var instance = JsonDocument.Parse(@"""test""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_AllOf_ShouldFail_WhenNotMatchingOneSchema()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""allOf"": [
                    { ""type"": ""string"" },
                    { ""minLength"": 5 }
                ]
            }").RootElement;

        var instance = JsonDocument.Parse(@"""test""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("allOf failure");
    }

    [Fact]
    public void Validate_AnyOf_ShouldPass_WhenMatchingOneSchema()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""anyOf"": [
                    { ""type"": ""string"" },
                    { ""type"": ""number"" }
                ]
            }").RootElement;

        var instance = JsonDocument.Parse(@"""test""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_AnyOf_ShouldFail_WhenMatchingNoSchemas()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""anyOf"": [
                    { ""type"": ""number"" },
                    { ""type"": ""boolean"" }
                ]
            }").RootElement;

        var instance = JsonDocument.Parse(@"""test""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("anyOf failure");
    }

    [Fact]
    public void Validate_OneOf_ShouldPass_WhenMatchingExactlyOneSchema()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""oneOf"": [
                    { ""type"": ""string"" },
                    { ""type"": ""number"" }
                ]
            }").RootElement;

        var instance = JsonDocument.Parse(@"""test""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_OneOf_ShouldFail_WhenMatchingMultipleSchemas()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""oneOf"": [
                    { ""type"": ""string"" },
                    { ""minLength"": 2 }
                ]
            }").RootElement;

        var instance = JsonDocument.Parse(@"""test""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("oneOf failure");
        result.Errors[0].ShouldContain("Expected exactly one valid subschema");
    }

    [Fact]
    public void Validate_Not_ShouldPass_WhenNotMatchingSchema()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""not"": { ""type"": ""number"" }
            }").RootElement;

        var instance = JsonDocument.Parse(@"""test""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Not_ShouldFail_WhenMatchingSchema()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""not"": { ""type"": ""string"" }
            }").RootElement;

        var instance = JsonDocument.Parse(@"""test""").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ShouldContain("not failure");
    }


    [Fact]
    public void Validate_ComplexSchema_ShouldValidateNestedStructures()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{
                ""type"": ""object"",
                ""required"": [""name"", ""address"", ""phoneNumbers""],
                ""properties"": {
                    ""name"": { ""type"": ""string"" },
                    ""age"": { ""type"": ""integer"", ""minimum"": 0 },
                    ""address"": {
                        ""type"": ""object"",
                        ""required"": [""street"", ""city""],
                        ""properties"": {
                            ""street"": { ""type"": ""string"" },
                            ""city"": { ""type"": ""string"" },
                            ""zipCode"": { ""type"": ""string"", ""pattern"": ""^\\d{5}$"" }
                        }
                    },
                    ""phoneNumbers"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""object"",
                            ""required"": [""type"", ""number""],
                            ""properties"": {
                                ""type"": { ""enum"": [""home"", ""work"", ""mobile""] },
                                ""number"": { ""type"": ""string"" }
                            }
                        }
                    }
                }
            }").RootElement;

        var instance = JsonDocument.Parse(@"{
                ""name"": ""John Doe"",
                ""age"": 30,
                ""address"": {
                    ""street"": ""123 Main St"",
                    ""city"": ""Anytown"",
                    ""zipCode"": ""12345""
                },
                ""phoneNumbers"": [
                    { ""type"": ""home"", ""number"": ""555-1234"" },
                    { ""type"": ""mobile"", ""number"": ""555-5678"" }
                ]
            }").RootElement;

        // Act
        var result = JsonSchemaValidator.Validate(schema, instance);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}