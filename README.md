# Another Json Library

[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/JacquesBronk/another-json-lib/blob/main/LICENSE) [![Build & Test](https://github.com/JacquesBronk/another-json-lib/actions/workflows/build-test.yaml/badge.svg)](https://github.com/JacquesBronk/another-json-lib/actions/workflows/build-test.yaml) [![Unit Test Status](https://gist.github.com/JacquesBronk/583f3a5e64e34c4125c923404dfa921f/raw/another_json_lib_tests.md_badge.svg)](https://gist.github.com/JacquesBronk/583f3a5e64e34c4125c923404dfa921f) [![CodeQL](https://github.com/JacquesBronk/another-json-lib/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/JacquesBronk/another-json-lib/actions/workflows/github-code-scanning/codeql)


## Overview

**Another Json Library** is a powerful collection of utility classes and methods for working with JSON data in C#. It empowers developers with a versatile set of tools to streamline JSON handling, including serialization, deserialization, comparison, merging, minification, and efficient token-based processing.

## Features

- **Serialization & Deserialization:** Easily serialize C# objects to JSON strings and deserialize JSON strings to C# objects.

- **Comparison:** Compare JSON strings for equality, optionally ignoring case and whitespace differences.

- **JSON Differences:** Find differences between two JSON strings represented as dictionaries.

- **JSON Pointer Evaluation:** Efficiently evaluate JSON Pointers against JSON documents.

- **JSON Merging:** Merge two JSON strings, giving preference to values from the patch document.

- **Minification:** Minify JSON strings by removing unnecessary whitespace and formatting.

- **Token-Based Processing:** Stream JSON data from files or streams and process tokens efficiently.

## Usage

### ToJson
- **Use Case:** Serialize an object to a JSON string.
- **Example:**
  ```csharp
  var simpleObject = new SimpleObject { Id = 1, Name = "John" };
  string json = simpleObject.ToJson();
  // json now contains '{"Id":1,"Name":"John"}'
  ```
### FromJson
- **Use Case:** Deserialize a JSON string to an object.
- **Example:**
  ```csharp
    string json = '{"Id":1,"Name":"John"}';
    var simpleObject = json.FromJson<SimpleObject>();
    // simpleObject is now an instance of SimpleObject with Id=1 and Name="John"
    ```
### LoggerFactory
- **Use Case:** Create and retrieve logger instances for logging
- **Example:**
  ```csharp
    var loggerFactory = LoggerFactory.Instance;
    var logger = loggerFactory.GetLogger<SerializationTests>();
    logger.LogInformation("This is a log message.");
     ```
### AreEqual
- **Use Case:** Compare two JSON strings for equality
- **Example:**
  ```csharp
    string json1 = '{"Id":1,"Name":"John"}';
    string json2 = '{"Name":"John","Id":1}';
    bool areEqual = json1.AreEqual(json2, ignoreWhitespace: true);
    // areEqual is true, ignoring whitespace and field order
     ```
### StreamJsonFile

- **Use Case:** Stream and process a JSON file token by token.
- **When to Use:**
  - When dealing with large JSON files that may not fit entirely in memory.
  - When you want to process JSON data as it's read from a file, rather than loading the entire file into memory.
  - For scenarios where you need to extract specific information or perform actions on individual JSON tokens.

#### Real-World Example

Consider a log file named `app.log` with the following JSON log entries:

```json
{"timestamp": "2023-10-20T10:15:30", "level": "INFO", "message": "User John logged in."}
{"timestamp": "2023-10-20T10:20:45", "level": "ERROR", "message": "Critical error occurred."}
{"timestamp": "2023-10-20T10:25:12", "level": "INFO", "message": "User Alice logged in."}
```
You want to extract and process the messages of all INFO-level log entries.
**Example Code**
```csharp
string filePath = "app.log";

filePath.StreamJsonFile((tokenType, tokenValue) =>
{
    if (tokenType == JsonTokenType.String && tokenValue != null)
    {
        if (tokenValue.Contains("INFO", StringComparison.OrdinalIgnoreCase))
        {
            // Process the INFO-level log message
            Console.WriteLine($"INFO Log Message: {tokenValue}");
        }
    }
});

```

In this real-world example, the `StreamJsonFile` method reads the log entries one by one and invokes the callback function for each token. When it encounters a string token that contains "INFO," it processes and prints the INFO-level log message.

#### When to Use StreamJsonFile:

- When processing large JSON files or streams efficiently.
- When dealing with real-time data streams where you want to act on JSON tokens as they arrive.
- In scenarios like log analysis, where you need to filter and process specific events from a large log file without loading the entire file into memory.
- By using StreamJsonFile, you can efficiently process JSON data token by token, making it suitable for applications with resource constraints and the need for real-time or selective data processing.

### Merge
- **Use Case:** Merge two JSON strings with overlapping keys
- **Example:**
  ```csharp
    string originalJson = '{"Id":1,"Name":"John"}';
    string patchJson = '{"Name":"Doe"}';
    string mergedJson = originalJson.Merge(patchJson);
    // mergedJson is '{"Id":1,"Name":"Doe"}'
     ```
### Minify
- **Use Case:** Minify a JSON string by removing whitespace and formatting
- **Example:**
  ```csharp
    string json = '{
        "Id": 1,
        "Name": "John"
    }';
    string minifiedJson = json.Minify();
    // minifiedJson is '{"Id":1,"Name":"John"}'
     ```
### StreamJsonFile
- **Use Case:** Minify a JSON string by removing whitespace and formatting
- **When to Use:**
  - When you want to selectively extract or manipulate parts of a JSON structure without deserializing the entire document.
  - In scenarios where you need to perform targeted operations on specific data within a complex JSON document.

#### Real-World Example

Imagine you have a JSON configuration file named config.json with the following structure:

```json
{
  "app": {
    "name": "My App",
    "version": "1.0"
  },
  "database": {
    "connectionString": "..."
  },
  "logging": {
    "level": "INFO",
    "enabled": true
  }
}
```
You want to extract the logging level from this configuration.

```csharp
string json = LoadJsonConfig(); // Load the JSON configuration
string pointer = "/logging/level";
JsonElement? result = json.EvaluatePointer(pointer);

if (result != null)
{
    string logLevel = result.Value.GetString();
    Console.WriteLine($"Logging Level: {logLevel}");
}
```
In this real-world example, the EvaluatePointer method is used to navigate the JSON structure and extract the logging level. It returns a JsonElement? representing the result, which can then be processed further.

#### When to Use EvaluatePointer

- When you need to access specific parts of a JSON document without parsing the entire JSON structure.
- In scenarios like configuration management, where you want to retrieve settings from a JSON configuration file efficiently.
- For data patching or advanced search operations within complex JSON structures.

By using `EvaluatePointer`, you can efficiently navigate and extract data from a JSON document based on a JSON Pointer, making it useful in scenarios where you need to work with specific elements within a large JSON structure without fully deserializing it.
