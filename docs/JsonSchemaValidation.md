# AnotherJsonLib.Utility.Schema
## Overview
The `AnotherJsonLib.Utility.Schema` namespace provides functionality for JSON schema validation. It contains classes designed to validate JSON data against JSON Schema specifications.
## Classes
### JsonSchemaValidator
This is the main class in this namespace that provides JSON schema validation capabilities.
#### Methods
##### Validate(JsonElement schema, JsonElement instance)
Validates a JSON instance against a JSON schema.
**Parameters:**
- `schema` (JsonElement): The JSON schema document to validate against.
- `instance` (JsonElement): The JSON instance to validate.

**Returns:**
- `JsonSchemaValidationResult`: A result object containing validation outcomes.

#### Usage Examples
``` csharp
// Example 1: Basic schema validation
using AnotherJsonLib.Utility.Schema;
using System.Text.Json;

// Parse JSON schema and instance
JsonDocument schemaDoc = JsonDocument.Parse(@"
{
    ""type"": ""object"",
    ""properties"": {
        ""name"": { ""type"": ""string"" },
        ""age"": { ""type"": ""number"", ""minimum"": 0 }
    },
    ""required"": [""name"", ""age""]
}");

JsonDocument instanceDoc = JsonDocument.Parse(@"
{
    ""name"": ""John Doe"",
    ""age"": 30
}");

// Validate instance against schema
var result = JsonSchemaValidator.Validate(schemaDoc.RootElement, instanceDoc.RootElement);

// Check validation result
if (result.IsValid)
{
    Console.WriteLine("Validation successful!");
}
else
{
    Console.WriteLine($"Validation failed: {string.Join(", ", result.Errors)}");
}
```
## Related Classes
### JsonSchemaValidationResult (in AnotherJsonLib.Domain namespace)
This class holds the results of a schema validation operation.
#### Properties
- `IsValid` (bool): Indicates whether the validation passed or failed.
- `Errors` (List ): Contains error messages if validation failed.

## Common Edge Cases and Solutions
### 1. Schema Reference Resolution
**Potential Issue:** When using `$ref` in your JSON schema, the validator needs to resolve these references.
**Solution Example:**
string
string
``` csharp
// You may need to ensure your schema has all referenced schemas available
// or provide a custom schema resolver if the library supports it.
```
### 2. Complex Schema Validation
**Potential Issue:** Complex schemas with nested conditions might be difficult to debug.
**Solution Example:**
``` csharp
// Break down complex validations into smaller test cases
var basicResult = JsonSchemaValidator.Validate(basicSchema, instance);
var advancedResult = JsonSchemaValidator.Validate(advancedSchema, instance);
```
### 3. Large JSON Documents
**Potential Issue:** Performance may degrade with very large JSON documents.
**Solution Example:**
``` csharp
// Consider validating only critical parts of large documents
// or implement pagination/chunking for large datasets
```
### 4. Custom Format Validation
**Potential Issue:** Standard JSON Schema might not support all custom formats you need.
**Solution Example:**
``` csharp
// Perform additional validation after schema validation if needed
var result = JsonSchemaValidator.Validate(schema, instance);
if (result.IsValid) 
{
    // Perform additional custom validations
}
```
## Best Practices
1. **Pre-validate schemas**: Ensure your schema itself is valid before using it for validation.
2. **Cache parsed schemas**: For frequently used schemas, parse them once and reuse.
3. **Use specific error messages**: Be specific when reporting validation errors to users.
4. **Consider performance**: For large documents or high-frequency validation, profile performance.

This documentation provides a starting point for working with the `AnotherJsonLib.Utility.Schema` namespace. For more specific use cases or advanced functionality, refer to the library's full API documentation or unit tests.
