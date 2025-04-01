# Another Json Library
[](https://github.com/JacquesBronk/another-json-lib/blob/main/LICENSE)[](https://github.com/JacquesBronk/another-json-lib/actions/workflows/build-test.yaml)[](https://gist.github.com/JacquesBronk/583f3a5e64e34c4125c923404dfa921f)[](https://github.com/JacquesBronk/another-json-lib/actions/workflows/github-code-scanning/codeql)
## Overview
**Another Json Library** is a powerful and comprehensive toolkit for JSON manipulation in C#/.NET applications. It goes beyond basic serialization and deserialization to provide advanced JSON operations like comparison, patching, path querying, and streaming large JSON files efficiently. Designed with performance and developer experience in mind, this library serves as a complete solution for working with JSON data in modern .NET applications.
## Documentation
- [JSON Operations](docs/JsonOperations.md) - Core JSON manipulation functionality including property mapping and transformation.
- [JSON Comparison](docs/JsonComparison.md) - Compare and identify differences between JSON documents.
- [JSON Element Utilities](docs/JsonElementUtilities.md) - Implementation of RFC 6901 JSON Pointer specification.
- [JSON Formatting](docs/JsonFormatting.md) - Format and query JSON documents using JSONPath expressions.
- [JSON Schema Validation](docs/JsonSchemaValidation.md) - Validate JSON documents against JSON Schema and apply patches following RFC 6902.
- [JSON Security](docs/JsonSecurity.md) - Secure JSON processing and handling of sensitive data.
- [JSON Serialization](docs/JsonSerialization.md) - Advanced serialization and deserialization capabilities.
- [JSON Compression](docs/JsonCompression.md) - Optimize JSON document size through compression techniques.
- [JSON Transformation](docs/JsonTransformation.md) - Merge, minify, and canonicalize JSON documents.

## Features
- **Serialization & Deserialization:** Efficiently convert C# objects to JSON and back with convenient extension methods and support for complex type hierarchies.
- **Advanced Comparison:** Compare JSON documents with fine-grained control over case sensitivity, whitespace handling, and array ordering for precise difference detection.
- **JSON Pointer Support:** Navigate and manipulate JSON documents using standardized JSON Pointer syntax (RFC 6901) for targeted operations.
- **JSON Path Querying:** Extract specific data from complex JSON documents using powerful JSONPath expressions with support for advanced filtering and projection.
- **Patch Generation & Application:** Create and apply JSON patches following RFC 6902 specifications to update documents efficiently with minimal data transfer.
- **Efficient Streaming:** Process large JSON files token by token without loading the entire content into memory, ideal for big data scenarios.
- **Document Transformation:** Merge, minify, and canonicalize JSON documents with configurable options to maintain semantic equivalence.
- **Difference Analysis:** Identify and report detailed differences between JSON structures with customizable output formats.
- **Property Mapping:** Transform JSON documents by mapping properties from one structure to another, perfect for data migration and API integration.
- **Schema Validation:** Validate JSON documents against JSON Schema definitions to ensure data integrity and structure compliance.
- **Performance Optimization:** Designed with high-performance use cases in mind, featuring minimal allocations and efficient algorithms.
- **Canonicalization:** Create canonical representations of JSON documents for cryptographic operations and comparison.
- **Security Features:** Protect against JSON injection attacks and secure handling of sensitive data.

## Usage
### ToJson
``` csharp
var simpleObject = new SimpleObject { Id = 1, Name = "John" };
string json = simpleObject.ToJson();
// json now contains '{"Id":1,"Name":"John"}'
```
### FromJson
``` csharp
string json = "{\"Id\":1,\"Name\":\"John\"}";
var simpleObject = json.FromJson<SimpleObject>();
// simpleObject is now an instance of SimpleObject with Id=1 and Name="John"
```
### AreEqual
``` csharp
string json1 = "{\"Id\":1,\"Name\":\"John\"}";
string json2 = "{\"Name\":\"John\",\"Id\":1}";
bool areEqual = json1.AreEqual(json2, ignoreWhitespace: true);
// areEqual is true, ignoring whitespace and field order
```
### StreamJsonFile
``` csharp
string filePath = "app.log";

filePath.StreamJsonFile((tokenType, tokenValue) => {
    // Process each token as it's read from the file
    if (tokenType == JsonTokenType.PropertyName && tokenValue.ToString() == "level" && 
        reader.Read() && reader.GetString() == "INFO") {
        // Process INFO level entries
    }
});
```
### MapProperties
``` csharp
// Original JSON with customer data
string customerJson = @"{
    ""CustomerName"": ""John Smith"",
    ""CustomerID"": 12345,
    ""Address"": {
        ""Street"": ""123 Main St"",
        ""City"": ""Springfield"",
        ""ZIP"": ""12345""
    }
}";

// Define property mapping for API standards
var propertyMapping = new Dictionary<string, string>
{
    {"CustomerName", "name"},
    {"CustomerID", "id"},
    {"Street", "streetAddress"},
    {"ZIP", "postalCode"}
};

// Apply the mapping
string transformedJson = customerJson.MapProperties(propertyMapping);
```
For detailed usage examples and API documentation, please refer to the documentation links above.
## Installation
``` bash
dotnet add package AnotherJsonLib
```
## Contributing
Contributions are welcome! Please feel free to submit a Pull Request.
## License
This project is licensed under the MIT License - see the [LICENSE](https://github.com/JacquesBronk/another-json-lib/blob/main/LICENSE) file for details.
