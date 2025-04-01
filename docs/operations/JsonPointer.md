# JsonPointer

_Implements RFC 6901 JSON Pointer for precise location referencing within JSON documents._

## Overview

`JsonPointer` is a utility class that provides functionality for referencing specific values within JSON documents using [RFC 6901 JSON Pointer](https://tools.ietf.org/html/rfc6901) syntax. JSON Pointer defines a string syntax for identifying a specific value within a JSON document, similar to how XPath works for XML but with a simpler syntax designed specifically for JSON's structure.

## Key Features

- Standards-compliant implementation following RFC 6901
- Precise targeting of values at any level in a JSON document
- Support for array indexing and property access
- Escape handling for special characters
- Error handling with try/catch patterns
- Helper methods for pointer construction

## JSON Pointer Syntax

JSON Pointer uses a concise syntax to navigate through JSON documents:

- A JSON Pointer starts with a forward slash `/` character
- Each segment between slashes represents a property name or an array index
- A pointer to the whole document is an empty string
- Special characters in property names are escaped using `~` followed by a replacement character:
    - `~0` represents `~` (tilde)
    - `~1` represents `/` (forward slash)
- Array indices are zero-based and represented as numbers

### Examples of JSON Pointer Syntax

Given this JSON document:

```json
{
    "foo": ["bar", "baz"],
    "": 0,
    "a/b": 1,
    "c%d": 2,
    "e^f": 3,
    "g|h": 4,
    "i\\j": 5,
    "k\"l": 6,
    " ": 7,
    "m~n": 8
}
```

The following JSON Pointers resolve to:

- `""` → the entire document
- `"/foo"` → `["bar", "baz"]`
- `"/foo/0"` → `"bar"`
- `"/foo/1"` → `"baz"`
- `"/"` → `0`
- `"/a~1b"` → `1` (note the escaping of `/` with `~1`)
- `"/c%d"` → `2`
- `"/e^f"` → `3`
- `"/g|h"` → `4`
- `"/i\\j"` → `5`
- `"/k\"l"` → `6`
- `"/ "` → `7`
- `"/m~0n"` → `8` (note the escaping of `~` with `~0`)

## Methods

### EvaluatePointer

```csharp
public static JsonElement? EvaluatePointer(this JsonDocument document, string pointer)
```

Evaluates a JSON Pointer against a JsonDocument and returns the referenced element.

#### Parameters

- **document**: The JsonDocument to evaluate the pointer against
- **pointer**: The JSON Pointer string that identifies the target location

#### Returns

- A JsonElement? representing the value at the specified location, or null if not found

#### Exceptions

- **ArgumentNullException**: Thrown when the document is null
- **JsonPointerSyntaxException**: Thrown when the pointer string has invalid syntax
- **JsonPointerResolutionException**: Thrown when the pointer can't be resolved (e.g., property doesn't exist)

#### Example

```csharp
string jsonString = @"{
    ""person"": {
        ""name"": ""John Doe"",
        ""age"": 30,
        ""address"": {
            ""street"": ""123 Main St"",
            ""city"": ""Anytown""
        },
        ""phoneNumbers"": [
            ""555-1234"",
            ""555-5678""
        ]
    }
}";

using JsonDocument document = JsonDocument.Parse(jsonString);

// Get a person's name
JsonElement? name = document.EvaluatePointer("/person/name");
Console.WriteLine(name?.GetString()); // Output: John Doe

// Get the second phone number
JsonElement? phone = document.EvaluatePointer("/person/phoneNumbers/1");
Console.WriteLine(phone?.GetString()); // Output: 555-5678

// Get the entire address object
JsonElement? address = document.EvaluatePointer("/person/address");
Console.WriteLine(address); // Output: {"street":"123 Main St","city":"Anytown"}
```

### TryEvaluatePointer

```csharp
public static bool TryEvaluatePointer(this JsonDocument? document, string pointer, out JsonElement? result)
```

Attempts to evaluate a JSON Pointer against a JsonDocument with error handling.

#### Parameters

- **document**: The JsonDocument to evaluate the pointer against
- **pointer**: The JSON Pointer string that identifies the target location
- **result**: Output parameter that receives the value at the specified location if found

#### Returns

- Boolean indicating whether the pointer was successfully evaluated

#### Example

```csharp
string jsonString = @"{""data"":{""items"":[1,2,3]}}";
using JsonDocument document = JsonDocument.Parse(jsonString);

if (document.TryEvaluatePointer("/data/items/1", out JsonElement? element))
{
        Console.WriteLine($"Found element: {element?.GetInt32()}"); // Output: Found element: 2
}
else
{
        Console.WriteLine("Element not found");
}

// For a path that doesn't exist:
if (document.TryEvaluatePointer("/data/missing", out JsonElement? notFound))
{
        Console.WriteLine($"Found element: {notFound}");
}
else
{
        Console.WriteLine("Element not found"); // This will execute
}
```

### Create

```csharp
public static string Create(params string[] segments)
```

Creates a JSON Pointer string from the provided path segments.

