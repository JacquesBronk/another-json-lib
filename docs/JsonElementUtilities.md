# AnotherJsonLib.Utility.JsonElementUtils
## Overview

`JsonElementUtils` is a utility class in the `AnotherJsonLib.Utility` namespace that provides methods for working with `System.Text.Json.JsonElement` objects. This static class helps with converting JSON elements to .NET objects, comparing JSON elements, and normalizing JSON data.

## Public Methods

### 1. ConvertToObject

```csharp
public static object? ConvertToObject(JsonElement element, bool sortProperties = false)
```

#### Description
Converts a JsonElement into a corresponding .NET object representation. This method handles JSON primitives, arrays, and objects.

#### Parameters
- `element`: The JsonElement to convert
- `sortProperties`: (Optional) When `true`, properties in JSON objects will be sorted by their names. Default is `false`.

#### Returns
- The .NET object representation of the provided JsonElement
- `null` if the value kind is Null or Undefined
- For primitives: corresponding .NET types (string, bool, int, long, decimal, or double)
- For objects: Dictionary<string, object?> or SortedDictionary<string, object?>
- For arrays: List<object?>

#### Example

```csharp
using System.Text.Json;
using AnotherJsonLib.Utility;

// Parse a JSON string to JsonDocument
string jsonString = @"{
    ""name"": ""John Smith"", 
    ""age"": 30,
    ""isActive"": true,
    ""scores"": [85, 90, 78]
}";

using JsonDocument doc = JsonDocument.Parse(jsonString);
JsonElement root = doc.RootElement;

// Convert to .NET object
object? result = JsonElementUtils.ConvertToObject(root);

// Access as Dictionary
if (result is IDictionary<string, object?> personDict)
{
    Console.WriteLine($"Name: {personDict["name"]}");
    Console.WriteLine($"Age: {personDict["age"]}");
    Console.WriteLine($"Is active: {personDict["isActive"]}");
    
    // Access nested array
    if (personDict["scores"] is List<object?> scores)
    {
        Console.WriteLine("Scores:");
        foreach (var score in scores)
        {
            Console.WriteLine($"- {score}");
        }
    }
}
```

#### Edge Cases
- Large numbers that exceed the range of `decimal` will be converted to `double`
- For numbers, the method attempts to use the most appropriate numeric type (int, long, decimal, double)
- If the JSON contains unexpected value kinds, it will return the string representation

### 2. DeepEquals

```csharp
public static bool DeepEquals(JsonElement a, JsonElement b, double epsilon = 0.0, bool caseSensitivePropertyNames = true)
```

#### Description
Compares two JsonElements for deep equality, checking their structure and values recursively.

#### Parameters
- `a`: First JsonElement to compare
- `b`: Second JsonElement to compare
- `epsilon`: (Optional) Tolerance for floating-point comparisons. Default is 0.0
- `caseSensitivePropertyNames`: (Optional) Whether to consider property name case during comparison. Default is `true`

#### Returns
- `true` if elements are deeply equal
- `false` otherwise

#### Example

```csharp
using System.Text.Json;
using AnotherJsonLib.Utility;

string json1 = @"{""value"": 10.0001, ""items"": [1, 2, 3]}";
string json2 = @"{""value"": 10.0002, ""items"": [1, 2, 3]}";

using JsonDocument doc1 = JsonDocument.Parse(json1);
using JsonDocument doc2 = JsonDocument.Parse(json2);

// Strict comparison (will likely return false)
bool strictEqual = JsonElementUtils.DeepEquals(doc1.RootElement, doc2.RootElement);
Console.WriteLine($"Strictly equal: {strictEqual}");

// With epsilon tolerance (will return true if difference is within tolerance)
bool tolerantEqual = JsonElementUtils.DeepEquals(doc1.RootElement, doc2.RootElement, 0.001);
Console.WriteLine($"Equal with tolerance: {tolerantEqual}");

// Case-insensitive property comparison
string json3 = @"{""VALUE"": 10, ""items"": [1, 2, 3]}";
using JsonDocument doc3 = JsonDocument.Parse(json3);
bool caseInsensitiveEqual = JsonElementUtils.DeepEquals(
    doc1.RootElement, 
    doc3.RootElement, 
    caseSensitivePropertyNames: false
);
Console.WriteLine($"Equal with case-insensitive properties: {caseInsensitiveEqual}");
```

