# AnotherJsonLib.Utility.Operations
## Overview
The `AnotherJsonLib.Utility.Operations` namespace contains utility classes for working with JSON documents, providing various operations for manipulating, comparing, and querying JSON data.
## Classes
### JsonMapping
#### Overview
`JsonMapping` is a static utility class in the `AnotherJsonLib.Utility.Operations` namespace that provides functionality for transforming JSON documents by mapping properties from one structure to another. It's particularly useful for data migration, API integration, and normalizing data from different sources.
##### Key Features
- Rename JSON properties according to a mapping dictionary
- Perform recursive transformation of nested objects and arrays
- Preserve JSON data types during transformation
- Option for formatted output with indentation

##### Methods
##### MapProperties(string json, Dictionary<string, string> mapping)
Renames object properties in a JSON document according to a mapping dictionary. For every property in an object, if a mapping exists for the key, it is renamed accordingly. The transformation is applied recursively to all nested objects and arrays.
###### Parameters
- **json**: The JSON string to transform.
- **mapping**: A dictionary where keys are original property names and values are the new property names.

###### Returns
A new JSON string with the properties renamed according to the mapping.
###### Exceptions
- **JsonArgumentException**: Thrown when the input JSON or mapping is null.
- **JsonParsingException**: Thrown when the input is not valid JSON.
- **JsonOperationException**: Thrown when the mapping operation fails for other reasons.

###### Example
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
##### MapProperties(string json, Dictionary<string, string> mapping, bool formatOutput)
An overloaded version of the MapProperties method that allows control over the formatting of the output JSON.
###### Additional Parameters
- **formatOutput**: When set to true, the output JSON will be indented for better readability. Default is false.

###### Example
``` csharp
// Generate pretty-printed JSON with the mapping applied
string formattedJson = customerJson.MapProperties(propertyMapping, true);
```
##### Behavior with Complex Structures
###### Arrays
The mapping applies to all objects within arrays, maintaining the array structure:
``` csharp
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
###### Nested Objects
The mapping is applied recursively to all nested objects:
``` csharp
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
##### Edge Cases
###### Empty Objects or Arrays
Empty objects and arrays are preserved in the output:
``` csharp
string jsonWithEmpty = @"{""EmptyObject"":{},""EmptyArray"":[]}";
// Mapping will preserve the empty structures
```
###### Property Name Collisions
If a mapping would result in multiple properties having the same name, the behavior is undefined and may lead to data loss:
``` csharp
string jsonWithConflict = @"{""prop1"": ""value1"", ""prop2"": ""value2""}";
var conflictMapping = new Dictionary<string, string>
{
    {"prop1", "newName"},
    {"prop2", "newName"}  // Conflict with the mapping above!
};
// This could result in only one property being preserved
```
###### Non-string Keys
The mapping only applies to string property names, which is standard for JSON objects:
``` csharp
// Arrays with numeric indices are not affected by the mapping
string arrayJson = @"[""first"", ""second"", ""third""]";
// Result will be identical to input
```
###### Performance Considerations
For very large JSON documents, the operation may have memory and performance implications:
- The entire document is loaded into memory
- A new document is constructed with the mapped properties
- Consider breaking up large documents when possible

##### Best Practices
1. **Validate Mapping**: Check your mapping dictionary for potential conflicts before applying it.
2. **Error Handling**: Always wrap mapping operations in try-catch blocks to handle exceptions.
3. **Preserve Types**: The mapping preserves the original data types of values.
4. **Testing**: Test with edge cases like empty objects, complex nested structures, and arrays.

##### Common Use Cases
- Adapting data from one API format to another
- Standardizing property names from various data sources
- Preparing data for export to different systems
- Migrating data between schema versions

##### Related Classes
- **JsonMerger**: For combining multiple JSON documents
- **JsonPatchGenerator**: For generating patch operations between JSON documents
- **JsonSorter**: For sorting JSON objects by property names

##### Notes
- The transformation creates a new JSON document and does not modify the original
- The implementation uses System.Text.Json for parsing and serialization
- Performance tracking is included via the PerformanceTracker class

By utilizing JsonMapping, you can easily transform JSON data between different schemas while maintaining the integrity of your data structures.

### JsonMerger
_Allows merging of multiple JSON documents into a single document._
#### Overview
`JsonMerger` is a static utility class that provides functionality to combine two or more JSON documents into a unified result. It applies configurable strategies to resolve conflicts where data overlaps, making it a versatile tool for various JSON manipulation scenarios.
##### Key Features
- Merge two or more JSON documents with customizable conflict resolution
- Multiple array merging strategies (concatenation, replacement, position-based merging)
- Deep merging of nested objects
- Preservation of data types during merging
- Optional formatting of the merged output

##### Use Cases
- Combining data from multiple sources into a unified document
- Applying patches or updates to existing JSON documents
- Implementing partial updates for REST APIs
- Building configuration systems with layered settings
- Creating document version control and change management systems
- Implementing overridable default configurations

##### Methods
###### Merge(string originalJson, string patchJson, MergeOptions? options = null)
Merges two JSON strings, with the second JSON (patch) overriding values in the original where they overlap. The behavior is configurable through MergeOptions.
###### Parameters
- **originalJson**: The original/base JSON string.
- **patchJson**: The JSON string containing updates or patches to apply.
- **options**: Optional configuration for the merge behavior. If null, default options are used.

###### Returns
A new JSON string containing the merged result.
###### Exceptions
- **JsonArgumentException**: Thrown when either JSON string is null or empty.
- **JsonParsingException**: Thrown when either input is not valid JSON.
- **JsonOperationException**: Thrown when the merge operation fails for other reasons.

##### MergeMultiple(IEnumerable jsonDocuments, MergeOptions? options = null)
Merges multiple JSON documents in sequence, with each subsequent document potentially overriding values from previous documents.
###### Parameters
- **jsonDocuments**: An enumerable collection of JSON strings to merge.
- **options**: Optional configuration for the merge behavior. If null, default options are used.

