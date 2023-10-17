using System.Text.Json;

namespace AnotherJsonLib.Utility;

public static partial class JsonTools
{
    /// <summary>
    /// Streams a JSON file from a provided path and executes a callback function for each JSON token.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <param name="callback">Callback function to execute for each JSON token.</param>
    public static void StreamJsonFile(this string filePath, Action<JsonTokenType, string?> callback)
    {
        using FileStream fs = new FileStream(filePath, FileMode.Open);
        StreamJson(fs, callback);
    }


    public static void StreamJson(this Stream jsonStream, Action<JsonTokenType, string?> callback)
    {
        var buffer = new byte[4096];

        while (true)
        {
            int bytesRead = jsonStream.Read(buffer, 0, buffer.Length);

            if (bytesRead == 0)
                break; // End of stream

            var reader = new Utf8JsonReader(buffer.AsSpan(0, bytesRead));

            while (reader.Read())
            {
                string? tokenValue =
                    reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.PropertyName
                        ? reader.GetString()
                        : null;
                callback(reader.TokenType, tokenValue);
            }
        }
    }
}