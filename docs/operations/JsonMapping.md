# JsonMapping

## Overview
`JsonMapping` is a static utility class in the `AnotherJsonLib.Utility.Operations` namespace that provides functionality for transforming JSON documents by mapping properties from one structure to another. It's particularly useful for data migration, API integration, and normalizing data from different sources.

### Key Features
- Rename JSON properties according to a mapping dictionary
- Perform recursive transformation of nested objects and arrays
- Preserve JSON data types during transformation
- Option for formatted output with indentation

## Methods

### MapProperties(string json, Dictionary<string, string> mapping)
Renames object properties in a JSON document according to a mapping dictionary. For every property in an object, if a mapping exists for the key, it is renamed accordingly. The transformation is applied recursively to all nested objects and arrays.

#### Parameters
- **json**: The JSON string to transform.
- **mapping**: A dictionary where keys are original property names and values are the new property names.

#### Returns
A new JSON string with the properties renamed according to the mapping.

#### Exceptions
- **JsonArgumentException**: Thrown when the input JSON or mapping is null.
- **JsonParsingException**: Thrown when the input is not valid JSON.
- **JsonOperationException**: Thrown when the mapping operation fails for other reasons.

#### Example
```csharp
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

// Result:
// {
//    "name": "John Smith",
//    "id": 12345,
//    "Address": {
//        "streetAddress": "123 Main St",
//        "City": "Springfield",
//        "postalCode": "12345"
//    }
// }
```

### MapProperties(string json, Dictionary<string, string> mapping, bool formatOutput)
An overloaded version of the `MapProperties` method that allows control over the formatting of the output JSON.

#### Additional Parameters
- **formatOutput**: When set to true, the output JSON will be indented for better readability. Default is false.

#### Example
```csharp
// Generate pretty-printed JSON with the mapping applied
string formattedJson = customerJson.MapProperties(propertyMapping, true);
```

## Behavior with Complex Structures

### Arrays
The mapping applies to all objects within arrays, maintaining the array structure:
```csharp
string jsonWithArray = @"{
    ""People"": [
        { ""PersonName"": ""Alice"", ""PersonAge"": 30 },
        { ""PersonName"": ""Bob"", ""PersonAge"": 25 }
    ]
}";

var mapping = new Dictionary<string, string>
{
    {"PersonName", "name"},
    {"PersonAge", "age"}
};

string result = jsonWithArray.MapProperties(mapping);
// Result will maintain the array structure with renamed properties
```

### Nested Objects
The mapping is applied recursively to all nested objects:
```csharp
string nestedJson = @"{
    ""User"": {
        ""UserDetails"": {
            ""UserName"": ""john_doe"",
            ""Email"": ""john@example.com""
        }
    }
}";

var mapping = new Dictionary<string, string>
{
    {"UserName", "username"},
    {"Email", "emailAddress"}
};

string result = nestedJson.MapProperties(mapping);
// Result will rename properties in the nested UserDetails object
```

## Edge Cases

### Empty Objects or Arrays
Empty objects and arrays are preserved in the output:
```csharp
string jsonWithEmpty = @"{""EmptyObject"":{},""EmptyArray"":[]}";
// Mapping will preserve the empty structures
```

### Property Name Collisions
If a mapping would result in multiple properties having the same name, the behavior is undefined and may lead to data loss:
```csharp
string jsonWithConflict = @"{""prop1"": ""value1"", ""prop2"": ""value2""}";
var conflictMapping = new Dictionary<string, string>
{
    {"prop1", "newName"},
    {"prop2", "newName"}  // Conflict with the mapping above!
};
// This could result in only one property being preserved
```

### Non-string Keys
The mapping only applies to string property names, which is standard for JSON objects:
```csharp
// Arrays with numeric indices are not affected by the mapping
string arrayJson = @"[""first"", ""second"", ""third""]";
// Result will be identical to input
```

### Performance Considerations
For very large JSON documents, the operation may have memory and performance implications:
- The entire document is loaded into memory
- A new document is constructed with the mapped properties
- Consider breaking up large documents when possible

## Best Practices
1. **Validate Mapping**: Check your mapping dictionary for potential conflicts before applying it.
2. **Error Handling**: Always wrap mapping operations in try-catch blocks to handle exceptions.
3. **Preserve Types**: The mapping preserves the original data types of values.
4. **Testing**: Test with edge cases like empty objects, complex nested structures, and arrays.

## Common Use Cases
- Adapting data from one API format to another
- Standardizing property names from various data sources
- Preparing data for export to different systems
- Migrating data between schema versions

## Related Classes
- **JsonMerger**: For combining multiple JSON documents
- **JsonPatchGenerator**: For generating patch operations between JSON documents
- **JsonSorter**: For sorting JSON objects by property names

## Notes
- The transformation creates a new JSON document and does not modify the original.
- The implementation uses `System.Text.Json` for parsing and serialization.
- Performance tracking is included via the `PerformanceTracker` class.

By utilizing `JsonMapping`, you can easily transform JSON data between different schemas while maintaining the integrity of your data structures.