###### Returns
A new JSON string containing the merged result of all input documents.
###### Exceptions
- Similar exceptions to the basic Merge method.

##### MergeOptions
The `MergeOptions` class provides configuration for merge operations:
###### Properties
- **ArrayMergeStrategy**: Controls how arrays from different documents are combined.
    - **Concat**: (Default) Combines arrays from both documents into one longer array.
    - **Replace**: Uses arrays from the patch document where they exist, replacing original arrays completely.
    - **Merge**: Combines array elements by position, with patch elements overriding originals at the same index.

- **FormatOutput**: When true, the resulting JSON will be indented for better readability.

##### Examples
###### Basic Merging
string
string
``` csharp
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
###### Array Handling Strategies
``` csharp
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
###### Multi-document Merging
``` csharp
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
##### Edge Cases
###### Empty Objects
When merging with empty objects, the non-empty object's properties are preserved:
``` csharp
string nonEmpty = @"{""name"": ""Product"", ""price"": 10.99}";
string empty = @"{}";

string result1 = JsonMerger.Merge(nonEmpty, empty);
// Result is equivalent to nonEmpty

string result2 = JsonMerger.Merge(empty, nonEmpty);
// Result is equivalent to nonEmpty
```
###### Null Values
Null values in the patch document explicitly override values in the original:
``` csharp
string original = @"{""status"": ""active"", ""description"": ""Primary account""}";
string patch = @"{""description"": null}";

string result = JsonMerger.Merge(original, patch);
// Result: {"status": "active", "description": null}
```
###### Type Conflicts
When property types differ between documents, the patch type takes precedence:
``` csharp
string original = @"{""data"": {""count"": 5}}";
string patch = @"{""data"": [1, 2, 3]}";

string result = JsonMerger.Merge(original, patch);
// Result: {"data": [1, 2, 3]}
```
###### Array of Objects
When merging arrays that contain objects, the chosen strategy affects how object properties are handled:
``` csharp
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
##### Performance Considerations
- For very large JSON documents, consider memory usage as the entire document tree is processed
- The time complexity increases with document size and nesting depth
- When merging numerous documents, use MergeMultiple rather than chained Merge calls

##### Best Practices
1. **Validate Inputs**: Ensure all input documents are valid JSON before attempting to merge
2. **Choose Strategies Deliberately**: Select an array merge strategy that fits your specific needs
3. **Handle Errors**: Implement proper error handling for invalid inputs
4. **Test Edge Cases**: Verify the behavior with empty objects, null values, and type conflicts
5. **Consider Schema Compatibility**: Be aware that merging incompatible schemas might lead to unexpected results

##### Related Functionality
- **JsonMapping**: For transforming JSON structure by renaming properties
- **JsonPatchGenerator**: For creating RFC 6902 JSON Patch documents
- **JsonPointer**: For referencing specific parts within JSON documents

By leveraging JsonMerger, you can implement sophisticated JSON document manipulation strategies that maintain data integrity while combining information from various sources.

### JsonPatchGenerator
Generates JSON Patch operations that describe the difference between two JSON documents.
##### Methods
- **GeneratePatchOperations**: Creates a set of operations needed to transform one JSON document into another.
- **OptimizePatchOperations**: Refines a set of patch operations to make them more efficient.

##### Example Usage
``` csharp
var sourceJson = JsonDocument.Parse("{\"name\":\"John\",\"age\":30}");
var targetJson = JsonDocument.Parse("{\"name\":\"John\",\"age\":31}");

var patchOperations = JsonPatchGenerator.GeneratePatchOperations(sourceJson, targetJson);
// Results in a patch operation to replace the "age" value
```
##### Edge Cases
- Complex array differences may result in suboptimal patch operations
- Deep nested structures might generate verbose patches

### JsonSorter
_Provides functionality to sort JSON objects by their property names._
#### Overview
`JsonSorter` is a utility class that creates normalized JSON representations by consistently ordering object properties. While JSON property ordering is semantically insignificant according to the specification, having a standardized ordering provides numerous benefits in various scenarios.
#### Key Features
- Lexicographical sorting of JSON object properties at all nesting levels
- Preservation of original data types and values
- Recursive processing of nested objects and arrays
- Optional pretty-printing with indentation
- High-performance implementation using System.Text.Json

#### Use Cases
- Creating canonical representations for cryptographic operations and digital signatures
- Enabling meaningful comparisons between JSON objects
- Generating consistent hash values from JSON data
- Improving readability of JSON data for debugging or documentation
- Ensuring deterministic serialization for testing and verification
- Normalizing JSON before storage or transmission
- Facilitating consistent diff generation between JSON versions

#### Methods
##### SortJson(string json, bool indented = false)
Normalizes a JSON string by sorting all object properties in lexicographical order throughout the entire document.
##### Parameters
- **json**: The input JSON string to be sorted.
- **indented**: Optional boolean indicating whether to format the output with indentation (default is false).

###### Returns
A normalized JSON string with sorted properties.
###### Exceptions
- **ArgumentNullException**: Thrown when the json parameter is null.
- **JsonParsingException**: Thrown when the input is not valid JSON.
- **JsonSortingException**: Thrown when an error occurs during the sorting process.

#### Examples
##### Basic Sorting
``` csharp
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
##### Nested Objects and Arrays
``` csharp
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
##### Canonicalization for Cryptographic Operations
``` csharp
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
#### Behavior Details
##### Property Ordering
Properties are sorted using lexicographical (dictionary) ordering of property names. This means:
- "a" comes before "b", which comes before "c"
- Lowercase letters come after uppercase letters ("Z" comes before "a")
- Numeric characters come before alphabetic characters ("1" comes before "A")

##### Nested Objects
All objects at any level of nesting will have their properties sorted:
``` csharp
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
##### Arrays
Array elements maintain their original order, as sequence is significant in JSON arrays:
``` csharp
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
``` csharp
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
##### Data Types
All JSON data types are preserved during the sorting process:
- Objects
- Arrays
- Strings
- Numbers
- Booleans (true/false)
- null

