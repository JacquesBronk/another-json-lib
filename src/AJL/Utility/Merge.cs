using System.Text.Json;

namespace AJL.Utility;

public static partial class JsonTools
{
    /// <summary>
    /// Merges two JSON strings. In the case of overlapping keys, values from the patch document are preferred.
    /// Arrays from both original and patch are appended.
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="patchJson">The JSON string that contains values to be merged into the original.</param>
    /// <returns>A new JSON string that results from merging the original and patch JSON strings.</returns>
    public static string Merge(this string originalJson, string patchJson)
    {
        using var originalDoc = JsonDocument.Parse(originalJson);
        using var patchDoc = JsonDocument.Parse(patchJson);

        using var memStream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(memStream);

        Merge(originalDoc.RootElement, patchDoc.RootElement, writer);

        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(memStream.ToArray());
    }

    private static void Merge(JsonElement source, JsonElement patch, Utf8JsonWriter writer)
    {
        switch (source.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();

                foreach (var prop in source.EnumerateObject())
                {
                    if (patch.TryGetProperty(prop.Name, out var patchValue))
                    {
                        writer.WritePropertyName(prop.Name);
                        Merge(prop.Value, patchValue, writer);
                    }
                    else
                    {
                        prop.WriteTo(writer);
                    }
                }

                // write properties that exist only in the patch document
                foreach (var prop in patch.EnumerateObject())
                {
                    if (!source.TryGetProperty(prop.Name, out _))
                    {
                        prop.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                // Here we're appending arrays; if you want to replace, just write patch array
                writer.WriteStartArray();

                foreach (var item in source.EnumerateArray())
                {
                    item.WriteTo(writer);
                }

                foreach (var item in patch.EnumerateArray())
                {
                    item.WriteTo(writer);
                }

                writer.WriteEndArray();
                break;

            default:
                patch.WriteTo(writer); // Prefer the patch value over the source value
                break;
        }
    }
}