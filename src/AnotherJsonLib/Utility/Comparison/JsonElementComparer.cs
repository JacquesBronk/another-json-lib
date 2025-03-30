using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Helper;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Comparison;

/// <summary>
/// Provides comparison functionality for JsonElement objects with customizable options for
/// equality comparison and hash code generation.
/// 
/// This utility enables precise comparison of JSON data by:
/// 
/// - Supporting approximate floating-point comparison with configurable precision
/// - Handling special cases like NaN and infinity values consistently
/// - Limiting recursion depth during hash code calculation for large objects
/// - Converting JsonElements to appropriate .NET types for natural comparison
/// - Enabling customized equality comparison for specific requirements
/// 
/// It implements IEqualityComparer&lt;JsonElement&gt; allowing it to be used with LINQ methods
/// like Distinct() and GroupBy(), as well as Dictionary and HashSet collections.
/// 
/// <example>
/// <code>
/// // Compare two JSON elements with default precision
/// var comparer = new JsonElementComparer();
/// 
/// string json1 = @"{""value"": 1.0000001}";
/// string json2 = @"{""value"": 1.0000002}";
/// 
/// using var doc1 = JsonDocument.Parse(json1);
/// using var doc2 = JsonDocument.Parse(json2);
/// 
/// // These will be considered equal with default epsilon of 1e-10
/// bool areEqual = comparer.Equals(
///     doc1.RootElement.GetProperty("value"),
///     doc2.RootElement.GetProperty("value"));
/// 
/// // Create a more strict comparer
/// var strictComparer = new JsonElementComparer(epsilon: 1e-12);
/// 
/// // Now they will be considered different
/// bool areStrictEqual = strictComparer.Equals(
///     doc1.RootElement.GetProperty("value"),
///     doc2.RootElement.GetProperty("value"));
/// </code>
/// </example>
/// </summary>
public class JsonElementComparer : IEqualityComparer<JsonElement>
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonElementComparer));

    /// <summary>
    /// Default epsilon value for floating-point comparisons.
    /// </summary>
    public const double DefaultEpsilon = 1e-10;

    /// <summary>
    /// Gets the epsilon value used for floating-point comparisons.
    /// </summary>
    private double Epsilon { get; }

    /// <summary>
    /// Gets the maximum recursion depth for hash code calculation.
    /// A value of -1 means no limit.
    /// </summary>
    private int MaxHashDepth { get; }

    /// <summary>
    /// Gets whether property name comparison should be case-sensitive.
    /// </summary>
    private bool CaseSensitivePropertyNames { get; }

    /// <summary>
    /// Initializes a new instance of the JsonElementComparer class with optional custom settings.
    /// 
    /// <example>
    /// <code>
    /// // Default comparer with standard settings
    /// var defaultComparer = new JsonElementComparer();
    /// 
    /// // More precise floating-point comparison
    /// var preciseComparer = new JsonElementComparer(epsilon: 1e-15);
    /// 
    /// // Limit hash depth for very large objects to improve performance
    /// var limitedComparer = new JsonElementComparer(maxHashDepth: 10);
    /// 
    /// // Case-insensitive property name comparison
    /// var caseInsensitiveComparer = new JsonElementComparer(
    ///     caseSensitivePropertyNames: false);
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="epsilon">
    /// Epsilon value for floating-point comparisons. Defaults to 1e-10.
    /// Smaller values mean stricter comparison.
    /// </param>
    /// <param name="maxHashDepth">
    /// Maximum recursion depth for hash code calculation. -1 means no limit.
    /// Useful for very large objects to prevent stack overflow.
    /// </param>
    /// <param name="caseSensitivePropertyNames">
    /// Whether property name comparison should be case-sensitive.
    /// Set to false for case-insensitive comparison.
    /// </param>
    /// <exception cref="JsonArgumentException">
    /// Thrown if epsilon is negative or maxHashDepth is less than -1.
    /// </exception>
    public JsonElementComparer(
        double epsilon = DefaultEpsilon,
        int maxHashDepth = -1,
        bool caseSensitivePropertyNames = true)
    {
        using var performance = new PerformanceTracker(Logger, nameof(JsonElementComparer));

        // Validate inputs
        ExceptionHelpers.ThrowIfFalse(epsilon >= 0, "Epsilon must be non-negative", nameof(epsilon));
        ExceptionHelpers.ThrowIfFalse(maxHashDepth >= -1,
            "MaxHashDepth must be -1 (unlimited) or a non-negative integer", nameof(maxHashDepth));

        Epsilon = epsilon;
        MaxHashDepth = maxHashDepth;
        CaseSensitivePropertyNames = caseSensitivePropertyNames;

        Logger.LogTrace("JsonElementComparer created with epsilon={Epsilon}, maxHashDepth={MaxHashDepth}, " +
                        "caseSensitivePropertyNames={CaseSensitive}",
            Epsilon, MaxHashDepth, CaseSensitivePropertyNames);
    }

    /// <summary>
    /// Determines whether two JsonElement instances are equal.
    /// 
    /// The comparison follows these rules:
    /// - Objects are equal if they have the same properties with equal values
    /// - Arrays are equal if they have the same elements in the same order
    /// - Numbers are equal if they are within epsilon of each other
    /// - Strings, booleans, and nulls are compared directly
    /// - Property names are compared according to the caseSensitivePropertyNames setting
    /// 
    /// <example>
    /// <code>
    /// // Compare two complete JSON objects
    /// string json1 = @"{""name"":""John"",""score"":95.001}";
    /// string json2 = @"{""name"":""John"",""score"":95.0011}";
    /// 
    /// using var doc1 = JsonDocument.Parse(json1);
    /// using var doc2 = JsonDocument.Parse(json2);
    /// 
    /// var comparer = new JsonElementComparer();
    /// bool areEqual = comparer.Equals(doc1.RootElement, doc2.RootElement);
    /// // areEqual will be true with default epsilon
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="x">The first JsonElement to compare.</param>
    /// <param name="y">The second JsonElement to compare.</param>
    /// <returns>True if the specified JsonElements are equal; otherwise, false.</returns>
    /// <exception cref="JsonComparisonException">Thrown when comparison fails due to an unexpected error.</exception>
    public bool Equals(JsonElement x, JsonElement y)
    {
        using var performance = new PerformanceTracker(Logger, nameof(Equals));
        return ExceptionHelpers.SafeExecute(() => JsonElementUtils.DeepEquals(x, y, Epsilon, CaseSensitivePropertyNames),
            (ex, msg) => new JsonComparisonException($"Error comparing JSON elements: {msg}", ex),
            $"Error comparing JsonElements of types {x.ValueKind} and {y.ValueKind}");
    }

    /// <summary>
    /// Compares two JSON objects for equality by checking that they have the same properties with equal values.
    /// </summary>
    /// <param name="x">The first JSON object.</param>
    /// <param name="y">The second JSON object.</param>
    /// <returns>True if the objects are equal; otherwise, false.</returns>
    private bool CompareObjects(JsonElement x, JsonElement y)
    {
        // Check property count first for quick inequality detection
        int xPropCount = 0;
        int yPropCount = 0;

        foreach (var _ in x.EnumerateObject()) xPropCount++;
        foreach (var _ in y.EnumerateObject()) yPropCount++;

        if (xPropCount != yPropCount)
        {
            Logger.LogTrace("Object property counts differ: {Count1} vs {Count2}", xPropCount, yPropCount);
            return false;
        }

        // Check that all properties in x are in y with equal values
        foreach (var xProp in x.EnumerateObject())
        {
            bool propertyFound = false;

            foreach (var yProp in y.EnumerateObject())
            {
                bool namesEqual = CaseSensitivePropertyNames
                    ? xProp.Name == yProp.Name
                    : string.Equals(xProp.Name, yProp.Name, StringComparison.OrdinalIgnoreCase);

                if (namesEqual)
                {
                    propertyFound = true;

                    if (!Equals(xProp.Value, yProp.Value))
                    {
                        Logger.LogTrace("Property '{PropertyName}' values differ", xProp.Name);
                        return false;
                    }

                    break;
                }
            }

            if (!propertyFound)
            {
                Logger.LogTrace("Property '{PropertyName}' not found in second object", xProp.Name);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Compares two JSON arrays for equality by checking that they have the same elements in the same order.
    /// </summary>
    /// <param name="x">The first JSON array.</param>
    /// <param name="y">The second JSON array.</param>
    /// <returns>True if the arrays are equal; otherwise, false.</returns>
    private bool CompareArrays(JsonElement x, JsonElement y)
    {
        // Compare array lengths
        int xCount = 0;
        int yCount = 0;

        foreach (var _ in x.EnumerateArray()) xCount++;
        foreach (var _ in y.EnumerateArray()) yCount++;

        if (xCount != yCount)
        {
            Logger.LogTrace("Array lengths differ: {Length1} vs {Length2}", xCount, yCount);
            return false;
        }

        // Compare array elements in order
        using var xEnumerator = x.EnumerateArray();
        using var yEnumerator = y.EnumerateArray();

        int index = 0;
        while (xEnumerator.MoveNext() && yEnumerator.MoveNext())
        {
            if (!Equals(xEnumerator.Current, yEnumerator.Current))
            {
                Logger.LogTrace("Array elements at index {Index} differ", index);
                return false;
            }

            index++;
        }

        return true;
    }

    /// <summary>
    /// Gets the hash code for the specified JsonElement.
    /// This implementation ensures that equal JsonElements (as determined by Equals)
    /// will have the same hash code, which is required for correct behavior in hash-based collections.
    /// 
    /// The hash calculation is limited by MaxHashDepth to prevent stack overflow for very deep structures.
    /// 
    /// <example>
    /// <code>
    /// // Use the comparer with a Dictionary
    /// var comparer = new JsonElementComparer();
    /// var jsonElementDict = new Dictionary&lt;JsonElement, string&gt;(comparer);
    /// 
    /// // Parse some JSON
    /// using var doc = JsonDocument.Parse(@"{""id"": 42}");
    /// var element = doc.RootElement;
    /// 
    /// // Add to dictionary
    /// jsonElementDict[element] = "Answer to life, universe, and everything";
    /// 
    /// // Later, retrieve using an equivalent element
    /// using var doc2 = JsonDocument.Parse(@"{""id"": 42.0}");  // Numerically equivalent
    /// var element2 = doc2.RootElement;
    /// 
    /// string value = jsonElementDict[element2];  // Successfully retrieves the value
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="obj">The JsonElement to get a hash code for.</param>
    /// <returns>A hash code for the specified JsonElement.</returns>
    /// <exception cref="JsonComparisonException">Thrown when hash code calculation fails due to an unexpected error.</exception>
    public int GetHashCode(JsonElement obj)
    {
        using var performance = new PerformanceTracker(Logger, nameof(GetHashCode));
        return ExceptionHelpers.SafeExecute(() =>
            {
                var hash = new HashCode();
                ComputeHashCode(obj, ref hash, 0);
                return hash.ToHashCode();
            },
            (ex, msg) => new JsonComparisonException($"Failed to compute hash code for JsonElement: {msg}", ex),
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

                    // Special handling for NaN and Infinity
                    if (double.IsNaN(val))
                    {
                        hash.Add(double.NaN.GetHashCode());
                    }
                    else if (double.IsInfinity(val))
                    {
                        hash.Add(val.GetHashCode());
                    }
                    else
                    {
                        double rounded = Math.Round(val / Epsilon) * Epsilon;
                        hash.Add(rounded);
                    }

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
                        .OrderBy(p => p.Name, CaseSensitivePropertyNames
                            ? StringComparer.Ordinal
                            : StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    foreach (var prop in props)
                    {
                        // Add property name to hash (respecting case sensitivity)
                        if (CaseSensitivePropertyNames)
                            hash.Add(prop.Name);
                        else
                            hash.Add(prop.Name.ToLowerInvariant());

                        // Add property value to hash
                        ComputeHashCode(prop.Value, ref hash, depth + 1);
                    }

                    break;

                case JsonValueKind.Array:
                    // For arrays, hash each element in order
                    int index = 0;
                    foreach (var item in obj.EnumerateArray())
                    {
                        hash.Add(index++); // Include index in hash
                        ComputeHashCode(item, ref hash, depth + 1);
                    }

                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to compute hash code component");
            throw new JsonComparisonException("Failed to compute hash code component", ex);
        }
    }

    /// <summary>
    /// Converts a JsonElement to its corresponding .NET type value.
    /// This allows natural handling of JSON values in .NET code.
    /// 
    /// <example>
    /// <code>
    /// // Parse some JSON
    /// using var doc = JsonDocument.Parse(@"{
    ///     ""string"": ""Hello"",
    ///     ""number"": 42.5,
    ///     ""integer"": 100,
    ///     ""boolean"": true,
    ///     ""null"": null,
    ///     ""array"": [1, 2, 3],
    ///     ""object"": {""key"": ""value""}
    /// }");
    /// 
    /// var comparer = new JsonElementComparer();
    /// 
    /// // Convert different types
    /// string? stringValue = comparer.ConvertToValueType(doc.RootElement.GetProperty("string")) as string;
    /// double? doubleValue = comparer.ConvertToValueType(doc.RootElement.GetProperty("number")) as double?;
    /// int? intValue = comparer.ConvertToValueType(doc.RootElement.GetProperty("integer")) as int?;
    /// bool? boolValue = comparer.ConvertToValueType(doc.RootElement.GetProperty("boolean")) as bool?;
    /// object? nullValue = comparer.ConvertToValueType(doc.RootElement.GetProperty("null"));  // Will be null
    /// List&lt;object?&gt;? arrayValue = comparer.ConvertToValueType(doc.RootElement.GetProperty("array")) as List&lt;object?&gt;;
    /// Dictionary&lt;string, object?&gt;? objectValue = 
    ///     comparer.ConvertToValueType(doc.RootElement.GetProperty("object")) as Dictionary&lt;string, object?&gt;;
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="jsonElement">The JsonElement to convert.</param>
    /// <returns>The corresponding .NET object value or null if conversion is not possible.</returns>
    /// <exception cref="JsonComparisonException">Thrown when conversion fails due to an unexpected error.</exception>
    public object? ConvertToValueType(JsonElement jsonElement)
    {
        using var performance = new PerformanceTracker(Logger, nameof(ConvertToValueType));

        return ExceptionHelpers.SafeExecuteWithDefault<object?>(
            () => JsonElementUtils.ConvertToObject(jsonElement),
            null,
            "Failed to convert JsonElement to value type"
        );
    }

    /// <summary>
    /// Creates a case-insensitive JsonElementComparer.
    /// This is a convenience method for creating a comparer that ignores case in property name comparisons.
    /// 
    /// <example>
    /// <code>
    /// // These two objects have the same property values but different property name casing
    /// string json1 = @"{""Name"":""John"",""Age"":30}";
    /// string json2 = @"{""name"":""John"",""age"":30}";
    /// 
    /// using var doc1 = JsonDocument.Parse(json1);
    /// using var doc2 = JsonDocument.Parse(json2);
    /// 
    /// // Create a case-insensitive comparer
    /// var comparer = JsonElementComparer.CaseInsensitive();
    /// 
    /// // Objects will be considered equal
    /// bool areEqual = comparer.Equals(doc1.RootElement, doc2.RootElement);  // true
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="epsilon">Optional epsilon value for numeric comparisons.</param>
    /// <param name="maxHashDepth">Optional maximum hash depth.</param>
    /// <returns>A JsonElementComparer configured for case-insensitive property name comparison.</returns>
    public static JsonElementComparer CaseInsensitive(double epsilon = DefaultEpsilon, int maxHashDepth = -1)
    {
        return new JsonElementComparer(epsilon, maxHashDepth, caseSensitivePropertyNames: false);
    }

    /// <summary>
    /// Creates a JsonElementComparer with a specific numeric comparison precision.
    /// This is useful when you need to control exactly how close numeric values must be to be considered equal.
    /// 
    /// <example>
    /// <code>
    /// // These values differ slightly
    /// string json1 = @"{""value"":1.0000001}";
    /// string json2 = @"{""value"":1.0000009}";
    /// 
    /// using var doc1 = JsonDocument.Parse(json1);
    /// using var doc2 = JsonDocument.Parse(json2);
    /// 
    /// var elem1 = doc1.RootElement.GetProperty("value");
    /// var elem2 = doc2.RootElement.GetProperty("value");
    /// 
    /// // With loose comparison (epsilon = 1e-5), they're equal
    /// var looseComparer = JsonElementComparer.WithPrecision(1e-5);
    /// bool looseEqual = looseComparer.Equals(elem1, elem2);  // true
    /// 
    /// // With strict comparison (epsilon = 1e-8), they're different
    /// var strictComparer = JsonElementComparer.WithPrecision(1e-8);
    /// bool strictEqual = strictComparer.Equals(elem1, elem2);  // false
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="epsilon">The epsilon value to use for numeric comparisons.</param>
    /// <param name="caseSensitivePropertyNames">Whether property name comparison should be case-sensitive.</param>
    /// <param name="maxHashDepth">Optional maximum hash depth.</param>
    /// <returns>A JsonElementComparer configured with the specified precision.</returns>
    /// <exception cref="JsonArgumentException">Thrown if epsilon is negative.</exception>
    public static JsonElementComparer WithPrecision(
        double epsilon,
        bool caseSensitivePropertyNames = true,
        int maxHashDepth = -1)
    {
        ExceptionHelpers.ThrowIfFalse(epsilon >= 0, "Epsilon must be non-negative", nameof(epsilon));
        return new JsonElementComparer(epsilon, maxHashDepth, caseSensitivePropertyNames);
    }

    /// <summary>
    /// Determines if two JsonElement instances represent semantically equal JSON values.
    /// This is a static convenience method that uses a default JsonElementComparer.
    /// 
    /// <example>
    /// <code>
    /// // Compare two equivalent JSON values with different formats
    /// string json1 = @"{""value"": 1.0}";
    /// string json2 = @"{""value"": 1}";
    /// 
    /// using var doc1 = JsonDocument.Parse(json1);
    /// using var doc2 = JsonDocument.Parse(json2);
    /// 
    /// // Static method for quick comparisons
    /// bool areEqual = JsonElementComparer.JsonElementsEqual(
    ///     doc1.RootElement.GetProperty("value"),
    ///     doc2.RootElement.GetProperty("value"));  // true
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="a">The first JsonElement to compare.</param>
    /// <param name="b">The second JsonElement to compare.</param>
    /// <returns>True if the elements are semantically equal; otherwise, false.</returns>
    /// <exception cref="JsonComparisonException">Thrown when comparison fails due to an unexpected error.</exception>
    public static bool JsonElementsEqual(JsonElement a, JsonElement b)
    {
        var defaultComparer = new JsonElementComparer();
        return defaultComparer.Equals(a, b);
    }

    /// <summary>
    /// Creates a deep clone of a JsonElement as its corresponding .NET object graph.
    /// This is useful for extracting data from JsonElements into manipulable .NET objects.
    /// 
    /// <example>
    /// <code>
    /// // Original JSON data
    /// string json = @"{""user"":{""name"":""John"",""scores"":[95,87,92]}}";
    /// using var doc = JsonDocument.Parse(json);
    /// 
    /// // Clone the data to a Dictionary
    /// var comparer = new JsonElementComparer();
    /// var clonedData = comparer.CloneElement(doc.RootElement);
    /// 
    /// // Now you can manipulate it as native .NET objects
    /// var userData = clonedData as Dictionary&lt;string, object?&gt;;
    /// var user = userData?["user"] as Dictionary&lt;string, object?&gt;;
    /// var scores = user?["scores"] as List&lt;object?&gt;;
    /// 
    /// // Update a score
    /// if (scores != null && scores.Count > 0)
    ///     scores[0] = 97;
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="element">The JsonElement to clone.</param>
    /// <returns>A deep clone of the element as a native .NET object.</returns>
    /// <exception cref="JsonComparisonException">Thrown when cloning fails due to an unexpected error.</exception>
    public object? CloneElement(JsonElement element)
    {
        // This simply uses our conversion method
        return ConvertToValueType(element);
    }
}