#### Formatting Options
##### Compact Output (Default)
By default, the sorted JSON is returned without whitespace, maximizing compactness:
``` csharp
string compact = JsonSorter.SortJson(json);
// {"name":"Example","values":[1,2,3]}
```
##### Indented Output
For improved human readability, you can request indented formatting:
``` csharp
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
#### Performance Considerations
- The implementation uses System.Text.Json for efficient parsing and serialization
- Time complexity is generally O(n log n) where n is the number of properties
- Memory usage scales with the complexity and size of the JSON document
- For very large documents, be mindful of memory consumption during the sorting process

#### Best Practices
1. **Use for Canonical Representation**: When you need deterministic JSON output for signing, hashing, or comparison
2. **Compare Normalized JSON**: Always normalize JSON before comparing for semantic equality
3. **Don't Depend on Order**: Even though the output is sorted, consuming code should not depend on property order
4. **Benchmark with Large Documents**: Test performance with your expected document sizes
5. **Error Handling**: Always handle potential exceptions, especially with user-provided JSON

#### Related Functionality
- **JsonComparer**: For comparing JSON documents semantically rather than by exact string matching
- **JsonMerger**: For combining multiple JSON documents with strategies for resolving conflicts
- **JsonMinifier**: For removing unnecessary whitespace from JSON documents
- **JsonDiffer**: For generating difference reports between JSON documents

By employing JsonSorter for JSON normalization, you can achieve consistent representations, reliable comparisons, and deterministic processing of JSON data throughout your application.

### JsonPathQuery
_Provides powerful JSON document querying capabilities using JSONPath expressions._
#### Overview
`JsonPathQuery` is a utility class for querying JSON documents using JSONPath syntax. It provides a flexible way to extract data from complex JSON structures without having to manually navigate through the object hierarchy. The implementation includes performance optimizations such as caching and structured parsing.
#### Key Features
- Query JSON documents using standardized JSONPath syntax
- Extract single values or collections of matching nodes
- Performance-optimized with configurable caching
- Support for complex traversal paths including wildcards and recursive descent
- Ability to navigate and filter arrays
- Error handling with try/catch patterns

#### JSONPath Operators
JSONPath uses a syntax similar to XPath for XML but adapted for JSON's structure. Here are all the supported operators:
##### Root Object Operator: `$`
Represents the root object of the JSON document.
``` 
$.store.book[0].title
```
_Selects the title of the first book in the store._
##### Child Operator: `.`
Accesses a direct child property of an object.
``` 
$.store.book.title
```
_Selects all title properties of all books in the store._
##### Recursive Descent Operator: `..`
Searches for all instances of a specified property at any depth in the document.
``` 
$..title
```
_Finds all title properties anywhere in the document, regardless of nesting level._
##### Wildcard Operator: `*`
Matches any property name or array index.
``` 
$.store.book[*].author
```
_Selects authors of all books in the store._
``` 
$.store.*
```
_Selects all properties directly under store (e.g., book, bicycle)._
##### Array Index Operator: `[n]`
Selects the element at the specified index in an array (zero-based).
``` 
$.store.book[0]
```
_Selects the first book in the array._
##### Array Slice Operator: `[start:end:step]`
Selects a range of elements from an array.
``` 
$.store.book[0:2]
```
_Selects the first two books (indexes 0 and 1)._
``` 
$.store.book[1:4:2]
```
_Selects books at indexes 1 and 3 (starting at 1, ending before 4, stepping by 2)._
##### Array Indices Operator: `[n,m,...]`
Selects multiple specific elements from an array.
``` 
$.store.book[0,2]
```
_Selects the first and third books (indexes 0 and 2)._
##### Filter Expression Operator: `[?(@.property op value)]`
Filters elements based on a condition.
``` 
$.store.book[?(@.price < 10)]
```
_Selects all books with price less than 10._
Available comparison operators:
- `==` (equality)
- `!=` (inequality)
- `<` (less than)
- `<=` (less than or equal to)
- `>` (greater than)
- `>=` (greater than or equal to)
- `=~` (regular expression match)

##### Script Expression Operator: `[(expression)]`
Evaluates a script expression to determine which elements to select.
``` 
$.store.book[(@.length-1)]
```
_Selects the last book in the array._
##### Union Operator: `[property1,property2]`
Combines results from multiple paths.
``` 
$.store.book[0]['title','author']
```
_Selects both the title and author of the first book._
#### Methods
##### QueryJson(string json, string jsonPath)
Executes a JSONPath query against a JSON string and returns matching elements.
###### Parameters
- **json**: The input JSON string to query.
- **jsonPath**: The JSONPath expression to evaluate.

###### Returns
An enumerable collection of JsonElement objects that match the JSONPath expression.
###### Example
``` csharp
// Query for all book titles
string json = @"{
  ""store"": {
    ""book"": [
      { ""title"": ""The Great Gatsby"", ""price"": 8.99 },
      { ""title"": ""Moby Dick"", ""price"": 12.99 }
    ]
  }
}";

var titles = JsonPathQuery.QueryJson(json, "$..title");
foreach (var title in titles) {
    Console.WriteLine(title);
}
// Output:
// "The Great Gatsby"
// "Moby Dick"
```
##### TryQueryJson(string json, string jsonPath, out IEnumerable<JsonElement?> results)
Attempts to execute a JSONPath query with error handling.
###### Parameters
- **json**: The input JSON string to query.
- **jsonPath**: The JSONPath expression to evaluate.
- **results**: Output parameter that receives the query results if successful.

###### Returns
A boolean indicating whether the query was executed successfully.
###### Example
``` csharp
string json = @"{ ""users"": [{ ""name"": ""John"" }, { ""name"": ""Jane"" }] }";

