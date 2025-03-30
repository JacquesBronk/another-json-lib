using System.Text.Json;

namespace AnotherJsonLib.Utility;

public static class JsonElementUtils
{
    /// <summary>
    /// Converts a JsonElement to a native .NET object (string, number, bool, list, dict, or null).
    /// </summary>
    public static object? ConvertToObject(JsonElement element, bool sortProperties = false)
    {
        // Handle simple JSON value kinds directly
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                // Determine the most appropriate numeric type
                if (element.TryGetInt32(out int intVal))
                    return intVal;
                if (element.TryGetInt64(out long longVal))
                    return longVal;
                if (element.TryGetDecimal(out decimal decVal))
                    return decVal;
                // Fallback to double for very large numbers
                return element.GetDouble();
            case JsonValueKind.Object:
                // Convert object to dictionary (optionally sorted)
                var objDict = sortProperties
                    ? (IDictionary<string, object?>)new SortedDictionary<string, object?>(StringComparer.Ordinal)
                    : new Dictionary<string, object?>();
                foreach (JsonProperty prop in element.EnumerateObject())
                {
                    string key = prop.Name;
                    objDict[key] = ConvertToObject(prop.Value, sortProperties);
                }
                return objDict;
            case JsonValueKind.Array:
                // Convert array to list
                var list = new List<object?>();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    list.Add(ConvertToObject(item, sortProperties));
                }
                return list;
            default:
                // Unexpected kind – return string representation
                return element.ToString();
        }
    }

    /// <summary>
    /// Deeply compares two JsonElement values for equality. Supports an optional numeric tolerance and case sensitivity for object property names.
    /// </summary>
    public static bool DeepEquals(JsonElement a, JsonElement b, double epsilon = 0.0, bool caseSensitivePropertyNames = true)
    {
        // If both are undefined, treat as equal; if kinds differ, not equal
        if (a.ValueKind == JsonValueKind.Undefined && b.ValueKind == JsonValueKind.Undefined)
            return true;
        if (a.ValueKind != b.ValueKind)
            return false;

        switch (a.ValueKind)
        {
            case JsonValueKind.Null:
                return true;  // both are null
            case JsonValueKind.True:
            case JsonValueKind.False:
                return a.GetBoolean() == b.GetBoolean();
            case JsonValueKind.String:
                return a.GetString() == b.GetString();
            case JsonValueKind.Number:
                if (epsilon > 0.0)
                {
                    // Approximate comparison using double
                    double aVal = a.GetDouble();
                    double bVal = b.GetDouble();
                    if (double.IsNaN(aVal) && double.IsNaN(bVal)) return true;
                    if (double.IsInfinity(aVal) || double.IsInfinity(bVal))
                        return double.IsInfinity(aVal) && double.IsInfinity(bVal) && Math.Abs(aVal - bVal) < epsilon;
                    return Math.Abs(aVal - bVal) < epsilon;
                }
                else
                {
                    // Strict comparison using decimal if possible
                    if (a.TryGetDecimal(out decimal aDec) && b.TryGetDecimal(out decimal bDec))
                        return aDec == bDec;
                    // Fallback to raw text comparison if decimal conversion fails
                    return a.GetRawText() == b.GetRawText();
                }
            case JsonValueKind.Object:
                // Compare objects: check property counts and values
                var aProps = a.EnumerateObject().ToList();
                var bProps = b.EnumerateObject().ToList();
                if (aProps.Count != bProps.Count) 
                    return false;
                foreach (JsonProperty aProp in aProps)
                {
                    // Find matching property in b (respect case sensitivity setting)
                    JsonProperty? bProp = caseSensitivePropertyNames 
                        ? bProps.FirstOrDefault(p => p.NameEquals(aProp.Name)) 
                        : bProps.FirstOrDefault(p => string.Equals(p.Name, aProp.Name, StringComparison.OrdinalIgnoreCase));
                    if (bProp == null) 
                        return false;
                    if (!DeepEquals(aProp.Value, bProp.Value.Value, epsilon, caseSensitivePropertyNames))
                        return false;
                }
                return true;
            case JsonValueKind.Array:
                // Compare arrays element by element
                if (a.GetArrayLength() != b.GetArrayLength())
                    return false;
                var enumA = a.EnumerateArray().GetEnumerator();
                var enumB = b.EnumerateArray().GetEnumerator();
                while (enumA.MoveNext() && enumB.MoveNext())
                {
                    if (!DeepEquals(enumA.Current, enumB.Current, epsilon, caseSensitivePropertyNames))
                        return false;
                }
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Produces a normalized .NET object from a JsonElement for canonicalization:
    /// sorts object properties and uses decimals for numeric values when possible.
    /// </summary>
    public static object? Normalize(JsonElement element)
    {
        // Reuse ConvertToObject with sorted properties for base structure,
        // and ensure numbers are represented as decimal where possible.
        return ConvertToObject(element, sortProperties: true);
    }
}