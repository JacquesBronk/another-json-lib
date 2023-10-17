using System.Text.Json;

namespace AnotherJsonLib.Utility;

/// <summary>
/// Provides utility methods for querying JSON data using JSONPath expressions.
/// </summary>
public static partial class JsonTools
{
    /// <summary>
    /// Queries JSON data using a JSONPath expression and returns a collection of matching JSON elements.
    /// </summary>
    /// <param name="jsonDocument"></param>
    /// <param name="jsonPath">The JSONPath expression.</param>
    /// <returns>A collection of matching JSON elements.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="jsonDocument"/> or <paramref name="jsonPath"/> is null or empty.</exception>
    public static IEnumerable<JsonElement?> QueryJsonElement(this JsonDocument jsonDocument, string jsonPath)
    {
        if (jsonDocument == null || string.IsNullOrEmpty(jsonPath))
            throw new ArgumentNullException($"{nameof(jsonDocument)} and {nameof(jsonPath)} should have values.");

        foreach (var result in QueryJsonElement(jsonDocument.RootElement, jsonPath))
        {
            yield return result;
        }
    }


    private static IEnumerable<JsonElement?> QueryJsonElement(JsonElement element, string jsonPath)
    {
        if (string.IsNullOrEmpty(jsonPath))
        {
            yield return element;
            yield break;
        }

        var parts = jsonPath.TrimStart('$').Split('.').Where(part => !string.IsNullOrEmpty(part)).ToArray();
        var part = parts[0];

        if (part == "##")
        {
            // Descendants
            foreach (var descendent in DescendantsOrSelf(element))
            {
                foreach (var match in QueryJsonElement(descendent, string.Join(".", parts.Skip(1))))
                {
                    yield return match;
                }
            }
        }
        else if (part == "*")
        {
            // Wildcard for all properties
            foreach (var property in element.EnumerateObject())
            {
                foreach (var match in QueryJsonElement(property.Value, string.Join(".", parts.Skip(1))))
                {
                    yield return match;
                }
            }
        }
        else if (part.EndsWith("]"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(part, @"(.*)\[(.+)\]");
            if (match.Success)
            {
                var propertyName = match.Groups[1].Value;
                var indexesPart = match.Groups[2].Value;
                var indexes = indexesPart.Split(',').Select(int.Parse);

                if (element.TryGetProperty(propertyName, out var childElement) &&
                    childElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var index in indexes)
                    {
                        if (index < childElement.GetArrayLength())
                        {
                            yield return childElement[index];
                        }
                    }
                }
            }
            else
            {
                // Property with index e.g., property[1]
                match = System.Text.RegularExpressions.Regex.Match(part, @"(.*)\[(\d+)\]");
                if (match.Success)
                {
                    var propertyName = match.Groups[1].Value;
                    var index = int.Parse(match.Groups[2].Value);

                    if (element.TryGetProperty(propertyName, out var childElement) &&
                        childElement.ValueKind == JsonValueKind.Array && index < childElement.GetArrayLength())
                    {
                        yield return childElement[index];
                    }
                }
            }
        }


        else if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(part, out var childElement))
        {
            var results = new List<JsonElement?>();

            if (childElement.ValueKind == JsonValueKind.String)
            {
                var stringValue = childElement.GetString();
                if (!string.IsNullOrEmpty(stringValue))
                {
                    results.Add(childElement);
                }
            }
            else if (childElement.ValueKind == JsonValueKind.Number)
            {
                results.Add(childElement);
            }
            else if (childElement.ValueKind == JsonValueKind.Object)
            {
                results.Add(childElement);
            }
            else if (childElement.ValueKind == JsonValueKind.Array)
            {
                results.AddRange(childElement.EnumerateArray().Select(e => (JsonElement?) e));
            }
            else if (childElement.ValueKind == JsonValueKind.False || childElement.ValueKind == JsonValueKind.True)
            {
                results.Add(childElement);
            }
            else if (childElement.ValueKind == JsonValueKind.Null)
            {
                // Handle the case where the childElement is a JSON null value
                // You can add your code specific to handling null values here
                yield return null;
            }

            foreach (var result in results)
            {
                yield return result;
            }
        }
    }

    private static IEnumerable<JsonElement> DescendantsOrSelf(JsonElement root)
    {
        var nodes = new Stack<JsonElement>(new[] {root});
        while (nodes.Any())
        {
            var node = nodes.Pop();
            yield return node;

            if (node.ValueKind == JsonValueKind.Object)
            {
                foreach (var child in node.EnumerateObject().Select(o => o.Value))
                {
                    nodes.Push(child);
                }
            }
            else if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in node.EnumerateArray())
                {
                    nodes.Push(child);
                }
            }
        }
    }
}