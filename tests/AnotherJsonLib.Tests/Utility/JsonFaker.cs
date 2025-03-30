using System.Text.Json;
using System.Text.Json.Nodes;
using Bogus;

namespace AnotherJsonLib.Tests.Utility;

/// <summary>
/// Generates various kinds of JSON data for testing purposes
/// </summary>
public class JsonFaker
{
    private readonly Faker _faker;
    private readonly Random _random;

    public JsonFaker(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _faker = seed.HasValue ? new Faker() { Random = new Randomizer(seed.Value) } : new Faker();
    }

    /// <summary>
    /// Generates a simple JSON object with primitive values
    /// </summary>
    public JsonObject GenerateSimpleObject(int propertyCount = 5)
    {
        var jsonObject = new JsonObject();
        var usedPropertyNames = new HashSet<string>(); // Track used property names

        // Add attempts counter and max limit
        int attempts = 0;
        const int maxAttempts = 100; // Prevent infinite loops

        while (jsonObject.Count < propertyCount && attempts < maxAttempts)
        {
            attempts++;

            // Generate a property name
            string basePropertyName = _faker.Database.Column();
            string propertyName = basePropertyName;

            // If we've already used this name, add a unique suffix
            int suffix = 1;
            while (!usedPropertyNames.Add(propertyName))
            {
                propertyName = $"{basePropertyName}_{suffix}";
                suffix++;

                // Safety check - if we've tried too many suffixes, move on
                if (suffix > 20) // Limit suffix attempts
                {
                    propertyName = $"{basePropertyName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                    usedPropertyNames.Add(propertyName);
                    break;
                }
            }

            var propertyType = _random.Next(5);

            switch (propertyType)
            {
                case 0:
                    jsonObject.Add(propertyName, _faker.Random.Bool());
                    break;
                case 1:
                    jsonObject.Add(propertyName, _faker.Random.Number(-1000, 1000));
                    break;
                case 2:
                    jsonObject.Add(propertyName, _faker.Random.Double(-1000, 1000));
                    break;
                case 3:
                    jsonObject.Add(propertyName, _faker.Lorem.Sentence());
                    break;
                case 4:
                    jsonObject.Add(propertyName, null);
                    break;
            }
        }

        return jsonObject;
    }

    /// <summary>
    /// Generates a JSON array with elements of specified type
    /// </summary>
    public JsonArray GenerateArray(int elementCount = 5, JsonValueKind elementType = JsonValueKind.String)
    {
        var array = new JsonArray();

        for (int i = 0; i < elementCount; i++)
        {
            switch (elementType)
            {
                case JsonValueKind.Number:
                    array.Add(_faker.Random.Number(-1000, 1000));
                    break;
                case JsonValueKind.String:
                    array.Add(_faker.Lorem.Word());
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    array.Add(_faker.Random.Bool());
                    break;
                case JsonValueKind.Null:
                    array.Add((JsonNode)null);
                    break;
                case JsonValueKind.Object:
                    array.Add(GenerateSimpleObject(_random.Next(2, 5)));
                    break;
                case JsonValueKind.Array:
                    var nestedArray = new JsonArray();
                    for (int j = 0; j < _random.Next(1, 3); j++)
                        nestedArray.Add(_faker.Lorem.Word());
                    array.Add(nestedArray);
                    break;
            }
        }

        return array;
    }

    /// <summary>
    /// Generates a complex JSON object with nested objects and arrays
    /// </summary>
    public JsonObject GenerateComplexObject(int depth = 3, int breadth = 3)
    {
        if (depth <= 0) return GenerateSimpleObject(SafeRandomNext(2, 5));

        var jsonObject = GenerateSimpleObject(SafeRandomNext(2, 5));

        for (int i = 0; i < breadth; i++)
        {
            var childType = _random.Next(2);
            var childName = _faker.Commerce.ProductName().Replace(" ", "");

            if (childType == 0) // Nested object
            {
                jsonObject.Add(childName, GenerateComplexObject(depth - 1, SafeRandomNext(1, Math.Max(2, breadth))));
            }
            else // Array
            {
                var arrayType = (JsonValueKind)SafeRandomNext((int)JsonValueKind.String, (int)JsonValueKind.Object + 1);
                jsonObject.Add(childName, GenerateArray(SafeRandomNext(2, 5), arrayType));
            }
        }

        return jsonObject;
    }

    private int SafeRandomNext(int minValue, int maxValue)
    {
        // Ensure min is less than max
        if (minValue >= maxValue)
        {
            maxValue = minValue + 1;
        }

        return _random.Next(minValue, maxValue);
    }

    /// <summary>
    /// Generates a pair of JSON objects with controlled differences
    /// </summary>
    public (JsonObject original, JsonObject modified, List<string> changes) GenerateDiffPair(int differenceCount = 3)
    {
        var original = GenerateComplexObject();
        var modified = JsonNode.Parse(original.ToJsonString())?.AsObject();
        var changes = new List<string>();

        // Create paths to modify
        var paths = CollectPaths(original);
        var selectedPaths = SelectRandomItems(paths, Math.Min(differenceCount, paths.Count));

        // Apply changes
        foreach (var path in selectedPaths)
        {
            ApplyChange(modified, path, out var changeDescription);
            changes.Add(changeDescription);
        }

        return (original, modified, changes);
    }

