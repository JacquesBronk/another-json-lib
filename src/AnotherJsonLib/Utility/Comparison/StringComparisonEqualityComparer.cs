using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Helper;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Comparison;

/// <summary>
/// Provides custom string comparison for JSON operations, allowing for configurable string comparison strategies.
/// </summary>
/// <remarks>
/// This class implements both IEqualityComparer&lt;string&gt; and IEqualityComparer&lt;byte&gt;,
/// enabling flexible comparison of strings and byte arrays using different string comparison rules.
/// It is particularly useful for case-insensitive or culture-specific JSON comparisons.
/// </remarks>
public class StringComparisonEqualityComparer : IEqualityComparer<byte>
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(StringComparisonEqualityComparer));
    private readonly StringComparison _comparison;

    /// <summary>
    /// Initializes a new instance of the StringComparisonEqualityComparer class with the specified comparison type.
    /// </summary>
    /// <param name="comparison">The StringComparison option to use for comparisons.</param>
    /// <example>
    /// <code>
    /// // Create a case-insensitive comparer
    /// var comparer = new StringComparisonEqualityComparer(StringComparison.OrdinalIgnoreCase);
    /// 
    /// // Create a culture-specific comparer
    /// var culturalComparer = new StringComparisonEqualityComparer(StringComparison.CurrentCulture);
    /// </code>
    /// </example>
    public StringComparisonEqualityComparer(StringComparison comparison)
    {
        _comparison = comparison;
        Logger.LogTrace("Created StringComparisonEqualityComparer with {Comparison} mode", comparison);
    }

    /// <summary>
    /// Determines whether two strings are equal using the configured StringComparison option.
    /// </summary>
    /// <param name="x">The first string to compare.</param>
    /// <param name="y">The second string to compare.</param>
    /// <returns>True if the strings are equal according to the configured comparison rules; otherwise, false.</returns>
    public bool Equals(string x, string y)
    {
        using var performance = new PerformanceTracker(Logger, nameof(Equals) + ".String");

        if (string.IsNullOrWhiteSpace(x) && string.IsNullOrWhiteSpace(y))
        {
            Logger.LogTrace("Both strings are null or whitespace, returning true");
            return string.IsNullOrWhiteSpace(x) && string.IsNullOrWhiteSpace(y);
        }
        
        if (string.IsNullOrWhiteSpace(x) || string.IsNullOrWhiteSpace(y))
        {
            Logger.LogTrace("One of the strings is null or whitespace, returning false");
            return false;
        }
        
        if (x.Length != y.Length)
        {
            Logger.LogTrace("Strings have different lengths, returning false");
            return false;
        }
        
        return ExceptionHelpers.SafeExecuteWithDefault(() => 
        {
            bool result = string.Equals(x, y, _comparison);
            Logger.LogTrace("String comparison result for '{X}' and '{Y}' with {Comparison}: {Result}", 
                x.Substring(0, Math.Min(10, (x.Length))), 
                y.Substring(0, Math.Min(10, (y.Length))), 
                _comparison, result);
            return result;
        }, 
        false,
        "String comparison failed");
    }

    /// <summary>
    /// Determines whether two bytes are equal.
    /// </summary>
    /// <param name="x">The first byte to compare.</param>
    /// <param name="y">The second byte to compare.</param>
    /// <returns>True if the bytes are equal; otherwise, false.</returns>
    public bool Equals(byte x, byte y)
    {
        return ExceptionHelpers.SafeExecuteWithDefault(() => x.Equals(y), false, "Byte comparison failed");
    }

    /// <summary>
    /// Gets the hash code for a byte.
    /// </summary>
    /// <param name="obj">The byte to get a hash code for.</param>
    /// <returns>A hash code for the specified byte.</returns>
    public int GetHashCode(byte obj)
    {
        return ExceptionHelpers.SafeExecuteWithDefault(obj.GetHashCode, 0, "Failed to get hash code for byte");
    }

    /// <summary>
    /// Gets the hash code for a string using the configured StringComparison option.
    /// </summary>
    /// <param name="obj">The string to get a hash code for.</param>
    /// <returns>
    /// A hash code for the specified string that is consistent with the equals method,
    /// or 0 if the string is null.
    /// </returns>
    public int GetHashCode(string obj)
    {
        return ExceptionHelpers.SafeExecuteWithDefault(() => 
        {
            if (string.IsNullOrWhiteSpace(obj))
                return 0;
                
            // For case-insensitive comparisons, use a case-insensitive hash code
            if (_comparison == StringComparison.OrdinalIgnoreCase || 
                _comparison == StringComparison.CurrentCultureIgnoreCase || 
                _comparison == StringComparison.InvariantCultureIgnoreCase)
            {
                return StringComparer.FromComparison(_comparison).GetHashCode(obj);
            }
            
            return obj.GetHashCode();
        }, 
        0, "Failed to get hash code for string");
    }

    /// <summary>
    /// Determines whether two byte arrays are equal by comparing each byte.
    /// </summary>
    /// <param name="x">The first byte array to compare.</param>
    /// <param name="y">The second byte array to compare.</param>
    /// <returns>True if the byte arrays are equal; otherwise, false.</returns>
    /// <exception cref="JsonComparisonException">Thrown when the comparison fails unexpectedly.</exception>
    public bool Equals(byte[]? x, byte[]? y)
    {
        using var performance = new PerformanceTracker(Logger, nameof(Equals) + ".ByteArray");
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            if (x == y)
                return true;
                
            if (x == null || y == null)
                return false;
                
            if (x.Length != y.Length)
                return false;
                
            return x.SequenceEqual(y, this);
        }, 
        (ex, msg) => new JsonComparisonException($"Failed to compare byte arrays: {msg}", ex),
        "Error comparing byte arrays");
    }

    /// <summary>
    /// Gets the hash code for a byte array.
    /// </summary>
    /// <param name="obj">The byte array to get a hash code for.</param>
    /// <returns>A hash code for the specified byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the byte array is null.</exception>
    /// <exception cref="JsonComparisonException">Thrown when hash code calculation fails.</exception>
    public int GetHashCode(byte[] obj)
    {
        using var performance = new PerformanceTracker(Logger, nameof(GetHashCode) + ".ByteArray");
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            ExceptionHelpers.ThrowIfNull(obj, nameof(obj));

            int hash = 17;
            foreach (byte b in obj)
            {
                hash = (hash * 31) + GetHashCode(b);
            }
            
            return hash;
        }, 
        (ex, msg) => new JsonComparisonException($"Failed to calculate hash code for byte array: {msg}", ex),
        "Error calculating hash code for byte array");
    }
}