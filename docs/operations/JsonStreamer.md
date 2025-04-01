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