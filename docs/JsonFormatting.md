# AnotherJsonLib.Utility.Formatting
## Overview
The `AnotherJsonLib.Utility.Formatting` namespace provides utility classes and extension methods for JSON formatting and canonicalization operations in the AnotherJsonLib library.
## What is JSON Canonicalization?

JSON canonicalization is the process of converting JSON data into a standardized, canonical form according to a set of predefined rules. This creates a consistent representation of structurally equivalent JSON documents regardless of initial formatting differences.

### Key aspects of canonicalization:

1. **Deterministic property ordering**: Object properties are sorted alphabetically
```json
// Before canonicalization
   {"z": 1, "a": 2}
   
   // After canonicalization
   {"a": 2, "z": 1}
```

2. **Whitespace normalization**: Extra whitespace is removed or standardized
```json
// Before canonicalization
   {  "name"  :  "John"  }
   
   // After canonicalization
   {"name":"John"}
```

3. **Consistent number formatting**: Numbers are formatted consistently
```json
// Before canonicalization
   {"value": 1.000, "count": 2.}
   
   // After canonicalization
   {"count":2,"value":1}
```

4. **Unicode handling**: Characters are properly escaped according to standard rules

### Why Use JSON Canonicalization?

1. **Consistent hashing**: Generate reliable hash values from JSON content for caching or verification
2. **Digital signatures**: Create signatures that remain valid regardless of formatting differences
3. **Deterministic comparison**: Compare JSON content by structure and value rather than exact string matching
4. **Cache keys**: Create consistent keys for caching JSON data
5. **Network protocols**: Ensure consistent data transmission in distributed systems

### Use Cases

- **Blockchain applications**: Where data consistency is critical
- **API response validation**: Comparing expected vs. actual results
- **Configuration management**: Detecting meaningful changes in configuration files
- **Data interchange**: Ensuring consistent interpretation across systems

## Classes
### JsonCanonicalizationExtensions
This class provides extension methods for JSON canonicalization, which is the process of converting JSON documents into a standard format to enable reliable comparison and verification.
#### Usage Examples
``` csharp
using AnotherJsonLib.Utility.Formatting;

// Basic canonicalization of a JSON string
string jsonString = "{\"b\":2,\"a\":1}";
string canonicalJson = jsonString.Canonicalize();
// Result: {"a":1,"b":2}

// Canonicalize a JSON object
var jsonObject = new JsonObject();
jsonObject.Add("b", 2);
jsonObject.Add("a", 1);
string canonicalResult = jsonObject.Canonicalize();
// Result: {"a":1,"b":2}
```
### JsonComparator
This class provides methods for comparing JSON documents.
#### Usage Examples
``` csharp
using AnotherJsonLib.Utility.Formatting;

// Compare two JSON strings
string json1 = "{\"a\":1,\"b\":2}";
string json2 = "{\"b\":2,\"a\":1}";
bool areEqual = JsonComparator.AreEqual(json1, json2);
// Result: true (since the contents are the same despite different ordering)

// Compare JSON with structural differences
string json3 = "{\"a\":1,\"b\":[1,2,3]}";
string json4 = "{\"a\":1,\"b\":[3,2,1]}";
bool areStructurallyEqual = JsonComparator.AreEqual(json3, json4, compareArrayOrder: false);
// Result: true (when array order comparison is disabled)
```
## Common Use Cases
1. **Normalizing JSON data**: When you need to standardize JSON documents from different sources
2. **Signature verification**: Creating consistent JSON representations for digital signatures
3. **Equality comparison**: Comparing JSON documents regardless of formatting differences
4. **Caching**: Creating reliable keys for caching JSON content

## Edge Cases and Considerations
1. **Floating-point numbers**: Canonicalization may handle floating-point numbers differently, potentially affecting equality comparisons.
``` csharp
   // Be aware of floating-point representation issues
   string json1 = "{\"value\":1.0}";
   string json2 = "{\"value\":1.00}";
   // These might be considered equal after canonicalization
```
1. **Large JSON documents**: Canonicalization of extremely large JSON documents might impact performance. Consider chunking or streaming for large datasets.
2. **Unicode characters**: Ensure proper handling of unicode characters in your JSON strings.
``` csharp
   // Unicode handling example
   string jsonWithUnicode = "{\"message\":\"こんにちは\"}";
   string canonicalJson = jsonWithUnicode.Canonicalize();
```
1. **Null values**: The handling of null values might differ between serialization implementations.
``` csharp
   // Be careful with null values
   string jsonWithNull = "{\"nullValue\":null}";
   // Some implementations might omit null values in the canonical form
```
1. **Circular references**: Be aware of potential issues with circular references in your JSON structure.

## Best Practices
1. Use canonicalization before comparing JSON documents for equality.
2. Consider using the `JsonComparator` class instead of string equality for more robust JSON comparisons.
3. For performance-critical applications, consider implementing caching of canonicalized results.
4. Always validate your JSON before attempting to canonicalize it.

Note: This documentation is based on available class and namespace references. For complete details about method parameters and additional functionality, please refer to the inline code documentation or unit tests.
