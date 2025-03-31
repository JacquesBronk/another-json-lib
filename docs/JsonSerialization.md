# AnotherJsonLib.Utility.Serialization
## Overview
`AnotherJsonLib.Utility.Serialization` is a static utility class that provides convenient JSON serialization and deserialization functionality built on top of `System.Text.Json`. It offers extension methods for converting objects to JSON strings and parsing JSON strings back into objects with robust error handling.
## Features
- Simple, fluent API for JSON serialization and deserialization
- Built-in error handling
- Performance tracking options
- Configurable serialization options
- Default serializer settings optimized for common use cases

## Default Serializer Settings
The class uses the following default JSON serializer settings:
``` csharp
private static readonly JsonSerializerOptions DefaultSerializerSettings = new JsonSerializerOptions()
{
    PropertyNameCaseInsensitive = true,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    IncludeFields = true,
    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
```
## Methods
### Serialization
#### ToJson<T>
``` csharp
public static string ToJson<T>(this T data, JsonSerializerOptions? options = null, bool useDiagnosticTracker = false)
```
Serializes an object to a JSON string.
**Parameters:**
- `data`: The object to serialize
- `options`: Optional JSON serializer options to override default settings
- `useDiagnosticTracker`: If true, uses DiagnosticPerformanceTracker; otherwise uses standard PerformanceTracker

**Returns:** A JSON string representation of the object
#### TryToJson<T>
``` csharp
public static bool TryToJson<T>(this T data, out string result, JsonSerializerOptions? options = null, bool useDiagnosticTracker = false)
```
Tries to serialize an object to a JSON string.
**Parameters:**
- `data`: The object to serialize
- `result`: When this method returns, contains the serialized JSON if successful; otherwise, an empty string
- `options`: Optional JSON serializer options to override default settings
- `useDiagnosticTracker`: If true, uses DiagnosticPerformanceTracker; otherwise uses standard PerformanceTracker

**Returns:** True if serialization was successful; otherwise, false
### Deserialization
#### FromJson<T>
``` csharp
public static T? FromJson<T>(this string json, JsonSerializerOptions? options = null, bool useDiagnosticTracker = false)
```
Deserializes a JSON string to an object.
**Parameters:**
- `json`: The JSON string to deserialize
- `options`: Optional JSON serializer options to override default settings
- `useDiagnosticTracker`: If true, uses DiagnosticPerformanceTracker; otherwise uses standard PerformanceTracker

**Returns:** The deserialized object, or default value if deserialization fails
#### TryFromJson<T>
``` csharp
public static bool TryFromJson<T>(this string json, out T? result, JsonSerializerOptions? options = null, bool useDiagnosticTracker = false)
```
Tries to deserialize a JSON string to an object.
**Parameters:**
- `json`: The JSON string to deserialize
- `result`: When this method returns, contains the deserialized object if successful; otherwise, default value
- `options`: Optional JSON serializer options to override default settings
- `useDiagnosticTracker`: If true, uses DiagnosticPerformanceTracker; otherwise uses standard PerformanceTracker

**Returns:** True if deserialization was successful; otherwise, false
## Usage Examples
### Basic Serialization
``` csharp
// Define a simple class
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// Create an instance and serialize it to JSON
var person = new Person { Name = "Alice", Age = 30 };
string json = person.ToJson();
// Result: {"Name":"Alice","Age":30}
```
### Basic Deserialization
``` csharp
// JSON string
string json = "{\"Name\":\"Bob\",\"Age\":25}";

// Deserialize to object
var person = json.FromJson<Person>();
// Result: Person { Name = "Bob", Age = 25 }
```
### Error Handling with Try Methods
``` csharp
// Attempt to serialize
if (person.TryToJson(out string personJson))
{
    Console.WriteLine($"Serialization successful: {personJson}");
}
else
{
    Console.WriteLine("Serialization failed");
}

// Attempt to deserialize
string invalidJson = "{\"Name\":\"Charlie\", bad json}";
if (invalidJson.TryFromJson<Person>(out var result))
{
    Console.WriteLine($"Deserialization successful: {result.Name}, {result.Age}");
}
else
{
    Console.WriteLine("Deserialization failed");
}
```
### Custom Serialization Options
``` csharp
// Create custom serialization options
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

// Use custom options for serialization
string prettyJson = person.ToJson(options);
/* Result:
{
  "name": "Alice",
  "age": 30
}
*/
```
### Working with JsonElement
``` csharp
// Parse JSON to JsonElement
var element = json.FromJson<JsonElement>();

// Convert JsonElement back to compact JSON
string compactJson = element.ToJson();

// Convert JsonElement to indented JSON
var indentedOptions = new JsonSerializerOptions { WriteIndented = true };
string indentedJson = element.ToJson(indentedOptions);
```
### Dictionary Serialization and Deserialization
``` csharp
// Create a dictionary
var dictionary = new Dictionary<string, object>
{
    { "name", "David" },
    { "age", 40 },
    { "isActive", true }
};

// Serialize dictionary to JSON
string jsonDict = dictionary.ToJson();

// Deserialize back to dictionary (using JsonElement for values)
var deserializedDict = jsonDict.FromJson<Dictionary<string, JsonElement>>();
```
## Best Practices
1. **Use the Try methods** when there's a possibility of serialization/deserialization failure to avoid exceptions
2. **Customize serialization options** when needed, but the default settings are suitable for most cases
3. **Consider performance tracking** for operations that might impact performance in critical paths
4. **Null handling** - the default settings will ignore null values when serializing

## Dependencies
This class depends on:
- `System.Text.Json` namespace for core JSON functionality
- `ExceptionHelpers` for null checks and safe execution
- Internal performance tracking mechanisms