if (JsonPathQuery.TryQueryJson(json, "$.users[?(@.name == 'John')]", out var johnUsers)) {
    foreach (var user in johnUsers) {
        Console.WriteLine($"Found user: {user}");
    }
} else {
    Console.WriteLine("Failed to execute query");
}
```
##### Cache Management Methods
###### ConfigureCache(int maxCacheSize = 1000, TimeSpan? cacheExpiration = null)
Configures the cache settings for parsed JSONPath expressions.
###### Parameters
- **maxCacheSize**: Maximum number of parsed JSONPath expressions to cache.
- **cacheExpiration**: Optional time after which cache entries expire.

###### Example
``` csharp
// Configure cache to store up to 500 parsed paths with a 10-minute expiration
JsonPathQuery.ConfigureCache(500, TimeSpan.FromMinutes(10));
```
###### ClearCache(), RemoveCacheEntry(string jsonPath), TrimCache()
Methods for managing the JSONPath expression cache.
#### Example Queries
##### Basic Property Access
``` csharp
// Get a specific property
var title = JsonPathQuery.QueryJson(bookJson, "$.store.book[0].title").FirstOrDefault();

// Get multiple properties
var authors = JsonPathQuery.QueryJson(bookJson, "$..author");
```
##### Array Operations
``` csharp
// Get specific array element
var firstBook = JsonPathQuery.QueryJson(bookJson, "$.store.book[0]");

// Get array slice
var firstTwoBooks = JsonPathQuery.QueryJson(bookJson, "$.store.book[0:2]");

// Get multiple specific elements
var selectedBooks = JsonPathQuery.QueryJson(bookJson, "$.store.book[0,2]");
```
##### Filtering
``` csharp
// Filter books by price
var cheapBooks = JsonPathQuery.QueryJson(bookJson, "$.store.book[?(@.price < 10)]");

// Filter books by existence of a property
var booksWithIsbn = JsonPathQuery.QueryJson(bookJson, "$.store.book[?(@.isbn)]");

// Complex filtering with multiple conditions
var filteredBooks = JsonPathQuery.QueryJson(bookJson, 
    "$.store.book[?(@.price < 20 && @.category == 'fiction')]");
```
##### Recursive Searching
``` csharp
// Find all prices anywhere in the document
var allPrices = JsonPathQuery.QueryJson(storeJson, "$..price");

// Find all items with a specific category at any level
var fictionItems = JsonPathQuery.QueryJson(storeJson, "$..[?(@.category == 'fiction')]");
```
##### Combining Operators
``` csharp
// Get the last author of books with price > 10
var expensiveLastAuthor = JsonPathQuery.QueryJson(bookJson, 
    "$.store.book[?(@.price > 10)][-1].author");

// Get all titles from the first 3 books that have an ISBN
var titlesWithIsbn = JsonPathQuery.QueryJson(bookJson, 
    "$.store.book[?(@.isbn)][0:3].title");
```
#### Error Handling
JsonPathQuery provides robust error handling for common issues:
- **Invalid JSONPath syntax**: Returns an empty result set or throws an exception
- **Malformed JSON**: Throws a JsonParsingException
- **Path not found**: Returns an empty result set (no exception)
- **Type mismatches**: Handles gracefully based on JSONPath specification

#### Best Practices
1. **Use TryQuery methods** for better error handling in production code
2. **Configure cache appropriately** based on your application's usage patterns
3. **Validate JSONPath syntax** before executing on large documents
4. **Be careful with recursive descent** (`..`) on large documents as it can be performance-intensive
5. **Consider extracting complex queries** into named constants or configuration for maintainability

#### Performance Considerations
- The query performance depends on document size and query complexity
- Recursive descent (`..`) and filter expressions are more expensive than direct path access
- Caching parsed JSONPath expressions improves performance for repeated queries
- Consider extracting smaller portions of large documents before applying complex queries

JsonPathQuery provides a powerful tool for navigating and extracting data from JSON documents using a concise and expressive syntax, allowing you to focus on the data you need rather than the mechanics of traversing complex JSON structures.

### JsonPointer
_Implements RFC 6901 JSON Pointer for precise location referencing within JSON documents._
#### Overview
`JsonPointer` is a utility class that provides functionality for referencing specific values within JSON documents using [RFC 6901 JSON Pointer](https://tools.ietf.org/html/rfc6901) syntax. JSON Pointer defines a string syntax for identifying a specific value within a JSON document, similar to how XPath works for XML but with a simpler syntax designed specifically for JSON's structure.
#### Key Features
- Standards-compliant implementation following RFC 6901
- Precise targeting of values at any level in a JSON document
- Support for array indexing and property access
- Escape handling for special characters
- Error handling with try/catch patterns
- Helper methods for pointer construction

#### JSON Pointer Syntax
JSON Pointer uses a concise syntax to navigate through JSON documents:
- A JSON Pointer starts with a forward slash `/` character
- Each segment between slashes represents a property name or an array index
- A pointer to the whole document is an empty string
- Special characters in property names are escaped using `~` followed by a replacement character:
  - `~0` represents `~` (tilde)
  - `~1` represents `/` (forward slash)

- Array indices are zero-based and represented as numbers

##### Examples of JSON Pointer Syntax
Given this JSON document:
``` json
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

#### Methods
##### EvaluatePointer
``` csharp
public static JsonElement? EvaluatePointer(this JsonDocument document, string pointer)
```
Evaluates a JSON Pointer against a JsonDocument and returns the referenced element.
###### Parameters
- **document**: The JsonDocument to evaluate the pointer against
- **pointer**: The JSON Pointer string that identifies the target location