    private List<string> CollectPaths(JsonNode node, string basePath = "")
    {
        var paths = new List<string>();

        if (node is JsonObject obj)
        {
            foreach (var prop in obj)
            {
                var currentPath = string.IsNullOrEmpty(basePath) ? prop.Key : $"{basePath}.{prop.Key}";
                paths.Add(currentPath);

                if (prop.Value is JsonObject or JsonArray)
                {
                    paths.AddRange(CollectPaths(prop.Value, currentPath));
                }
            }
        }
        else if (node is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                var currentPath = $"{basePath}[{i}]";
                paths.Add(currentPath);

                if (arr[i] is JsonObject or JsonArray)
                {
                    paths.AddRange(CollectPaths(arr[i], currentPath));
                }
            }
        }

        return paths;
    }

    private List<T> SelectRandomItems<T>(List<T> source, int count)
    {
        var result = new List<T>();
        var tempList = new List<T>(source);

        for (int i = 0; i < count && tempList.Count > 0; i++)
        {
            int index = _random.Next(tempList.Count);
            result.Add(tempList[index]);
            tempList.RemoveAt(index);
        }

        return result;
    }

    private void ApplyChange(JsonNode root, string path, out string changeDescription)
    {
        var parts = path.Split('.', '[', ']');
        parts = Array.FindAll(parts, p => !string.IsNullOrEmpty(p));

        JsonNode current = root;
        JsonNode parent = null;
        string lastKey = parts[parts.Length - 1];
        bool isArrayIndex = int.TryParse(lastKey, out int arrayIndex);

        // Navigate to the parent of the target node
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];
            if (int.TryParse(part, out int index))
            {
                parent = current;
                current = current.AsArray()[index];
            }
            else
            {
                parent = current;
                current = current.AsObject()[part];
            }
        }

        // Randomly decide what type of change to apply
        int changeType = _random.Next(3);

        if (isArrayIndex && parent is JsonArray parentArray)
        {
            switch (changeType)
            {
                case 0: // Modify
                    var oldValue = parentArray[arrayIndex];
                    parentArray[arrayIndex] = GenerateRandomValue();
                    changeDescription = $"Modified {path} from {oldValue} to {parentArray[arrayIndex]}";
                    break;
                case 1: // Remove
                    oldValue = parentArray[arrayIndex];
                    parentArray.RemoveAt(arrayIndex);
                    changeDescription = $"Removed element at {path} with value {oldValue}";
                    break;
                case 2: // Add (insert)
                    var newValue = GenerateRandomValue();
                    parentArray.Insert(arrayIndex, newValue);
                    changeDescription = $"Inserted {newValue} at {path}";
                    break;
                default:
                    changeDescription = "No change";
                    break;
            }
        }
        else if (parent is JsonObject parentObj)
        {
            switch (changeType)
            {
                case 0: // Modify
                    var oldValue = parentObj[lastKey];
                    parentObj[lastKey] = GenerateRandomValue();
                    changeDescription = $"Modified {path} from {oldValue} to {parentObj[lastKey]}";
                    break;
                case 1: // Remove
                    oldValue = parentObj[lastKey];
                    parentObj.Remove(lastKey);
                    changeDescription = $"Removed {path} with value {oldValue}";
                    break;
                case 2: // Add new property
                    var newKey = _faker.Database.Column() + _random.Next(100);
                    var newValue = GenerateRandomValue();
                    parentObj.Add(newKey, newValue);
                    changeDescription = $"Added new property {newKey} with value {newValue}";
                    break;
                default:
                    changeDescription = "No change";
                    break;
            }
        }
        else
        {
            changeDescription = "Invalid path";
        }
    }

    private JsonNode GenerateRandomValue()
    {
        int valueType = _random.Next(4);
        return valueType switch
        {
            0 => JsonValue.Create(_faker.Random.Bool()),
            1 => JsonValue.Create(_faker.Random.Number(-100, 100)),
            2 => JsonValue.Create(_faker.Lorem.Word()),
            3 => JsonValue.Create((string)null),
            _ => JsonValue.Create(_faker.Lorem.Word())
        };
    }

    /// <summary>
    /// Generates a JSON object that has the specified issues (for negative testing)
    /// </summary>
    public string GenerateInvalidJson(InvalidJsonType invalidType = InvalidJsonType.MissingClosingBrace)
    {
        var validJson = GenerateSimpleObject().ToJsonString();

        return invalidType switch
        {
            InvalidJsonType.MissingClosingBrace => validJson.TrimEnd('}'),
            InvalidJsonType.MissingQuotes => validJson.Replace("\"", ""),
            InvalidJsonType.ExtraCommas => validJson.Replace(":", ",:,"),
            InvalidJsonType.MalformedProperty => validJson.Replace("\":", ""),
            InvalidJsonType.UnclosedString => validJson.Replace("\":", "\""),
            _ => validJson
        };
    }

    /// <summary>
    /// Generates a JSON document that's specifically designed to exceed a reasonable parsing size
    /// </summary>
    public string GenerateLargeJson(int depth = 5, int breadth = 10)
    {
        var root = GenerateComplexObject(depth, breadth);
        return root.ToJsonString();
    }
}