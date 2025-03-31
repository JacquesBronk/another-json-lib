using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Comparison;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonElementComparerTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var comparer = new JsonElementComparer(
            epsilon: 0.001,
            maxHashDepth: 5,
            caseSensitivePropertyNames: false);

        // Assert - if no exception is thrown, the test passes
        comparer.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(-0.001)]
    public void Constructor_WithNegativeEpsilon_ShouldThrowJsonArgumentException(double epsilon)
    {
        // Act & Assert
        Should.Throw<JsonArgumentException>(() =>
            new JsonElementComparer(epsilon: epsilon));
    }

    [Theory]
    [InlineData(-2)]
    public void Constructor_WithInvalidMaxHashDepth_ShouldThrowJsonArgumentException(int maxHashDepth)
    {
        // Act & Assert
        Should.Throw<JsonArgumentException>(() =>
            new JsonElementComparer(maxHashDepth: maxHashDepth));
    }

    [Fact]
    public void Equals_IdenticalNumbers_ShouldBeTrue()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse("42").RootElement;
        var element2 = JsonDocument.Parse("42").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_NumbersWithinEpsilon_ShouldBeTrue()
    {
        // Arrange
        var comparer = new JsonElementComparer(epsilon: 0.01);
        var element1 = JsonDocument.Parse("1.005").RootElement;
        var element2 = JsonDocument.Parse("1.01").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_NumbersOutsideEpsilon_ShouldBeFalse()
    {
        // Arrange
        var comparer = new JsonElementComparer(epsilon: 0.001);
        var element1 = JsonDocument.Parse("1.005").RootElement;
        var element2 = JsonDocument.Parse("1.01").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_IntegerAndFloatWithSameValue_ShouldBeTrue()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse("42").RootElement;
        var element2 = JsonDocument.Parse("42.0").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_NaN_ShouldBeEqual()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse("\"NaN\"").RootElement;
        var element2 = JsonDocument.Parse("\"NaN\"").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_Infinity_ShouldBeEqual()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse("\"Infinity\"").RootElement;
        var element2 = JsonDocument.Parse("\"Infinity\"").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_IdenticalStrings_ShouldBeTrue()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse("\"test\"").RootElement;
        var element2 = JsonDocument.Parse("\"test\"").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentStrings_ShouldBeFalse()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse("\"test\"").RootElement;
        var element2 = JsonDocument.Parse("\"Test\"").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_IdenticalBooleans_ShouldBeTrue()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse("true").RootElement;
        var element2 = JsonDocument.Parse("true").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentBooleans_ShouldBeFalse()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse("true").RootElement;
        var element2 = JsonDocument.Parse("false").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_NullValues_ShouldBeTrue()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse("null").RootElement;
        var element2 = JsonDocument.Parse("null").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_IdenticalObjects_ShouldBeTrue()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json = "{\"name\":\"John\",\"age\":30}";
        var element1 = JsonDocument.Parse(json).RootElement;
        var element2 = JsonDocument.Parse(json).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ObjectsWithSamePropertiesDifferentOrder_ShouldBeTrue()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json1 = "{\"name\":\"John\",\"age\":30}";
        var json2 = "{\"age\":30,\"name\":\"John\"}";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ObjectsWithDifferentPropertyCount_ShouldBeFalse()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json1 = "{\"name\":\"John\",\"age\":30}";
        var json2 = "{\"name\":\"John\",\"age\":30,\"city\":\"New York\"}";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ObjectsWithDifferentPropertyValues_ShouldBeFalse()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json1 = "{\"name\":\"John\",\"age\":30}";
        var json2 = "{\"name\":\"John\",\"age\":31}";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ObjectsWithDifferentPropertyNames_ShouldBeFalse()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json1 = "{\"name\":\"John\",\"age\":30}";
        var json2 = "{\"name\":\"John\",\"years\":30}";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ObjectsWithDifferentCasedPropertyNames_CaseSensitive_ShouldBeFalse()
    {
        // Arrange
        var comparer = new JsonElementComparer(caseSensitivePropertyNames: true);
        var json1 = "{\"Name\":\"John\",\"Age\":30}";
        var json2 = "{\"name\":\"John\",\"age\":30}";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ObjectsWithDifferentCasedPropertyNames_CaseInsensitive_ShouldBeTrue()
    {
        // Arrange
        var comparer = new JsonElementComparer(caseSensitivePropertyNames: false);
        var json1 = "{\"Name\":\"John\",\"Age\":30}";
        var json2 = "{\"name\":\"John\",\"age\":30}";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_IdenticalArrays_ShouldBeTrue()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json = "[1, 2, 3]";
        var element1 = JsonDocument.Parse(json).RootElement;
        var element2 = JsonDocument.Parse(json).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ArraysWithDifferentLength_ShouldBeFalse()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json1 = "[1, 2, 3]";
        var json2 = "[1, 2, 3, 4]";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ArraysWithSameValuesInDifferentOrder_ShouldBeFalse()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json1 = "[1, 2, 3]";
        var json2 = "[3, 2, 1]";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ArraysWithDifferentValues_ShouldBeFalse()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json1 = "[1, 2, 3]";
        var json2 = "[1, 2, 4]";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_NestedArrays_ShouldCompareDeep()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json1 = "[[1, 2], [3, 4]]";
        var json2 = "[[1, 2], [3, 4]]";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ComplexNestedStructure_ShouldCompareDeep()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json = "{\"name\":\"John\",\"scores\":[90,85,92],\"address\":{\"city\":\"New York\",\"zip\":\"10001\"}}";
        var element1 = JsonDocument.Parse(json).RootElement;
        var element2 = JsonDocument.Parse(json).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ComplexStructuresWithNumericalDifferences_ShouldRespectEpsilon()
    {
        // Arrange
        var json1 = "{\"name\":\"John\",\"scores\":[90.001,85.002,92.003]}";
        var json2 = "{\"name\":\"John\",\"scores\":[90.0015,85.0025,92.0035]}";

        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act - with permissive epsilon
        var permissiveComparer = new JsonElementComparer(epsilon: 0.001);
        bool permissiveResult = permissiveComparer.Equals(element1, element2);

        // Act - with strict epsilon
        var strictComparer = new JsonElementComparer(epsilon: 0.0001);
        bool strictResult = strictComparer.Equals(element1, element2);

        // Assert
        permissiveResult.ShouldBeTrue();
        strictResult.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentValueKinds_ShouldBeFalse()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse("42").RootElement;
        var element2 = JsonDocument.Parse("\"42\"").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_EqualElements_ShouldHaveSameHashCode()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json1 = "{\"name\":\"John\",\"age\":30}";
        var json2 = "{\"age\":30,\"name\":\"John\"}";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        int hashCode1 = comparer.GetHashCode(element1);
        int hashCode2 = comparer.GetHashCode(element2);

        // Assert
        hashCode1.ShouldBe(hashCode2);
    }

    [Fact]
    public void GetHashCode_EqualNumbersWithinEpsilon_ShouldHaveSameHashCode()
    {
        // Arrange
        var comparer = new JsonElementComparer(epsilon: 0.01);
        var element1 = JsonDocument.Parse("1.005").RootElement;
        var element2 = JsonDocument.Parse("1.01").RootElement;

        // Act
        int hashCode1 = comparer.GetHashCode(element1);
        int hashCode2 = comparer.GetHashCode(element2);

        // Assert
        hashCode1.ShouldBe(hashCode2);
    }

    [Fact]
    public void GetHashCode_MaxHashDepthRespected()
    {
        // Arrange
        var json = "{\"level1\":{\"level2\":{\"level3\":{\"level4\":{\"value\":42}}}}}";
        var element = JsonDocument.Parse(json).RootElement;

        // Create comparers with different max hash depths
        var unlimitedComparer = new JsonElementComparer(maxHashDepth: -1);
        var limitedComparer = new JsonElementComparer(maxHashDepth: 2);
        var differentLimitedComparer = new JsonElementComparer(maxHashDepth: 3);

        // Act
        int unlimitedHash = unlimitedComparer.GetHashCode(element);
        int limitedHash = limitedComparer.GetHashCode(element);
        int differentLimitedHash = differentLimitedComparer.GetHashCode(element);

        // Assert - different depths should produce different hash codes
        limitedHash.ShouldNotBe(unlimitedHash);
        limitedHash.ShouldNotBe(differentLimitedHash);
    }

    [Fact]
    public void GetHashCode_CaseSensitivityRespected()
    {
        // Arrange
        var json1 = "{\"Name\":\"John\",\"Age\":30}";
        var json2 = "{\"name\":\"John\",\"age\":30}";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        var caseSensitiveComparer = new JsonElementComparer(caseSensitivePropertyNames: true);
        var caseInsensitiveComparer = new JsonElementComparer(caseSensitivePropertyNames: false);

        // Act
        int caseSensitiveHash1 = caseSensitiveComparer.GetHashCode(element1);
        int caseSensitiveHash2 = caseSensitiveComparer.GetHashCode(element2);

        int caseInsensitiveHash1 = caseInsensitiveComparer.GetHashCode(element1);
        int caseInsensitiveHash2 = caseInsensitiveComparer.GetHashCode(element2);

        // Assert
        caseSensitiveHash1.ShouldNotBe(caseSensitiveHash2);
        caseInsensitiveHash1.ShouldBe(caseInsensitiveHash2);
    }

    [Fact]
    public void CaseInsensitive_ShouldCreateCaseInsensitiveComparer()
    {
        // Arrange
        var comparer = JsonElementComparer.CaseInsensitive();
        var json1 = "{\"Name\":\"John\",\"Age\":30}";
        var json2 = "{\"name\":\"John\",\"age\":30}";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void WithPrecision_ShouldCreateComparerWithSpecifiedPrecision()
    {
        // Arrange
        double epsilon = 0.0001;
        var comparer = JsonElementComparer.WithPrecision(epsilon);
        var element1 = JsonDocument.Parse("1.0001").RootElement;
        var element2 = JsonDocument.Parse("1.0002").RootElement;

        // Act
        bool result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBeTrue();

        // Test with stricter precision
        var stricterComparer = JsonElementComparer.WithPrecision(epsilon / 10);
        bool stricterResult = stricterComparer.Equals(element1, element2);
        stricterResult.ShouldBeFalse();
    }

    [Fact]
    public void WithPrecision_NegativeEpsilon_ShouldThrowJsonArgumentException()
    {
        // Act & Assert
        Should.Throw<JsonArgumentException>(() =>
            JsonElementComparer.WithPrecision(-0.001));
    }

    [Fact]
    public void JsonElementsEqual_ShouldUseDefaultComparer()
    {
        // Arrange
        var json1 = "{\"name\":\"John\",\"age\":30}";
        var json2 = "{\"name\":\"John\",\"age\":30.0}";
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        bool result = JsonElementComparer.JsonElementsEqual(element1, element2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void ConvertToValueType_String_ShouldReturnString()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element = JsonDocument.Parse("\"test\"").RootElement;

        // Act
        var result = comparer.ConvertToValueType(element);

        // Assert
        result.ShouldBeOfType<string>();
        result.ShouldBe("test");
    }

    [Fact]
    public void ConvertToValueType_Float_ShouldReturnDouble()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element = JsonDocument.Parse("42.5").RootElement;

        // Act
        var result = comparer.ConvertToValueType(element);

        // Assert
        result.ShouldBeOfType<double>();
        result.ShouldBe(42.5);
    }

    [Fact]
    public void ConvertToValueType_Boolean_ShouldReturnBoolean()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element = JsonDocument.Parse("true").RootElement;

        // Act
        var result = comparer.ConvertToValueType(element);

        // Assert
        result.ShouldBeOfType<bool>();
        result.ShouldBe(true);
    }

    [Fact]
    public void ConvertToValueType_Null_ShouldReturnNull()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element = JsonDocument.Parse("null").RootElement;

        // Act
        var result = comparer.ConvertToValueType(element);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertToValueType_Array_ShouldReturnList()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element = JsonDocument.Parse("[1, 2, 3]").RootElement;

        // Act
        var result = comparer.ConvertToValueType(element);

        // Assert
        result.ShouldBeOfType<List<object?>>();
        var list = result as List<object?>;
        list.ShouldNotBeNull();
        list!.Count.ShouldBe(3);
        list[0].ShouldBe(1L);
        list[1].ShouldBe(2L);
        list[2].ShouldBe(3L);
    }

    [Fact]
    public void ConvertToValueType_Object_ShouldReturnDictionary()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var element = JsonDocument.Parse("{\"name\":\"John\",\"age\":30}").RootElement;

        // Act
        var result = comparer.ConvertToValueType(element);

        // Assert
        result.ShouldBeOfType<Dictionary<string, object?>>();
        var dict = result as Dictionary<string, object?>;
        dict.ShouldNotBeNull();
        dict!.Count.ShouldBe(2);
        dict["name"].ShouldBe("John");
        dict["age"].ShouldBe(30L);
    }

    [Fact]
    public void CloneElement_ShouldReturnEquivalentObject()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var json = "{\"name\":\"John\",\"scores\":[90,85,92]}";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = comparer.CloneElement(element);

        // Assert
        result.ShouldBeOfType<Dictionary<string, object?>>();
        var dict = result as Dictionary<string, object?>;
        dict.ShouldNotBeNull();
        dict!.Count.ShouldBe(2);
        dict["name"].ShouldBe("John");

        dict["scores"].ShouldBeOfType<List<object?>>();
        var scores = dict["scores"] as List<object?>;
        scores.ShouldNotBeNull();
        scores!.Count.ShouldBe(3);
        scores[0].ShouldBe(90L);
        scores[1].ShouldBe(85L);
        scores[2].ShouldBe(92L);
    }

    [Fact]
    public void Dictionary_WithJsonElementAsKey_ShouldWorkCorrectly()
    {
        // Arrange
        var comparer = new JsonElementComparer();
        var dict = new Dictionary<JsonElement, string>(comparer);

        var key1 = JsonDocument.Parse("{\"id\":1}").RootElement;
        var key2 = JsonDocument.Parse("{\"id\":1.0}").RootElement;
        var key3 = JsonDocument.Parse("{\"id\":2}").RootElement;

        // Act
        dict[key1] = "value1";

        // Assert
        dict.ContainsKey(key2).ShouldBeTrue();
        dict.ContainsKey(key3).ShouldBeFalse();
        dict[key2].ShouldBe("value1");
    }
}