# JsonSorter

_Provides functionality to sort JSON objects by their property names._

## Overview

`JsonSorter` is a utility class that creates normalized JSON representations by consistently ordering object properties. While JSON property ordering is semantically insignificant according to the specification, having a standardized ordering provides numerous benefits in various scenarios.

## Key Features

- Lexicographical sorting of JSON object properties at all nesting levels
- Preservation of original data types and values
- Recursive processing of nested objects and arrays
- Optional pretty-printing with indentation
- High-performance implementation using System.Text.Json

## Use Cases

- Creating canonical representations for cryptographic operations and digital signatures
- Enabling meaningful comparisons between JSON objects
- Generating consistent hash values from JSON data
- Improving readability of JSON data for debugging or documentation
- Ensuring deterministic serialization for testing and verification
- Normalizing JSON before storage or transmission
- Facilitating consistent diff generation between JSON versions

## Methods

### SortJson(string json, bool indented = false)

Normalizes a JSON string by sorting all object properties in lexicographical order throughout the entire document.

#### Parameters

- **json**: The input JSON string to be sorted.
- **indented**: Optional boolean indicating whether to format the output with indentation (default is false).

#### Returns

A normalized JSON string with sorted properties.

#### Exceptions

- **ArgumentNullException**: Thrown when the json parameter is null.
- **JsonParsingException**: Thrown when the input is not valid JSON.
- **JsonSortingException**: Thrown when an error occurs during the sorting process.

## Examples

### Basic Sorting

```csharp
// Unsorted JSON with arbitrary property order
string json = @"{
    ""status"": ""active"",
    ""id"": 12345,
    ""name"": ""Example Item"",
    ""createdAt"": ""2023-01-15T10:30:00Z""
}";

// Sort properties lexicographically
string sorted = JsonSorter.SortJson(json);

// Result: {"createdAt":"2023-01-15T10:30:00Z","id":12345,"name":"Example Item","status":"active"}
```

### Nested Objects and Arrays

```csharp
// Complex JSON with nested structures
string json = @"{
    ""results"": [
        {
            ""score"": 95,
            ""user"": ""alice"",
            ""details"": {
                ""time"": 45,
                ""correct"": 19,
                ""attempts"": 20
            }
        },
        {
            ""user"": ""bob"",
            ""score"": 82,
            ""details"": {
                ""attempts"": 20,
                ""correct"": 16,
                ""time"": 38
            }
        }
    ],
    ""metadata"": {
        ""version"": ""2.1"",
        ""generated"": ""2023-06-10T14:22:00Z"",
        ""source"": ""quiz-system""
    }
}";

// Sort with indentation for readability
string sorted = JsonSorter.SortJson(json, true);

// Result will have all object properties sorted alphabetically at every level
// while maintaining array order and all original values
```

### Canonicalization for Cryptographic Operations

```csharp
// Two semantically identical JSONs with different property ordering
string json1 = @"{""b"":2,""a"":1,""c"":3}";
string json2 = @"{""a"":1,""c"":3,""b"":2}";

// Normalize both for comparison
string canonical1 = JsonSorter.SortJson(json1);
string canonical2 = JsonSorter.SortJson(json2);

// canonical1 and canonical2 will be identical:
// {"a":1,"b":2,"c":3}

// This allows for cryptographic operations like signing or hashing
string hash1 = ComputeHash(canonical1);
string hash2 = ComputeHash(canonical2);

// hash1 will equal hash2 due to the canonical representation
```

## Behavior Details

### Property Ordering

Properties are sorted using lexicographical (dictionary) ordering of property names. This means:

- "a" comes before "b", which comes before "c"
- Lowercase letters come after uppercase letters ("Z" comes before "a")
- Numeric characters come before alphabetic characters ("1" comes before "A")

### Nested Objects

All objects at any level of nesting will have their properties sorted:

```csharp
// Input
{
    "person": {
        "name": "Alice",
        "age": 30,
        "contact": {
            "phone": "555-1234",
            "email": "alice@example.com"
        }
    }
}

// Output
{
    "person": {
        "age": 30,
        "contact": {
            "email": "alice@example.com",
            "phone": "555-1234"
        },
        "name": "Alice"
    }
}
```

### Arrays

Array elements maintain their original order, as sequence is significant in JSON arrays:

```csharp
// Input
{
    "values": [10, 5, 8, 2]
}

// Output
{
    "values": [10, 5, 8, 2]
}
```

However, objects within arrays will have their properties sorted:

```csharp
// Input
{
    "users": [
        {"role": "admin", "name": "Alice"},
        {"name": "Bob", "role": "user"}
    ]
}

// Output
{
    "users": [
        {"name": "Alice", "role": "admin"},
        {"name": "Bob", "role": "user"}
    ]
}
```

### Data Types

All JSON data types are preserved during the sorting process:

- Objects
- Arrays
- Strings
- Numbers
- Booleans (true/false)
- null

## Formatting Options

### Compact Output (Default)

By default, the sorted JSON is returned without whitespace, maximizing compactness:

```csharp
string compact = JsonSorter.SortJson(json);
// {"name":"Example","values":[1,2,3]}
```

### Indented Output

For improved human readability, you can request indented formatting:

```csharp
string readable = JsonSorter.SortJson(json, true);
// {
//   "name": "Example",
//   "values": [
//     1,
//     2,
//     3
//   ]
// }
```

## Performance Considerations

- The implementation uses System.Text.Json for efficient parsing and serialization
- Time complexity is generally O(n log n) where n is the number of properties
- Memory usage scales with the complexity and size of the JSON document
- For very large documents, be mindful of memory consumption during the sorting process

## Best Practices

1. **Use for Canonical Representation**: When you need deterministic JSON output for signing, hashing, or comparison
2. **Compare Normalized JSON**: Always normalize JSON before comparing for semantic equality
3. **Don't Depend on Order**: Even though the output is sorted, consuming code should not depend on property order
4. **Benchmark with Large Documents**: Test performance with your expected document sizes
5. **Error Handling**: Always handle potential exceptions, especially with user-provided JSON

## Related Functionality

- **JsonComparer**: For comparing JSON documents semantically rather than by exact string matching
- **JsonMerger**: For combining multiple JSON documents with strategies for resolving conflicts
- **JsonMinifier**: For removing unnecessary whitespace from JSON documents
- **JsonDiffer**: For generating difference reports between JSON documents

By employing JsonSorter for JSON normalization, you can achieve consistent representations, reliable comparisons, and deterministic processing of JSON data throughout your application.