###### Returns
- A JsonElement? representing the value at the specified location, or null if not found

###### Exceptions
- **ArgumentNullException**: Thrown when the document is null
- **JsonPointerSyntaxException**: Thrown when the pointer string has invalid syntax
- **JsonPointerResolutionException**: Thrown when the pointer can't be resolved (e.g., property doesn't exist)

###### Example
``` csharp
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
##### TryEvaluatePointer
``` csharp
public static bool TryEvaluatePointer(this JsonDocument? document, string pointer, out JsonElement? result)
```
Attempts to evaluate a JSON Pointer against a JsonDocument with error handling.
###### Parameters
- **document**: The JsonDocument to evaluate the pointer against
- **pointer**: The JSON Pointer string that identifies the target location
- **result**: Output parameter that receives the value at the specified location if found

###### Returns
- Boolean indicating whether the pointer was successfully evaluated

###### Example
``` csharp
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
##### Create
``` csharp
public static string Create(params string[] segments)
```
Creates a JSON Pointer string from the provided path segments.
###### Parameters
- **segments**: An array of string segments representing the path components

###### Returns
- A properly formatted and escaped JSON Pointer string

###### Example
``` csharp
// Create a pointer to a nested property
string pointer = JsonPointer.Create("person", "address", "street");
Console.WriteLine(pointer); // Output: /person/address/street

// Create a pointer with special characters that need escaping
string pointerWithSpecial = JsonPointer.Create("products", "category/type", "price");
Console.WriteLine(pointerWithSpecial); // Output: /products/category~1type/price
```
##### Append
``` csharp
public static string Append(string pointer, string segment)
```
Appends a new segment to an existing JSON Pointer string.
###### Parameters
- **pointer**: The existing JSON Pointer string
- **segment**: The new segment to append

###### Returns
- A new JSON Pointer string with the segment appended

###### Example
``` csharp
// Start with a base pointer
string basePointer = "/data/users";

// Append a user ID
string userPointer = JsonPointer.Append(basePointer, "12345");
Console.WriteLine(userPointer); // Output: /data/users/12345

// Append a property name to access a specific field
string emailPointer = JsonPointer.Append(userPointer, "email");
Console.WriteLine(emailPointer); // Output: /data/users/12345/email
```
#### Common Use Cases
##### Document Navigation
``` csharp
// Navigate to deeply nested elements
var config = document.EvaluatePointer("/settings/security/authentication/providers/0/config");
```
##### Array Manipulation
``` csharp
// Add an item at the end of an array
int arrayLength = document.EvaluatePointer("/items")?.GetArrayLength() ?? 0;
string newItemPointer = JsonPointer.Create("items", arrayLength.ToString());
```
##### Dynamic Access Paths
``` csharp
// Build pointers programmatically
string userId = "user123";
string field = "profile";
string dynamicPointer = JsonPointer.Create("users", userId, field);
var userProfile = document.EvaluatePointer(dynamicPointer);
```
##### Combined with JSON Patch Operations
``` csharp
// Define a JSON Patch operation targeting a specific location
var patchOperation = new
{
    op = "replace",
    path = JsonPointer.Create("person", "age"),
    value = 31
};
```
#### JSON Pointer vs. JSONPath
While both are ways to reference locations in JSON documents, they serve different purposes:

| Feature | JSON Pointer | JSONPath |
| --- | --- | --- |
| Purpose | Precise targeting of a single value | Query language for selecting multiple values |
| Syntax | Concise slash-separated path | XPath-like syntax with operators |
| Result | Single value or null | Collection of matching values |
| Use Case | Exact references, patches | Searching, filtering, data extraction |
| Standard | RFC 6901 | De facto standard with variations |
``` csharp
// JSON Pointer - gets one specific element
var specificField = document.EvaluatePointer("/users/0/email");

// JSONPath - can select multiple elements matching criteria
var allEmails = JsonPathQuery.QueryJson(jsonString, "$.users[*].email");
```
#### Error Handling
##### Common Errors
1. **Invalid Pointer Syntax**: If the pointer doesn't follow RFC 6901 syntax
2. **Non-existent Property**: When a property in the path doesn't exist
3. **Index Out of Bounds**: When an array index exceeds the available elements
4. **Type Mismatch**: When trying to navigate through a primitive value

##### Best Practice
Always use the `TryEvaluatePointer` method when working with untrusted input or when you need to handle missing values gracefully:
``` csharp
if (!document.TryEvaluatePointer(userProvidedPath, out var result))
{
    Console.WriteLine("Path not found or invalid");
    return;
}

// Safe to use result here
```
#### Performance Considerations
- JSON Pointer is evaluated in a single pass through the document
- Time complexity is O(n) where n is the number of segments in the pointer
- For repeatedly accessing the same document, consider caching the JsonDocument
- When working with large documents, consider using streaming approaches for initial parsing

#### Real-World Examples
##### Configuration Access
``` csharp
// Access specific configuration settings
var dbConnectionString = config.EvaluatePointer("/database/connectionStrings/default");
var logLevel = config.EvaluatePointer("/logging/level");
```
##### API Response Processing
``` csharp
// Extract specific data from an API response
var pageCount = apiResponse.EvaluatePointer("/meta/pagination/pages");
var firstResultId = apiResponse.EvaluatePointer("/data/0/id");
```
##### Form Data Validation
``` csharp
// Check if a specific field has validation errors
if (validationResult.TryEvaluatePointer("/errors/email", out var emailErrors))
{
    foreach (var error in emailErrors.EnumerateArray())
    {
        Console.WriteLine($"Email error: {error.GetString()}");
    }
}
```
#### Best Practices
1. **Use `Create` and `Append` methods** to build pointers programmatically rather than string concatenation
2. **Use `TryEvaluatePointer`** for better error handling in production code
3. **Validate user input** before using it to construct JSON Pointers
4. **Consider case sensitivity** as JSON property names are case-sensitive
5. **Handle null results** appropriately when querying for optional fields