#### Edge Cases
- Floating-point values should be compared with an appropriate epsilon to account for precision errors
- When comparing objects with different property name casing, set `caseSensitivePropertyNames` to `false`
- Arrays must contain the same elements in the same order to be considered equal

### 3. Normalize

```csharp
public static object? Normalize(JsonElement element)
```

#### Description
Normalizes a JsonElement to a standardized object representation, useful for consistent JSON processing.

#### Parameters
- `element`: The JsonElement to normalize

#### Returns
- A normalized object representation of the JsonElement

#### Example

```csharp
using System.Text.Json;
using AnotherJsonLib.Utility;

// Parse a JSON with mixed numeric formats
string jsonString = @"{
    ""integerAsString"": ""42"",
    ""decimalAsString"": ""10.5"",
    ""actualNumber"": 100
}";

using JsonDocument doc = JsonDocument.Parse(jsonString);
JsonElement root = doc.RootElement;

// Normalize the JsonElement
object? normalized = JsonElementUtils.Normalize(root);

// Use the normalized representation
Console.WriteLine($"Normalized JSON: {JsonSerializer.Serialize(normalized)}");
```

## Common Usage Scenarios

### Converting JsonElement to Strongly-Typed Objects

When you need to work with JSON data in a more structured way:

```csharp
using System.Text.Json;
using AnotherJsonLib.Utility;

// Parse configuration from JSON
string configJson = File.ReadAllText("config.json");
using JsonDocument doc = JsonDocument.Parse(configJson);

// Convert to Dictionary for easier access
var config = JsonElementUtils.ConvertToObject(doc.RootElement) as IDictionary<string, object?>;

// Access configuration values
string apiUrl = config["apiUrl"] as string;
int timeout = Convert.ToInt32(config["timeout"]);
```

### JSON Comparison for Testing

Useful in test scenarios when comparing expected and actual JSON results:

```csharp
using System.Text.Json;
using AnotherJsonLib.Utility;
using NUnit.Framework;

[Test]
public void ApiResponse_ShouldMatchExpected()
{
    // Arrange
    string expected = @"{""status"":""success"",""data"":{""id"":1,""value"":10.001}}";
    string actual = GetApiResponse(); // Some method that returns JSON
    
    using JsonDocument expectedDoc = JsonDocument.Parse(expected);
    using JsonDocument actualDoc = JsonDocument.Parse(actual);
    
    // Act & Assert
    // Compare with tolerance for floating point values
    bool areEqual = JsonElementUtils.DeepEquals(
        expectedDoc.RootElement, 
        actualDoc.RootElement,
        epsilon: 0.01
    );
    
    Assert.IsTrue(areEqual, "API response should match expected JSON");
}
```

## Edge Cases and Considerations

1. **Type Inference for Numbers**
    - The class attempts to use the most specific numeric type (int → long → decimal → double)
    - Very large numbers that exceed decimal range will be converted to double, which may lead to precision loss

2. **Floating-Point Comparison**
    - Always use an appropriate epsilon value when comparing floating-point numbers
    - Default epsilon of 0.0 requires exact equality, which may fail due to floating-point precision issues

3. **Memory Usage**
    - Converting large, deeply-nested JSON structures may consume significant memory
    - Consider processing JSON data in smaller chunks for large datasets

4. **Performance Considerations**
    - Recursive processing of deep JSON structures could lead to stack overflow for extremely nested JSONs
    - Sorting properties (`sortProperties = true`) adds additional overhead during conversion

5. **Property Name Case Sensitivity**
    - By default, property names are compared case-sensitively
    - For case-insensitive property name comparison, explicitly set `caseSensitivePropertyNames` to `false`

6. **Culture-Specific Issues**
    - Numeric parsing might be affected by culture settings in some edge cases
    - The class uses invariant culture internally for consistent behavior

## Best Practices

1. Always dispose JsonDocument instances using `using` statements
2. Use appropriate epsilon values for floating-point comparisons
3. Consider memory constraints when dealing with large JSON documents
4. Be explicit about case sensitivity requirements when comparing JSON objects
5. Handle potential exceptions that might arise from malformed JSON input

This utility class simplifies working with System.Text.Json's JsonElement by providing convenient conversion and comparison methods that handle the complexities of JSON data types and structures.