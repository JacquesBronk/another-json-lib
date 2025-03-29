using System.Text.Json;

namespace AnotherJsonLib.Infra;

/// <summary>
/// Extension methods for JsonElement.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Clones the JsonElement's value as a native .NET object.
    /// For objects and arrays, returns nested dictionaries or lists.
    /// Numeric values are preserved in their original form:
    /// - If the raw text does not contain a decimal point or exponent, it's parsed as a long.
    /// - Otherwise, it's parsed as a decimal.
    /// </summary>
    public static object? CloneValue(this JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();
            case JsonValueKind.Number:
                string raw = element.GetRawText();
                if (raw.Contains('.') || raw.Contains('e') || raw.Contains('E'))
                {
                    if (decimal.TryParse(raw, out decimal decValue))
                        return decValue;
                    else if (double.TryParse(raw, out double dblValue))
                        return dblValue;
                    else
                        return raw;
                }
                else
                {
                    if (long.TryParse(raw, out long longValue))
                        return longValue;
                    else if (decimal.TryParse(raw, out decimal decValue))
                        return decValue;
                    else
                        return raw;
                }
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                    list.Add(item.CloneValue());
                return list;
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                    dict[prop.Name] = prop.Value.CloneValue();
                return dict;
            default:
                return element.ToString();
        }
    }
}