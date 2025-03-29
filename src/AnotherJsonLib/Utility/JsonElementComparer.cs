using System.Text.Json;
using AnotherJsonLib.Exceptions;
using Microsoft.Extensions.Logging;
using AnotherJsonLib.Infra;

namespace AnotherJsonLib.Utility;

/// <summary>
/// Provides comparison functionality for JsonElement objects with customizable
/// floating-point equality precision and hash depth control.
/// </summary>
public class JsonElementComparer : IEqualityComparer<JsonElement>
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonElementComparer));

    /// <summary>
    /// Default epsilon value for floating-point comparisons.
    /// </summary>
    private const double DefaultEpsilon = 1e-10;

    /// <summary>
    /// Gets the epsilon value used for floating-point comparisons.
    /// </summary>
    private double Epsilon { get; }

    /// <summary>
    /// Gets the maximum recursion depth for hash code calculation.
    /// </summary>
    private int MaxHashDepth { get; }

    /// <summary>
    /// Initializes a new instance of the JsonElementComparer class with optional custom settings.
    /// </summary>
    /// <param name="epsilon">Epsilon value for floating-point comparisons. Defaults to 1e-10.</param>
    /// <param name="maxHashDepth">Maximum recursion depth for hash code calculation. -1 means no limit.</param>
    /// <exception cref="JsonArgumentException">Thrown if epsilon is negative or maxHashDepth is less than -1.</exception>
    public JsonElementComparer(double epsilon = DefaultEpsilon, int maxHashDepth = -1)
    {
        using var performance = new PerformanceTracker(Logger, nameof(JsonElementComparer));

        ExceptionHelpers.ThrowIfFalse(epsilon >= 0, "Epsilon must be non-negative", nameof(epsilon));
        ExceptionHelpers.ThrowIfFalse(maxHashDepth >= -1,
            "MaxHashDepth must be -1 (unlimited) or a non-negative integer", nameof(maxHashDepth));

        Epsilon = epsilon;
        MaxHashDepth = maxHashDepth;

        Logger.LogTrace("JsonElementComparer created with epsilon={Epsilon}, maxHashDepth={MaxHashDepth}", Epsilon,
            MaxHashDepth);
    }

    /// <summary>
    /// Determines whether two JsonElement instances are equal.
    /// </summary>
    /// <param name="x">The first JsonElement to compare.</param>
    /// <param name="y">The second JsonElement to compare.</param>
    /// <returns>true if the specified JsonElements are equal; otherwise, false.</returns>
    public bool Equals(JsonElement x, JsonElement y)
    {
        using var performance = new PerformanceTracker(Logger, nameof(Equals));

        return ExceptionHelpers.SafeExecute(() =>
            {
                // If both are undefined, consider them equal
                if (x.ValueKind == JsonValueKind.Undefined && y.ValueKind == JsonValueKind.Undefined)
                    return true;

                // If one is undefined but the other is not, or if they have different kinds
                if (x.ValueKind != y.ValueKind)
                    return false;

                switch (x.ValueKind)
                {
                    case JsonValueKind.Null:
                        return true;

                    case JsonValueKind.String:
                        return x.GetString() == y.GetString();

                    case JsonValueKind.Number:
                        // For numbers, use epsilon comparison for doubles
                        double xValue = x.GetDouble();
                        double yValue = y.GetDouble();
                        return Math.Abs(xValue - yValue) < Epsilon;

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return x.GetBoolean() == y.GetBoolean();

                    case JsonValueKind.Object:
                        // For objects, check if all properties match
                        var xProps = x.EnumerateObject().ToDictionary(p => p.Name);
                        var yProps = y.EnumerateObject().ToDictionary(p => p.Name);

                        if (xProps.Count != yProps.Count)
                            return false;

                        foreach (var prop in xProps)
                        {
                            if (!yProps.TryGetValue(prop.Key, out var yProp) || !Equals(prop.Value.Value, yProp.Value))
                                return false;
                        }

                        return true;

                    case JsonValueKind.Array:
                        // For arrays, check if all elements match in order
                        var xArray = x.EnumerateArray().ToArray();
                        var yArray = y.EnumerateArray().ToArray();

                        if (xArray.Length != yArray.Length)
                            return false;

                        for (int i = 0; i < xArray.Length; i++)
                        {
                            if (!Equals(xArray[i], yArray[i]))
                                return false;
                        }

                        return true;

                    default:
                        return false;
                }
            }, (ex, msg) => new JsonOperationException("Failed to compare JsonElements: " + msg, ex),
            "Failed to compare JsonElements");
    }

    /// <summary>
    /// Gets the hash code for the specified JsonElement.
    /// </summary>
    /// <param name="obj">The JsonElement to get a hash code for.</param>
    /// <returns>A hash code for the specified JsonElement.</returns>
    public int GetHashCode(JsonElement obj)
    {
        using var performance = new PerformanceTracker(Logger, nameof(GetHashCode));

        return ExceptionHelpers.SafeExecute(() =>
            {
                var hash = new HashCode();
                ComputeHashCode(obj, ref hash, 0);
                return hash.ToHashCode();
            }, (ex, msg) => new JsonOperationException("Failed to compute hash code for JsonElement: " + msg, ex),
            "Failed to compute hash code for JsonElement");
    }

    /// <summary>
    /// Recursively computes a hash code for the specified JsonElement.
    /// </summary>
    /// <param name="obj">The JsonElement to compute a hash code for.</param>
    /// <param name="hash">The HashCode object to add the hash to.</param>
    /// <param name="depth">The current recursion depth.</param>
    public void ComputeHashCode(JsonElement obj, ref HashCode hash, int depth)
    {
        try
        {
            // Check if we've reached the maximum recursion depth
            if (MaxHashDepth >= 0 && depth > MaxHashDepth)
            {
                Logger.LogTrace("Max hash depth reached ({MaxHashDepth}), truncating hash calculation", MaxHashDepth);
                return;
            }

            // Add the JsonValueKind to the hash
            hash.Add(obj.ValueKind);

            switch (obj.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    // No value to hash
                    break;

                case JsonValueKind.Number:
                    // For numbers, round to account for epsilon
                    double val = obj.GetDouble();
                    double rounded = Math.Round(val / Epsilon) * Epsilon;
                    hash.Add(rounded);
                    break;

                case JsonValueKind.String:
                    hash.Add(obj.GetString());
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    hash.Add(obj.GetBoolean());
                    break;

                case JsonValueKind.Object:
                    // For objects, sort properties by name for consistent hash
                    var props = obj.EnumerateObject()
                        .OrderBy(p => p.Name)
                        .ToArray();

                    foreach (var prop in props)
                    {
                        hash.Add(prop.Name);
                        ComputeHashCode(prop.Value, ref hash, depth + 1);
                    }

                    break;

                case JsonValueKind.Array:
                    // For arrays, hash each element in order
                    foreach (var item in obj.EnumerateArray())
                    {
                        ComputeHashCode(item, ref hash, depth + 1);
                    }

                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to compute hash code component");
            Logger.LogError(ex, "Failed to compute hash code component");
            throw new JsonOperationException("Failed to compute hash code component", ex);
        }
    }

    /// <summary>
    /// Converts a JsonElement to its corresponding .NET type value.
    /// </summary>
    /// <param name="jsonElement">The JsonElement to convert.</param>
    /// <returns>The corresponding .NET object value or null if conversion is not possible.</returns>
    public object? ConvertToValueType(JsonElement jsonElement)
    {
        using var performance = new PerformanceTracker(Logger, nameof(ConvertToValueType));

        return ExceptionHelpers.SafeExecuteWithDefault<object?>(
            () =>
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return null;

                    case JsonValueKind.Number:
                        // Try to determine the most appropriate numeric type
                        if (jsonElement.TryGetInt32(out int intValue))
                            return intValue;
                        if (jsonElement.TryGetInt64(out long longValue))
                            return longValue;
                        return jsonElement.GetDouble();

                    case JsonValueKind.String:
                        return jsonElement.GetString();

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return jsonElement.GetBoolean();

                    case JsonValueKind.Object:
                        var obj = new Dictionary<string, object?>();
                        foreach (var property in jsonElement.EnumerateObject())
                        {
                            obj[property.Name] = ConvertToValueType(property.Value);
                        }

                        return obj;

                    case JsonValueKind.Array:
                        return jsonElement.EnumerateArray()
                            .Select(ConvertToValueType)
                            .ToList();

                    default:
                        Logger.LogWarning("Unknown JsonValueKind {ValueKind} encountered", jsonElement.ValueKind);
                        return null;
                }
            },
            null,
            "Failed to convert JsonElement to value type"
        );
    }
}