using System;
using System.Text.Json;

namespace AJL.Utility
{
    public static partial class JsonTools
    {
        // JSON serialization settings for minification
        private static readonly JsonSerializerOptions MinifySettings = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Minifies a JSON string by removing unnecessary whitespace and formatting.
        /// </summary>
        /// <param name="json">The JSON string to minify.</param>
        /// <returns>A minified JSON string.</returns>
        /// <exception cref="JsonException">Thrown if there is an error while parsing or serializing the JSON.</exception>
        public static string Minify(this string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc.RootElement, MinifySettings);
            }
            catch (Exception ex)
            {
                // Ideally, handle this exception more gracefully, e.g., logging it or throwing a custom exception.
                throw new JsonException("An error occurred while minifying the JSON.", ex);
            }
        }
    }
}