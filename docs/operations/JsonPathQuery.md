# JsonPathQuery

_Provides powerful JSON document querying capabilities using JSONPath expressions._

## Overview

`JsonPathQuery` is a utility class for querying JSON documents using JSONPath syntax. It provides a flexible way to extract data from complex JSON structures without having to manually navigate through the object hierarchy. The implementation includes performance optimizations such as caching and structured parsing.

## Key Features

- Query JSON documents using standardized JSONPath syntax
- Extract single values or collections of matching nodes
- Performance-optimized with configurable caching
- Support for complex traversal paths including wildcards and recursive descent
- Ability to navigate and filter arrays
- Error handling with try/catch patterns

## JSONPath Operators

JSONPath uses a syntax similar to XPath for XML but adapted for JSON's structure. Here are all the supported operators:

### Root Object Operator: `$`

Represents the root object of the JSON document.

``` 
$.store.book[0].title
```

_Selects the title of the first book in the store._

### Child Operator: `.`

Accesses a direct child property of an object.

``` 
$.store.book.title
```

_Selects all title properties of all books in the store._

### Recursive Descent Operator: `..`

Searches for all instances of a specified property at any depth in the document.

``` 
$..title
```

_Finds all title properties anywhere in the document, regardless of nesting level._

### Wildcard Operator: `*`

Matches any property name or array index.

``` 
$.store.book[*].author
```

_Selects authors of all books in the store._

``` 
$.store.*
```

_Selects all properties directly under store (e.g., book, bicycle)._

### Array Index Operator: `[n]`

Selects the element at the specified index in an array (zero-based).

``` 
$.store.book[0]
```

_Selects the first book in the array._

### Array Slice Operator: `[start:end:step]`

Selects a range of elements from an array.

``` 
$.store.book[0:2]
```

_Selects the first two books (indexes 0 and 1)._

``` 
$.store.book[1:4:2]
```

_Selects books at indexes 1 and 3 (starting at 1, ending before 4, stepping by 2)._

### Array Indices Operator: `[n,m,...]`

Selects multiple specific elements from an array.

``` 
$.store.book[0,2]
```

_Selects the first and third books (indexes 0 and 2)._

### Filter Expression Operator: `[?(@.property op value)]`

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

### Script Expression Operator: `[(expression)]`

Evaluates a script expression to determine which elements to select.

``` 
$.store.book[(@.length-1)]
```

_Selects the last book in the array._

### Union Operator: `[property1,property2]`

Combines results from multiple paths.

``` 
$.store.book[0]['title','author']
```

_Selects both the title and author of the first book._

## Methods

### QueryJson(string json, string jsonPath)

Executes a JSONPath query against a JSON string and returns matching elements.

#### Parameters

- **json**: The input JSON string to query.
- **jsonPath**: The JSONPath expression to evaluate.

#### Returns

An enumerable collection of JsonElement objects that match the JSONPath expression.

#### Example

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

### TryQueryJson(string json, string jsonPath, out IEnumerable<JsonElement?> results)

Attempts to execute a JSONPath query with error handling.

#### Parameters

- **json**: The input JSON string to query.
- **jsonPath**: The JSONPath expression to evaluate.
- **results**: Output parameter that receives the query results if successful.

#### Returns

A boolean indicating whether the query was executed successfully.

#### Example

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

### Cache Management Methods

#### ConfigureCache(int maxCacheSize = 1000, TimeSpan? cacheExpiration = null)

Configures the cache settings for parsed JSONPath expressions.

#### Parameters

- **maxCacheSize**: Maximum number of parsed JSONPath expressions to cache.
- **cacheExpiration**: Optional time after which cache entries expire.

#### Example

``` csharp
// Configure cache to store up to 500 parsed paths with a 10-minute expiration
JsonPathQuery.ConfigureCache(500, TimeSpan.FromMinutes(10));
```

#### ClearCache(), RemoveCacheEntry(string jsonPath), TrimCache()

Methods for managing the JSONPath expression cache.

## Example Queries

### Basic Property Access

``` csharp
// Get a specific property
var title = JsonPathQuery.QueryJson(bookJson, "$.store.book[0].title").FirstOrDefault();

// Get multiple properties
var authors = JsonPathQuery.QueryJson(bookJson, "$..author");
```

### Array Operations

``` csharp
// Get specific array element
var firstBook = JsonPathQuery.QueryJson(bookJson, "$.store.book[0]");

// Get array slice
var firstTwoBooks = JsonPathQuery.QueryJson(bookJson, "$.store.book[0:2]");

// Get multiple specific elements
var selectedBooks = JsonPathQuery.QueryJson(bookJson, "$.store.book[0,2]");
```

### Filtering

``` csharp
// Filter books by price
var cheapBooks = JsonPathQuery.QueryJson(bookJson, "$.store.book[?(@.price < 10)]");

// Filter books by existence of a property
var booksWithIsbn = JsonPathQuery.QueryJson(bookJson, "$.store.book[?(@.isbn)]");

// Complex filtering with multiple conditions
var filteredBooks = JsonPathQuery.QueryJson(bookJson, 
        "$.store.book[?(@.price < 20 && @.category == 'fiction')]");
```

### Recursive Searching

``` csharp
// Find all prices anywhere in the document
var allPrices = JsonPathQuery.QueryJson(storeJson, "$..price");

// Find all items with a specific category at any level
var fictionItems = JsonPathQuery.QueryJson(storeJson, "$..[?(@.category == 'fiction')]");
```

### Combining Operators

``` csharp
// Get the last author of books with price > 10
var expensiveLastAuthor = JsonPathQuery.QueryJson(bookJson, 
        "$.store.book[?(@.price > 10)][-1].author");

// Get all titles from the first 3 books that have an ISBN
var titlesWithIsbn = JsonPathQuery.QueryJson(bookJson, 
        "$.store.book[?(@.isbn)][0:3].title");
```

## Error Handling

JsonPathQuery provides robust error handling for common issues:

- **Invalid JSONPath syntax**: Returns an empty result set or throws an exception
- **Malformed JSON**: Throws a JsonParsingException
- **Path not found**: Returns an empty result set (no exception)
- **Type mismatches**: Handles gracefully based on JSONPath specification

## Best Practices

1. **Use TryQuery methods** for better error handling in production code
2. **Configure cache appropriately** based on your application's usage patterns
3. **Validate JSONPath syntax** before executing on large documents
4. **Be careful with recursive descent** (`..`) on large documents as it can be performance-intensive
5. **Consider extracting complex queries** into named constants or configuration for maintainability

## Performance Considerations

- The query performance depends on document size and query complexity
- Recursive descent (`..`) and filter expressions are more expensive than direct path access
- Caching parsed JSONPath expressions improves performance for repeated queries
- Consider extracting smaller portions of large documents before applying complex queries

`JsonPathQuery` provides a powerful tool for navigating and extracting data from JSON documents using a concise and expressive syntax, allowing you to focus on the data you need rather than the mechanics of traversing complex JSON structures.