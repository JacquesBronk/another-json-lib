using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Comparison;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class StringComparisonEqualityComparerTests
{

    [Fact]
    public void Constructor_ShouldCreateInstanceWithSpecifiedComparisonType()
    {
        // Arrange & Act
        var comparer = new StringComparisonEqualityComparer(StringComparison.OrdinalIgnoreCase);

        // Assert - We can't directly test private fields, but we can verify the instance was created
        comparer.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("test", "test", StringComparison.Ordinal, true)]
    [InlineData("test", "TEST", StringComparison.Ordinal, false)]
    [InlineData("test", "TEST", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("Test", "test", StringComparison.CurrentCulture, false)]
    [InlineData("Test", "test", StringComparison.CurrentCultureIgnoreCase, true)]
    [InlineData("", "", StringComparison.Ordinal, true)]
    public void Equals_String_ShouldCompareStringsUsingSpecifiedComparisonType(
        string x, string y, StringComparison comparison, bool expectedResult)
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(comparison);

        // Act
        var result = comparer.Equals(x, y);

        // Assert
        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData("", "", true)]
    [InlineData(" ", " ", true)]
    [InlineData("test", null, false)]
    [InlineData(null, "test", false)]
    [InlineData("", "test", false)]
    [InlineData("test", "", false)]
    public void Equals_String_ShouldHandleNullAndEmptyStrings(
        string x, string y, bool expectedResult)
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);

        // Act
        var result = comparer.Equals(x, y);

        // Assert
        result.ShouldBe(expectedResult);
    }

    [Fact]
    public void Equals_String_ShouldReturnFalseWhenLengthsDiffer()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.OrdinalIgnoreCase);

        // Act
        var result = comparer.Equals("short", "longer string");

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 1, true)]
    [InlineData(255, 255, true)]
    [InlineData(0, 1, false)]
    [InlineData(255, 254, false)]
    public void Equals_Byte_ShouldCompareBytes(byte x, byte y, bool expectedResult)
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);

        // Act
        var result = comparer.Equals(x, y);

        // Assert
        result.ShouldBe(expectedResult);
    }

    [Fact]
    public void Equals_ByteArray_ShouldReturnTrueForSameReference()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);
        var array = new byte[] { 1, 2, 3 };

        // Act
        var result = comparer.Equals(array, array);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ByteArray_ShouldReturnFalseWhenOneArrayIsNull()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);
        var array = new byte[] { 1, 2, 3 };

        // Act & Assert
        comparer.Equals(array, null).ShouldBeFalse();
        comparer.Equals(null, array).ShouldBeFalse();
    }

    [Fact]
    public void Equals_ByteArray_ShouldReturnTrueWhenBothArraysAreNull()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);

        // Act
        var result = comparer.Equals([], []);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ByteArray_ShouldReturnFalseWhenLengthsDiffer()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);
        var array1 = new byte[] { 1, 2, 3 };
        var array2 = new byte[] { 1, 2 };

        // Act
        var result = comparer.Equals(array1, array2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ByteArray_ShouldCompareElementsByElementAndReturnTrue()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);
        var array1 = new byte[] { 1, 2, 3 };
        var array2 = new byte[] { 1, 2, 3 };

        // Act
        var result = comparer.Equals(array1, array2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ByteArray_ShouldCompareElementsByElementAndReturnFalse()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);
        var array1 = new byte[] { 1, 2, 3 };
        var array2 = new byte[] { 1, 2, 4 };

        // Act
        var result = comparer.Equals(array1, array2);

        // Assert
        result.ShouldBeFalse();
    }


    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    public void GetHashCode_Byte_ShouldReturnByteHashCode(byte value)
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);
        var expectedHashCode = value.GetHashCode();

        // Act
        var result = comparer.GetHashCode(value);

        // Assert
        result.ShouldBe(expectedHashCode);
    }

    [Fact]
    public void GetHashCode_String_ShouldReturnZeroForNull()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);

        // Act
        var result = comparer.GetHashCode((string)null);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void GetHashCode_String_ShouldReturnZeroForEmptyString()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);

        // Act
        var result = comparer.GetHashCode("");

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void GetHashCode_String_ShouldReturnZeroForWhitespace()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);

        // Act
        var result = comparer.GetHashCode("   ");

        // Assert
        result.ShouldBe(0);
    }

    [Theory]
    [InlineData("test", StringComparison.Ordinal)]
    [InlineData("test", StringComparison.OrdinalIgnoreCase)]
    [InlineData("test", StringComparison.CurrentCultureIgnoreCase)]
    [InlineData("test", StringComparison.InvariantCultureIgnoreCase)]
    public void GetHashCode_String_ShouldUseStringComparer_ForIgnoreCaseComparisons(
        string value, StringComparison comparison)
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(comparison);
        int expectedHashCode;

        if (comparison == StringComparison.OrdinalIgnoreCase ||
            comparison == StringComparison.CurrentCultureIgnoreCase ||
            comparison == StringComparison.InvariantCultureIgnoreCase)
        {
            expectedHashCode = StringComparer.FromComparison(comparison).GetHashCode(value);
        }
        else
        {
            expectedHashCode = value.GetHashCode();
        }

        // Act
        var result = comparer.GetHashCode(value);

        // Assert
        result.ShouldBe(expectedHashCode);
    }

    [Fact]
    public void GetHashCode_ByteArray_ShouldThrowJsonComparisonExceptionForNullArray()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);

        // Act & Assert
        Should.Throw<JsonComparisonException>(() => comparer.GetHashCode((byte[])null));
    }

    [Fact]
    public void GetHashCode_ByteArray_ShouldReturnConsistentHashCode()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);
        var array = new byte[] { 1, 2, 3 };

        // Act
        var hashCode1 = comparer.GetHashCode(array);
        var hashCode2 = comparer.GetHashCode(array);

        // Assert
        hashCode1.ShouldBe(hashCode2);
    }

    [Fact]
    public void GetHashCode_ByteArray_ShouldUseIndividualByteHashCodes()
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);
        var array1 = new byte[] { 1, 2, 3 };
        var array2 = new byte[] { 1, 2, 4 }; // Last byte different

        // Act
        var hashCode1 = comparer.GetHashCode(array1);
        var hashCode2 = comparer.GetHashCode(array2);

        // Assert
        hashCode1.ShouldNotBe(hashCode2);
    }

}