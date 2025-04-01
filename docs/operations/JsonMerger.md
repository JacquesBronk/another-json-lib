# JsonMerger

_Allows merging of multiple JSON documents into a single document._

## Overview

`JsonMerger` is a static utility class that provides functionality to combine two or more JSON documents into a unified result. It applies configurable strategies to resolve conflicts where data overlaps, making it a versatile tool for various JSON manipulation scenarios.

### Key Features

- Merge two or more JSON documents with customizable conflict resolution
- Multiple array merging strategies (concatenation, replacement, position-based merging)
- Deep merging of nested objects
- Preservation of data types during merging
- Optional formatting of the merged output

### Use Cases

- Combining data from multiple sources into a unified document
- Applying patches or updates to existing JSON documents
- Implementing partial updates for REST APIs
- Building configuration systems with layered settings
- Creating document version control and change management systems
- Implementing overridable default configurations

## Methods

### `Merge(string originalJson, string patchJson, MergeOptions? options = null)`

Merges two JSON strings, with the second JSON (patch) overriding values in the original where they overlap. The behavior is configurable through `MergeOptions`.

#### Parameters

- **originalJson**: The original/base JSON string.
- **patchJson**: The JSON string containing updates or patches to apply.
- **options**: Optional configuration for the merge behavior. If null, default options are used.

#### Returns

A new JSON string containing the merged result.

#### Exceptions

- **JsonArgumentException**: Thrown when either JSON string is null or empty.
- **JsonParsingException**: Thrown when either input is not valid JSON.
- **JsonOperationException**: Thrown when the merge operation fails for other reasons.

---

### `MergeMultiple(IEnumerable jsonDocuments, MergeOptions? options = null)`

Merges multiple JSON documents in sequence, with each subsequent document potentially overriding values from previous documents.

#### Parameters

- **jsonDocuments**: An enumerable collection of JSON strings to merge.
- **options**: Optional configuration for the merge behavior. If null, default options are used.

#### Returns

A new JSON string containing the merged result of all input documents.

#### Exceptions

- Similar exceptions to the basic `Merge` method.

---

## MergeOptions

The `MergeOptions` class provides configuration for merge operations:

### Properties

- **ArrayMergeStrategy**: Controls how arrays from different documents are combined.
    - **Concat**: (Default) Combines arrays from both documents into one longer array.
    - **Replace**: Uses arrays from the patch document where they exist, replacing original arrays completely.
    - **Merge**: Combines array elements by position, with patch elements overriding originals at the same index.

- **FormatOutput**: When true, the resulting JSON will be indented for better readability.

---

## Examples

### Basic Merging

```csharp
string baseConfig = @"{
    ""server"": {
        ""port"": 8080,
        ""host"": ""localhost"",
        ""timeoutSeconds"": 30
    },
    ""logging"": {
        ""level"": ""info""
    }
}";

string overrides = @"{
    ""server"": {
        ""port"": 9000,
        ""timeoutSeconds"": 60
    },
    ""logging"": {
        ""format"": ""json""
    }
}";

string merged = JsonMerger.Merge(baseConfig, overrides);

// Result:
// {
//   "server": {
//     "port": 9000,
//     "host": "localhost",
//     "timeoutSeconds": 60
//   },
//   "logging": {
//     "level": "info",
//     "format": "json"
//   }
// }
```

---

### Array Handling Strategies

```csharp
string originalJson = @"{
    ""tags"": [""important"", ""user""],
    ""permissions"": [""read"", ""write""]
}";

string patchJson = @"{
    ""tags"": [""critical"", ""system""]
}";

// Using Concat strategy (default)
string concatResult = JsonMerger.Merge(originalJson, patchJson);
// tags will be ["important", "user", "critical", "system"]
// permissions remains ["read", "write"]

// Using Replace strategy
var replaceOptions = new MergeOptions { ArrayMergeStrategy = ArrayMergeStrategy.Replace };
string replaceResult = JsonMerger.Merge(originalJson, patchJson, replaceOptions);
// tags will be ["critical", "system"]
// permissions remains ["read", "write"]

// Using Merge strategy
var mergeOptions = new MergeOptions { ArrayMergeStrategy = ArrayMergeStrategy.Merge };
string mergeResult = JsonMerger.Merge(originalJson, patchJson, mergeOptions);
// tags will be ["critical", "system"]
// permissions remains ["read", "write"]
```