By understanding and using JSON Pointer effectively, you can precisely target and manipulate specific parts of JSON documents with minimal code and maximum clarity.

### JsonStreamer

_A memory-efficient utility for processing large JSON files by streaming tokens._

#### Overview

`JsonStreamer` is a static utility class that enables processing JSON data in a streaming fashion without loading the entire content into memory. This makes it ideal for working with large JSON files or in memory-constrained environments. By processing JSON token-by-token, it allows you to work with files that would otherwise be impractical to handle using traditional deserialization approaches.

#### Key Features

- Memory-efficient JSON processing through streaming
- Token-by-token callback mechanism
- Handles tokens that span across buffer boundaries
- Works with files or any stream source
- Built-in performance monitoring
- Thorough error handling with detailed exceptions
- Support for filtered token processing

#### When to Use JsonStreamer

- Working with very large JSON files (hundreds of MB or GB)
- Memory-constrained environments (mobile, embedded, or containerized applications)
- When you only need to extract specific data from large JSON files
- Implementing progressive parsing for UI responsiveness
- Processing streaming data sources

#### Core Methods

##### StreamJsonFile

```csharp
public static void StreamJsonFile(this string filePath, Action<JsonTokenType, string?> callback)
```

Streams JSON data from a file and invokes the callback for each JSON token.

###### Parameters
- **filePath**: Path to the JSON file
- **callback**: Action that will be invoked for each token with the token type and value

###### Exceptions
- **ArgumentNullException**: When filePath or callback is null
- **FileNotFoundException**: When the specified file doesn't exist
- **JsonOperationException**: When the streaming operation fails
- **JsonLibException**: When a general error occurs during streaming

###### Example

```csharp
// Count objects in a large JSON array
string filePath = "massive-data.json";
int objectCount = 0;

filePath.StreamJsonFile((tokenType, tokenValue) => {
    // Increment counter whenever we find the start of an object
    if (tokenType == JsonTokenType.StartObject) {
        objectCount++;
    }
});

Console.WriteLine($"Found {objectCount} objects in the JSON file");
```

##### StreamJson (Stream Extension)

```csharp
public static void StreamJson(this Stream jsonStream, Action<JsonTokenType, string?> callback)
```

Streams JSON data from any Stream source and invokes the callback for each token.

###### Parameters
- **jsonStream**: Source Stream containing JSON data
- **callback**: Action that will be invoked for each token with the token type and value

###### Exceptions
- **ArgumentNullException**: When jsonStream or callback is null
- **JsonOperationException**: When the streaming operation fails

###### Example

```csharp
// Process JSON response from an HTTP request
using HttpClient client = new HttpClient();
using Stream responseStream = await client.GetStreamAsync("https://api.example.com/large-dataset");

// Extract only specific fields
var extractedData = new Dictionary<string, string>();
string? currentProperty = null;

responseStream.StreamJson((tokenType, tokenValue) => {
    if (tokenType == JsonTokenType.PropertyName) {
        currentProperty = tokenValue;
    }
    else if (tokenType == JsonTokenType.String && 
            (currentProperty == "id" || currentProperty == "name")) {
        extractedData[currentProperty] = tokenValue ?? "";
    }
});
```

##### StreamFilteredTokens

```csharp
public static void StreamFilteredTokens(
    this Stream jsonStream, 
    Func<JsonTokenType, string?, bool> filter, 
    Action<JsonTokenType, string?> callback)
```

Streams JSON data and invokes the callback only for tokens that match the filter condition.

###### Parameters
- **jsonStream**: Source Stream containing JSON data
- **filter**: Function that determines which tokens to process
- **callback**: Action that will be invoked for matching tokens

###### Example

```csharp
// Process only numeric values in a JSON file
string filePath = "metrics.json";
var numericValues = new List<double>();

using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
fileStream.StreamFilteredTokens(
    // Filter: only process numbers
    (tokenType, _) => tokenType == JsonTokenType.Number,
    
    // Callback: parse and collect numeric values
    (_, tokenValue) => {
        if (double.TryParse(tokenValue, out double value)) {
            numericValues.Add(value);
        }
    }
);

// Calculate statistics
Console.WriteLine($"Count: {numericValues.Count}");
Console.WriteLine($"Average: {numericValues.Average()}");
Console.WriteLine($"Max: {numericValues.Max()}");
```

#### Common Use Cases

##### Parsing Large Data Sets

```csharp
// Extract specific data from a multi-gigabyte JSON file
string hugeFilePath = "logs-archive.json";
var relevantEntries = new List<LogEntry>();

hugeFilePath.StreamJsonFile((tokenType, tokenValue) => {
    // Use a state machine to track current position and extract relevant data
    if (StateMachine.IsInLogEntry && tokenType == JsonTokenType.PropertyName) {
        if (tokenValue == "level" && NextTokenIs("ERROR")) {
            StateMachine.MarkCurrentEntryRelevant();
        }
        else if (tokenValue == "timestamp" && NextTokenIsAfter(DateTime.Now.AddDays(-1))) {
            StateMachine.MarkCurrentEntryRelevant();
        }
    }
    else if (tokenType == JsonTokenType.EndObject && StateMachine.IsEntryRelevant) {
        relevantEntries.Add(StateMachine.BuildCurrentEntry());
        StateMachine.Reset();
    }
});
```

##### Progressive UI Updates

```csharp
// Process a large JSON file while updating UI progress
public async Task ProcessLargeFileWithProgressAsync(string filePath, IProgress<int> progress)
{
    int totalTokens = 0;
    int processedTokens = 0;
    
    // First pass: count total tokens for progress calculation
    filePath.StreamJsonFile((_, _) => totalTokens++);
    
    // Second pass: actual processing with progress updates
    filePath.StreamJsonFile((tokenType, tokenValue) => {
        ProcessToken(tokenType, tokenValue);
        
        processedTokens++;
        if (processedTokens % 1000 == 0) {
            int percentage = (int)(processedTokens * 100.0 / totalTokens);
            progress.Report(percentage);
        }
    });
}
```