#### Parameters

- **segments**: An array of string segments representing the path components

#### Returns

- A properly formatted and escaped JSON Pointer string

#### Example

```csharp
// Create a pointer to a nested property
string pointer = JsonPointer.Create("person", "address", "street");
Console.WriteLine(pointer); // Output: /person/address/street

// Create a pointer with special characters that need escaping
string pointerWithSpecial = JsonPointer.Create("products", "category/type", "price");
Console.WriteLine(pointerWithSpecial); // Output: /products/category~1type/price
```

### Append

```csharp
public static string Append(string pointer, string segment)
```

Appends a new segment to an existing JSON Pointer string.

#### Parameters

- **pointer**: The existing JSON Pointer string
- **segment**: The new segment to append

#### Returns

- A new JSON Pointer string with the segment appended

#### Example

```csharp
// Start with a base pointer
string basePointer = "/data/users";

// Append a user ID
string userPointer = JsonPointer.Append(basePointer, "12345");
Console.WriteLine(userPointer); // Output: /data/users/12345

// Append a property name to access a specific field
string emailPointer = JsonPointer.Append(userPointer, "email");
Console.WriteLine(emailPointer); // Output: /data/users/12345/email
```

## Common Use Cases

### Document Navigation

```csharp
// Navigate to deeply nested elements
var config = document.EvaluatePointer("/settings/security/authentication/providers/0/config");
```

### Array Manipulation

```csharp
// Add an item at the end of an array
int arrayLength = document.EvaluatePointer("/items")?.GetArrayLength() ?? 0;
string newItemPointer = JsonPointer.Create("items", arrayLength.ToString());
```

### Dynamic Access Paths

```csharp
// Build pointers programmatically
string userId = "user123";
string field = "profile";
string dynamicPointer = JsonPointer.Create("users", userId, field);
var userProfile = document.EvaluatePointer(dynamicPointer);
```

### Combined with JSON Patch Operations

```csharp
// Define a JSON Patch operation targeting a specific location
var patchOperation = new
{
        op = "replace",
        path = JsonPointer.Create("person", "age"),
        value = 31
};
```

## JSON Pointer vs. JSONPath

While both are ways to reference locations in JSON documents, they serve different purposes:

| Feature | JSON Pointer | JSONPath |
| --- | --- | --- |
| Purpose | Precise targeting of a single value | Query language for selecting multiple values |
| Syntax | Concise slash-separated path | XPath-like syntax with operators |
| Result | Single value or null | Collection of matching values |
| Use Case | Exact references, patches | Searching, filtering, data extraction |
| Standard | RFC 6901 | De facto standard with variations |

```csharp
// JSON Pointer - gets one specific element
var specificField = document.EvaluatePointer("/users/0/email");

// JSONPath - can select multiple elements matching criteria
var allEmails = JsonPathQuery.QueryJson(jsonString, "$.users[*].email");
```

## Error Handling

### Common Errors

1. **Invalid Pointer Syntax**: If the pointer doesn't follow RFC 6901 syntax
2. **Non-existent Property**: When a property in the path doesn't exist
3. **Index Out of Bounds**: When an array index exceeds the available elements
4. **Type Mismatch**: When trying to navigate through a primitive value

### Best Practice

Always use the `TryEvaluatePointer` method when working with untrusted input or when you need to handle missing values gracefully:

```csharp
if (!document.TryEvaluatePointer(userProvidedPath, out var result))
{
        Console.WriteLine("Path not found or invalid");
        return;
}

// Safe to use result here
```

## Performance Considerations

- JSON Pointer is evaluated in a single pass through the document
- Time complexity is O(n) where n is the number of segments in the pointer
- For repeatedly accessing the same document, consider caching the JsonDocument
- When working with large documents, consider using streaming approaches for initial parsing

## Real-World Examples

### Configuration Access

```csharp
// Access specific configuration settings
var dbConnectionString = config.EvaluatePointer("/database/connectionStrings/default");
var logLevel = config.EvaluatePointer("/logging/level");
```

### API Response Processing

```csharp
// Extract specific data from an API response
var pageCount = apiResponse.EvaluatePointer("/meta/pagination/pages");
var firstResultId = apiResponse.EvaluatePointer("/data/0/id");
```

### Form Data Validation

```csharp
// Check if a specific field has validation errors
if (validationResult.TryEvaluatePointer("/errors/email", out var emailErrors))
{
        foreach (var error in emailErrors.EnumerateArray())
        {
                Console.WriteLine($"Email error: {error.GetString()}");
        }
}
```

## Best Practices

1. **Use `Create` and `Append` methods** to build pointers programmatically rather than string concatenation
2. **Use `TryEvaluatePointer`** for better error handling in production code
3. **Validate user input** before using it to construct JSON Pointers
4. **Consider case sensitivity** as JSON property names are case-sensitive
5. **Handle null results** appropriately when querying for optional fields

By understanding and using JSON Pointer effectively, you can precisely target and manipulate specific parts of JSON documents with minimal code and maximum clarity.