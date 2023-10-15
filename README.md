# Another Json Library

[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/JacquesBronk/another-json-lib/blob/main/LICENSE) [![Build & Test](https://github.com/JacquesBronk/another-json-lib/actions/workflows/main-checks.yaml/badge.svg?branch=main&event=status)](https://github.com/JacquesBronk/another-json-lib/actions/workflows/main-checks.yaml)

**Another Json Library** is a collection of utility classes and methods for working with JSON data in C#. It provides a set of tools to streamline JSON handling, including serialization, deserialization, comparison, merging, and more.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Classes](#classes)
- [Contributing](#contributing)
- [License](#license)

## Features

- Serialize C# objects to JSON strings and deserialize JSON strings to C# objects.
- Compare JSON strings for equality, optionally ignoring case and whitespace differences.
- Find differences between two JSON strings represented as dictionaries.
- Evaluate JSON Pointers against JSON documents.
- Merge two JSON strings, giving preference to values from the patch document.
- Minify JSON strings by removing unnecessary whitespace and formatting.
- Stream JSON data from files or streams and process tokens efficiently.

```csharp
using AJL.Utility;

// Serialize an object to JSON
var data = new { Name = "John", Age = 30 };
string json = data.ToJson();

// Deserialize JSON to an object
var deserializedData = json.FromJson<MyClass>();

// Compare JSON strings
bool isEqual = json1.AreEqual(json2, ignoreCase: true, ignoreWhitespace: true);

// Minify JSON string
string minifiedJson = json.Prettify();

// Merge JSON strings
string mergedJson = originalJson.Merge(patchJson);

// Evaluate JSON Pointer
var jsonDocument = JsonDocument.Parse(json);
var result = jsonDocument.EvaluatePointer("/path/to/property");

```
