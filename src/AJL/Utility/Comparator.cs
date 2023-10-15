using System.Text.Json;

namespace AJL.Utility;

public static partial class JsonTools
{
    /// <summary>
    /// Compares two JSON strings for equality, optionally ignoring case and/or whitespace.
    /// </summary>
    /// <param name="json1">The first JSON string to compare.</param>
    /// <param name="json2">The second JSON string to compare.</param>
    /// <param name="ignoreCase">Whether to perform a case-insensitive comparison.</param>
    /// <param name="ignoreWhitespace">Whether to ignore whitespace differences in the comparison.</param>
    /// <returns>True if the JSON strings are equal; otherwise, false.</returns>
    public static bool AreEqual(this string json1, string json2, bool ignoreCase = false, bool ignoreWhitespace = false)
    {
        try
        {
            var jsonReader1 = new Utf8JsonReader(json1.ToUtf8Bytes());
            var jsonReader2 = new Utf8JsonReader(json2.ToUtf8Bytes());

            while (jsonReader1.Read() && jsonReader2.Read())
            {
                if (!jsonReader1.TokenType.Equals(jsonReader2.TokenType))
                    return false;

                if (jsonReader1.TokenType == JsonTokenType.PropertyName ||
                    jsonReader1.TokenType == JsonTokenType.String)
                {
                    var value1 = jsonReader1.GetString();
                    var value2 = jsonReader2.GetString();

                    if (ignoreWhitespace)
                    {
                        if (string.Compare(value1?.Trim(), value2?.Trim(), StringComparison.Ordinal) != 0)
                            return false;
                    }
                    else if (ignoreCase)
                    {
                        if (string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase) != 0)
                            return false;
                    }
                    else
                    {
                        if (value1 != value2)
                            return false;
                    }
                }
                else if (jsonReader1.TokenType == JsonTokenType.Number)
                {
                    if (jsonReader1.GetDecimal() != jsonReader2.GetDecimal())
                        return false;
                }
            }

            return !jsonReader1.Read() && !jsonReader2.Read();
        }
        catch (Exception)
        {
            return false;
        }
    }


    /// <summary>
    /// Converts a string to its UTF-8 byte representation.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The UTF-8 byte representation of the string.</returns>
    private static byte[] ToUtf8Bytes(this string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }


}