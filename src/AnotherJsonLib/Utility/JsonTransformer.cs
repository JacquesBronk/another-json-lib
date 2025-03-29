using System.Text.Json;
using AnotherJsonLib.Infra;

namespace AnotherJsonLib.Utility;

public static class JsonTransformer
{
    /// <summary>
    /// Recursively transforms a JsonElement into a native .NET object, applying a custom transformation function 
    /// after processing all children. The transformation function receives the transformed value of the current node 
    /// (which may be a primitive, dictionary, or list) and returns the new value.
    /// 
    /// If the transformation function is the identity (returns its input), this method effectively clones the JSON.
    /// </summary>
    /// <param name="element">The source JsonElement.</param>
    /// <param name="transformFunc">
    /// A function that takes the current transformed node (object, list, or primitive) and returns a new value.
    /// </param>
    /// <returns>A native .NET object representing the transformed JSON.</returns>
    public static object? Transform(JsonElement element, Func<object?, object?> transformFunc)
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
    }
}