---

### Multi-document Merging

```csharp
// Layer 1: Default settings
string defaults = @"{
    ""theme"": ""light"",
    ""fontSize"": 12,
    ""showToolbar"": true,
    ""panels"": [""explorer"", ""output"", ""terminal""]
}";

// Layer 2: User profile settings
string userProfile = @"{
    ""theme"": ""dark"",
    ""panels"": [""explorer"", ""debug""]
}";

// Layer 3: Project-specific settings
string projectSettings = @"{
    ""fontSize"": 14,
    ""compiler"": {
        ""target"": ""es2020""
    }
}";

// Merge all layers with the last having highest priority
string mergedSettings = JsonMerger.MergeMultiple(
        new[] { defaults, userProfile, projectSettings }
);

// Result combines all settings with later documents taking precedence
```

---

### Edge Cases

#### Empty Objects

When merging with empty objects, the non-empty object's properties are preserved:

```csharp
string nonEmpty = @"{""name"": ""Product"", ""price"": 10.99}";
string empty = @"{}";

string result1 = JsonMerger.Merge(nonEmpty, empty);
// Result is equivalent to nonEmpty

string result2 = JsonMerger.Merge(empty, nonEmpty);
// Result is equivalent to nonEmpty
```

#### Null Values

Null values in the patch document explicitly override values in the original:

```csharp
string original = @"{""status"": ""active"", ""description"": ""Primary account""}";
string patch = @"{""description"": null}";

string result = JsonMerger.Merge(original, patch);
// Result: {"status": "active", "description": null}
```

#### Type Conflicts

When property types differ between documents, the patch type takes precedence:

```csharp
string original = @"{""data"": {""count"": 5}}";
string patch = @"{""data"": [1, 2, 3]}";

string result = JsonMerger.Merge(original, patch);
// Result: {"data": [1, 2, 3]}
```

#### Array of Objects

When merging arrays that contain objects, the chosen strategy affects how object properties are handled:

```csharp
string original = @"{""users"": [
    {""id"": 1, ""name"": ""Alice""},
    {""id"": 2, ""name"": ""Bob""}
]}";

string patch = @"{""users"": [
    {""id"": 1, ""role"": ""admin""},
    {""id"": 3, ""name"": ""Charlie""}
]}";

// With custom handling to merge objects by ID
// (requires implementation of a custom merger)
```

---

## Performance Considerations

- For very large JSON documents, consider memory usage as the entire document tree is processed.
- The time complexity increases with document size and nesting depth.
- When merging numerous documents, use `MergeMultiple` rather than chained `Merge` calls.

---

## Best Practices

1. **Validate Inputs**: Ensure all input documents are valid JSON before attempting to merge.
2. **Choose Strategies Deliberately**: Select an array merge strategy that fits your specific needs.
3. **Handle Errors**: Implement proper error handling for invalid inputs.
4. **Test Edge Cases**: Verify the behavior with empty objects, null values, and type conflicts.
5. **Consider Schema Compatibility**: Be aware that merging incompatible schemas might lead to unexpected results.

---

## Related Functionality

- **JsonMapping**: For transforming JSON structure by renaming properties.
- **JsonPatchGenerator**: For creating RFC 6902 JSON Patch documents.
- **JsonPointer**: For referencing specific parts within JSON documents.

By leveraging `JsonMerger`, you can implement sophisticated JSON document manipulation strategies that maintain data integrity while combining information from various sources.