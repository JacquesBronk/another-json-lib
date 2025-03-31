# AnotherJsonLib.Utility.Transformation
## Overview
The `AnotherJsonLib.Utility.Transformation` namespace provides a set of utilities designed to transform JSON data structures with ease and flexibility. These tools help manipulate JSON by targeting specific properties, values, or the entire JSON structure without having to manually parse and recreate complex JSON objects.
## Components
The namespace includes three main components:
1. **JsonTransformer**: Core transformation engine that orchestrates the transformation process
2. **JsonPropertyTransformer**: Specialized in transforming JSON property names
3. **JsonValueTransformer**: Focused on transforming JSON values based on various conditions

## Use Cases
- **Data Migration**: Transform JSON data structure from one schema to another when interfacing with different APIs or systems
- **Data Normalization**: Standardize property names or value formats across your JSON data
- **Data Enrichment**: Add, modify, or compute new values based on existing data
- **Format Conversion**: Convert between different JSON formatting conventions (e.g., camelCase to snake_case)
- **Data Masking**: Transform sensitive data in JSON objects for logging or debugging purposes

## Code Examples
### Basic Property Transformation
``` csharp
using AnotherJsonLib.Utility.Transformation;
using System.Text.Json;

// Sample JSON with mixed-case property names
string jsonInput = @"{""firstName"":""John"",""LastName"":""Doe"",""email_address"":""john.doe@example.com""}";

// Create a property transformer to convert all property names to camelCase
var transformer = new JsonPropertyTransformer(PropertyNameTransformations.ToCamelCase);

// Apply the transformation
string transformedJson = transformer.Transform(jsonInput);

// Result: {"firstName":"John","lastName":"Doe","emailAddress":"john.doe@example.com"}
Console.WriteLine(transformedJson);
```
### Value Transformation
``` csharp
using AnotherJsonLib.Utility.Transformation;
using System.Text.Json;

// Sample JSON with values we want to transform
string jsonInput = @"{""name"":""Jane Doe"",""email"":""jane.doe@example.com"",""ssn"":""123-45-6789""}";

// Create a value transformer to mask the SSN
var transformer = new JsonValueTransformer();
transformer.AddRule("$.ssn", value => "XXX-XX-" + value.ToString().Substring(7));

// Apply the transformation
string transformedJson = transformer.Transform(jsonInput);

// Result: {"name":"Jane Doe","email":"jane.doe@example.com","ssn":"XXX-XX-6789"}
Console.WriteLine(transformedJson);
```
### Chaining Transformations
``` csharp
using AnotherJsonLib.Utility.Transformation;
using System.Text.Json;

string jsonInput = @"{""USER_ID"":1,""User_Name"":""admin"",""CREATED_DATE"":""2023-01-01""}";

// Create a composite transformer
var transformer = new JsonTransformer();

// Add property name transformation (convert to camelCase)
transformer.AddPropertyTransformation(PropertyNameTransformations.ToCamelCase);

// Add value transformations
transformer.AddValueTransformation("$.userId", value => int.Parse(value.ToString()) + 1000);
transformer.AddValueTransformation("$.createdDate", value => DateTime.Parse(value.ToString()).ToString("yyyy-MM-dd HH:mm:ss"));

// Apply all transformations
string transformedJson = transformer.Transform(jsonInput);

// Result: {"userId":1001,"userName":"admin","createdDate":"2023-01-01 00:00:00"}
Console.WriteLine(transformedJson);
```
### Conditional Transformations
``` csharp
using AnotherJsonLib.Utility.Transformation;
using System.Text.Json;

string jsonInput = @"{""items"":[{""id"":1,""price"":10.99},{""id"":2,""price"":0},{""id"":3,""price"":-5.99}]}";

// Create a value transformer with conditional logic
var transformer = new JsonValueTransformer();

// Apply discount to positive prices only
transformer.AddRule("$.items[*].price", 
    value => decimal.Parse(value.ToString()) > 0 
        ? decimal.Parse(value.ToString()) * 0.9m  // 10% discount
        : value,
    condition: value => decimal.Parse(value.ToString()) > 0);

// Set negative prices to zero
transformer.AddRule("$.items[*].price",
    value => 0,
    condition: value => decimal.Parse(value.ToString()) < 0);

// Apply the transformations
string transformedJson = transformer.Transform(jsonInput);

// Result: {"items":[{"id":1,"price":9.891},{"id":2,"price":0},{"id":3,"price":0}]}
Console.WriteLine(transformedJson);
```
## Benefits
- **Declarative approach**: Define what should be transformed rather than how
- **JsonPath support**: Target specific parts of the JSON using JsonPath expressions
- **Fluent API**: Chain multiple transformations for complex scenarios
- **Type safety**: Works with the System.Text.Json serialization system
- **Performance optimized**: Transforms JSON without full deserialization when possible

## Requirements
- .NET 8.0 or higher
- System.Text.Json

## Contributing
Contributions are welcome! Please feel free to submit a Pull Request.
This markdown provides an overview of the JsonTransformation utilities in the `AnotherJsonLib.Utility.Transformation` namespace. The examples demonstrate the major use cases and how to work with the different transformers available in the library.
