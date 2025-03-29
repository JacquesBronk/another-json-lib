using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility;

public static class JsonTransformer
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonTransformer));

    public static object? Transform(JsonElement element, Func<object?, object?> transformFunc)
    {
        using var performance = new PerformanceTracker(Logger, nameof(Transform));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNull(transformFunc, nameof(transformFunc));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            // Recursively transform children first.
            object? transformedValue;
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var property in element.EnumerateObject())
                    {
                        dict[property.Name] = Transform(property.Value, transformFunc);
                    }

                    transformedValue = dict;
                    break;

                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(Transform(item, transformFunc));
                    }

                    transformedValue = list;
                    break;

                default:
                    transformedValue = element.CloneValue();
                    break;
            }

            // Apply the transformation function to the current node.
            return transformFunc(transformedValue);
        }, 
        (ex, msg) => new JsonTransformationException($"Failed to transform JSON element: {msg}", ex),
        "Failed to transform JSON element");
    }
    
    // Add a "Try" variant for the Transform method
    public static bool TryTransform(JsonElement element, Func<object?, object?> transformFunc, out object? result)
    {
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => Transform(element, transformFunc),
            null,
            "Failed to transform JSON element"
        );
        
        return result != null;
    }
}