##### JSON Validation

```csharp
// Validate a JSON file without loading it completely
public bool ValidateJsonStructure(string filePath) 
{
    try {
        int objectDepth = 0;
        int arrayDepth = 0;
        bool hasRootElement = false;
        
        filePath.StreamJsonFile((tokenType, _) => {
            if (tokenType == JsonTokenType.StartObject) {
                if (objectDepth == 0 && arrayDepth == 0) {
                    hasRootElement = true;
                }
                objectDepth++;
            }
            else if (tokenType == JsonTokenType.EndObject) {
                objectDepth--;
            }
            else if (tokenType == JsonTokenType.StartArray) {
                if (objectDepth == 0 && arrayDepth == 0) {
                    hasRootElement = true;
                }
                arrayDepth++;
            }
            else if (tokenType == JsonTokenType.EndArray) {
                arrayDepth--;
            }
        });
        
        return hasRootElement && objectDepth == 0 && arrayDepth == 0;
    }
    catch (JsonOperationException) {
        return false;
    }
}
```

##### Data Transformation

```csharp
// Transform a large JSON file to CSV without loading the entire JSON
public void ConvertJsonToCsv(string jsonFilePath, string csvFilePath)
{
    using var writer = new StreamWriter(csvFilePath);
    
    // State for tracking the current object being processed
    var currentRow = new Dictionary<string, string>();
    string currentProperty = null;
    bool inObject = false;
    bool headerWritten = false;
    
    jsonFilePath.StreamJsonFile((tokenType, tokenValue) => {
        if (tokenType == JsonTokenType.StartObject) {
            inObject = true;
            currentRow.Clear();
        }
        else if (tokenType == JsonTokenType.EndObject && inObject) {
            // Write header row if first object
            if (!headerWritten) {
                writer.WriteLine(string.Join(",", currentRow.Keys));
                headerWritten = true;
            }
            
            // Write data row
            writer.WriteLine(string.Join(",", 
                currentRow.Values.Select(v => $"\"{v.Replace("\"", "\"\"")}\"")
            ));
            
            inObject = false;
        }
        else if (tokenType == JsonTokenType.PropertyName && inObject) {
            currentProperty = tokenValue;
        }
        else if ((tokenType == JsonTokenType.String || 
                 tokenType == JsonTokenType.Number || 
                 tokenType == JsonTokenType.True ||
                 tokenType == JsonTokenType.False) && 
                 inObject && currentProperty != null) {
            currentRow[currentProperty] = tokenValue ?? "";
        }
    });
}
```

##### Working with Streaming APIs

```csharp
// Process a continuous server-sent events stream of JSON objects
async Task ProcessServerSentEvents(CancellationToken cancellationToken)
{
    using HttpClient client = new HttpClient();
    client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite); // No timeout
    
    using var response = await client.GetAsync(
        "https://api.example.com/events-stream", 
        HttpCompletionOption.ResponseHeadersRead, 
        cancellationToken);
    
    using var stream = await response.Content.ReadAsStreamAsync();
    
    // Use JsonStreamer to process the continuous stream
    stream.StreamJson((tokenType, tokenValue) => {
        if (cancellationToken.IsCancellationRequested) {
            throw new OperationCanceledException();
        }
        
        // Process JSON tokens from the stream
        if (tokenType == JsonTokenType.PropertyName && tokenValue == "event") {
            // Prepare to read the event type
        }
    });
}
```

#### Performance Considerations

1. **Buffer Size**: The default buffer size is 4096 bytes, which balances memory usage and I/O operations. You can customize this for specific scenarios.

2. **Callback Overhead**: Keep the token processing callback lightweight, especially for very large files. Consider batching operations or deferring heavy processing.

3. **Memory Profile**: JsonStreamer is designed for low memory usage, but your callback might still accumulate data. Be mindful of what you store during streaming.

4. **Filtered Processing**: Use `StreamFilteredTokens` when you only need a subset of tokens to avoid unnecessary callback invocations.

5. **State Management**: Managing state within callbacks requires careful design. Consider using a dedicated state object to track context during streaming.

#### Best Practices

##### Implement Proper State Management

```csharp
// Create a state machine for tracking context during streaming
public class JsonStreamingState
{
    public Stack<string> PropertyPath { get; } = new Stack<string>();
    public bool InArray { get; set; }
    public int ArrayDepth { get; set; }
    public int ObjectDepth { get; set; }
    
    // Track the current path in dot notation
    public string CurrentPath => string.Join(".", PropertyPath.Reverse());
    
    public void HandleToken(JsonTokenType tokenType, string tokenValue)
    {
        if (tokenType == JsonTokenType.PropertyName)
            PropertyPath.Push(tokenValue);
        else if (tokenType == JsonTokenType.StartObject)
            ObjectDepth++;
        else if (tokenType == JsonTokenType.EndObject) {
            ObjectDepth--;
            if (ObjectDepth >= 0 && PropertyPath.Count > 0)
                PropertyPath.Pop();
        }
        // Handle other token types...
    }
}
```

##### Error Handling

```csharp
try
{
    filePath.StreamJsonFile((tokenType, tokenValue) => {
        try {
            // Process token
        }
        catch (Exception ex) {
            // Log the error but allow streaming to continue
            Logger.LogError(ex, "Error processing token {TokenType}", tokenType);
        }
    });
}
catch (JsonOperationException ex)
{
    // Handle streaming failure
    Logger.LogError(ex, "JSON streaming failed");
}
```

##### Maintaining Context

