using System.Text.Json;

namespace AnotherJsonLib.Utility;

public class JsonElementComparer : IEqualityComparer<JsonElement>
{
    private const double Epsilon = 1e-10; // Define your desired epsilon value

    public JsonElementComparer() : this(-1)
    {
    }

    public JsonElementComparer(int maxHashDepth) => MaxHashDepth = maxHashDepth;

    private int MaxHashDepth { get; }

    public bool Equals(JsonElement x, JsonElement y)
    {
        if (x.ValueKind != y.ValueKind)
            return false;

        switch (x.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Undefined:
                return true;

            case JsonValueKind.Number:
                // Compare floating-point numbers with epsilon
                return Math.Abs(x.GetDouble() - y.GetDouble()) < Epsilon;

            case JsonValueKind.String:
                return x.GetString() == y.GetString();

            case JsonValueKind.Array:
                if (x.GetArrayLength() != y.GetArrayLength())
                    return false;

                if (MaxHashDepth != 0)
                {
                    using var enumeratorX = x.EnumerateArray().GetEnumerator();
                    using var enumeratorY = y.EnumerateArray().GetEnumerator();
                    while (enumeratorX.MoveNext() && enumeratorY.MoveNext())
                    {
                        if (!Equals(enumeratorX.Current, enumeratorY.Current))
                            return false;
                    }
                }

                return true;

            case JsonValueKind.Object:
                var xProperties = new Dictionary<string, JsonElement>();
                var yProperties = new Dictionary<string, JsonElement>();

                foreach (var property in x.EnumerateObject())
                {
                    xProperties[property.Name] = property.Value;
                }

                foreach (var property in y.EnumerateObject())
                {
                    yProperties[property.Name] = property.Value;
                }

                if (xProperties.Count != yProperties.Count)
                    return false;

                if (MaxHashDepth != 0)
                {
                    foreach (var propertyName in xProperties.Keys)
                    {
                        if (!yProperties.TryGetValue(propertyName, out var yPropertyValue)
                            || !Equals(xProperties[propertyName], yPropertyValue))
                        {
                            return false;
                        }
                    }
                }

                return true;

            default:
                throw new JsonException($"Unknown JsonValueKind {x.ValueKind}");
        }
    }

    public object ConvertToValueType(JsonElement jsonElement)
    {
        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Null:
                return null!;
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.String:
                return jsonElement.GetString()!;
            case JsonValueKind.Number:
                // Handle numeric conversion here, e.g., using ToInt32, ToDouble, etc.
                // Example:
                return jsonElement.GetInt32();
            default:
                throw new NotSupportedException($"Unsupported JSON element value kind: {jsonElement.ValueKind}");
        }
    }
    
    public int GetHashCode(JsonElement obj)
    {
        var hash = new HashCode();

        ComputeHashCode(obj, ref hash);

        return hash.ToHashCode();
    }

    private void ComputeHashCode(JsonElement obj, ref HashCode hash)
    {
        hash.Add(obj.ValueKind);

        switch (obj.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Undefined:
                break;

            case JsonValueKind.Number:
                // Use the hashed value of the double
                hash.Add(obj.GetDouble().GetHashCode());
                break;

            case JsonValueKind.String:
                hash.Add(obj.GetString());
                break;

            case JsonValueKind.Array:
                if (MaxHashDepth != 0)
                {
                    foreach (var item in obj.EnumerateArray())
                    {
                        ComputeHashCode(item, ref hash);
                    }
                }
                else
                {
                    hash.Add(obj.GetArrayLength());
                }
                break;

            case JsonValueKind.Object:
                foreach (var property in obj.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    hash.Add(property.Name);
                    if (MaxHashDepth != 0)
                    {
                        ComputeHashCode(property.Value, ref hash);
                    }
                }
                break;

            default:
                throw new JsonException($"Unknown JsonValueKind {obj.ValueKind}");
        }
    }
}