```csharp
// Use class fields to maintain context between callbacks
public class JsonProcessor
{
    private readonly Stack<string> _path = new();
    private readonly Dictionary<string, object> _result = new();
    
    public Dictionary<string, object> Process(string filePath)
    {
        filePath.StreamJsonFile(HandleToken);
        return _result;
    }
    
    private void HandleToken(JsonTokenType tokenType, string tokenValue)
    {
        // Update path and build result based on token context
        // This maintains state between callback invocations
    }
}
```

##### Batching For Performance

```csharp
// Batch processed items for bulk operations
public void ProcessLargeJsonWithBatching(string filePath)
{
    const int batchSize = 1000;
    var batch = new List<Item>(batchSize);
    
    filePath.StreamJsonFile((tokenType, tokenValue) => {
        // Extract items from the JSON stream
        Item item = ExtractItemFromToken(tokenType, tokenValue);
        
        if (item != null) {
            batch.Add(item);
            
            // When batch is full, process and clear it
            if (batch.Count >= batchSize) {
                ProcessBatch(batch);
                batch.Clear();
            }
        }
    });
    
    // Process any remaining items
    if (batch.Count > 0) {
        ProcessBatch(batch);
    }
}
```

#### Advanced Use Cases

##### Building a JSON Query Engine

```csharp
// Extract data based on a JSONPath-like query
public List<string> QueryJson(string filePath, string jsonPath)
{
    var pathSegments = ParseJsonPath(jsonPath);
    var results = new List<string>();
    var currentPath = new Stack<string>();
    bool collectNextValue = false;
    
    filePath.StreamJsonFile((tokenType, tokenValue) => {
        // Update current path based on token type
        UpdatePath(tokenType, tokenValue, currentPath);
        
        // Check if current path matches query path
        if (PathMatches(currentPath, pathSegments)) {
            collectNextValue = true;
        }
        else if (collectNextValue && IsValueToken(tokenType)) {
            results.Add(tokenValue);
            collectNextValue = false;
        }
    });
    
    return results;
}
```

##### Building a Streaming JSON Transformer

```csharp
// Transform JSON while streaming
public async Task TransformJson(string inputPath, string outputPath, 
                               Func<JsonTokenType, string, (JsonTokenType, string)> transformer)
{
    using var writer = new StreamWriter(outputPath);
    using var jsonWriter = new Utf8JsonWriter(writer.BaseStream);
    
    inputPath.StreamJsonFile((tokenType, tokenValue) => {
        // Apply transformation
        var (newType, newValue) = transformer(tokenType, tokenValue);
        
        // Write transformed token
        WriteToken(jsonWriter, newType, newValue);
    });
    
    await jsonWriter.FlushAsync();
}
```

##### Event-based Processing Model

```csharp
// Implement an event-based processing model
public class JsonStreamProcessor
{
    public event EventHandler<JsonTokenEventArgs> TokenProcessed;
    public event EventHandler<JsonErrorEventArgs> ErrorOccurred;
    public event EventHandler StreamingCompleted;
    
    public async Task ProcessAsync(string filePath, CancellationToken cancellationToken)
    {
        try {
            filePath.StreamJsonFile((tokenType, tokenValue) => {
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }
                
                try {
                    TokenProcessed?.Invoke(this, new JsonTokenEventArgs(tokenType, tokenValue));
                }
                catch (Exception ex) {
                    ErrorOccurred?.Invoke(this, new JsonErrorEventArgs(ex));
                }
            });
            
            StreamingCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) {
            ErrorOccurred?.Invoke(this, new JsonErrorEventArgs(ex));
        }
    }
}
```

##### Statistical Analysis of JSON Structure

```csharp
// Analyze the structure of a JSON document
public JsonDocumentStatistics AnalyzeJsonStructure(string filePath)
{
    var stats = new JsonDocumentStatistics();
    
    filePath.StreamJsonFile((tokenType, tokenValue) => {
        stats.TotalTokens++;
        
        switch (tokenType) {
            case JsonTokenType.StartObject:
                stats.ObjectCount++;
                stats.CurrentDepth++;
                stats.MaxDepth = Math.Max(stats.MaxDepth, stats.CurrentDepth);
                break;
                
            case JsonTokenType.EndObject:
                stats.CurrentDepth--;
                break;
                
            case JsonTokenType.StartArray:
                stats.ArrayCount++;
                stats.CurrentDepth++;
                stats.MaxDepth = Math.Max(stats.MaxDepth, stats.CurrentDepth);
                break;
                
            case JsonTokenType.EndArray:
                stats.CurrentDepth--;
                break;
                
            case JsonTokenType.PropertyName:
                stats.PropertyCount++;
                stats.PropertyNameLengths.Add(tokenValue?.Length ?? 0);
                break;
                
            case JsonTokenType.String:
                stats.StringCount++;
                stats.StringValueLengths.Add(tokenValue?.Length ?? 0);
                break;
                
            case JsonTokenType.Number:
                stats.NumberCount++;
                break;
        }
    });
    
    return stats;
}
```

JsonStreamer provides an efficient way to process large JSON files while maintaining low memory usage, making it possible to work with data sizes that would otherwise be impractical with traditional JSON parsing approaches.

## Common Edge Cases
1. **Large Documents**: Some operations may have performance impacts on extremely large JSON documents.
2. **Circular References**: JSON documents with circular references are not supported and may cause infinite loops.
3. **Special Characters**: JSON paths containing special characters may require proper escaping.
4. **Schema Changes**: Operations that expect a certain structure may fail if the schema changes unexpectedly.
5. **Numeric Precision**: Be mindful of potential precision loss when working with floating-point numbers.

## Best Practices
1. **Validate Input**: Always validate JSON input before performing operations.
2. **Error Handling**: Implement proper error handling for malformed JSON.
3. **Memory Management**: Consider using streaming approaches for very large documents.
4. **Testing**: Test operations with edge cases, including empty objects, arrays, and null values.


