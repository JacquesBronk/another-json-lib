using System.Text.Json;
using AnotherJsonLib.Domain;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Comparison;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class AdvancedArrayDifferTests
{
    [Fact]
    public void GenerateArrayPatch_FullDiff_ShouldReturnOperations()
    {
        // Arrange: original array and updated array.
        string originalArrayJson = "[1, 2, 3]";
        string updatedArrayJson = "[1, 4, 3, 5]";
        using var originalDoc = JsonDocument.Parse(originalArrayJson);
        using var updatedDoc = JsonDocument.Parse(updatedArrayJson);
        JsonElement originalElement = originalDoc.RootElement;
        JsonElement updatedElement = updatedDoc.RootElement;
        string basePath = "/items";

        // Act: generate patch operations using full diff mode.
        List<JsonPatchOperation>? operations = AdvancedArrayDiffer.GenerateArrayPatch(
            basePath, originalElement, updatedElement, ArrayDiffMode.Full);

        // Assert: operations list should not be null and should contain expected operations.
        operations.ShouldNotBeNull();
        operations.Count.ShouldBeGreaterThan(0);

        // For instance, in full diff mode, we expect:
        // - Some operation to add the new element at index 3 ("5")
        // - Some operation for the difference at index 1 (from 2 to 4)
        bool hasAdd = operations.Exists(op => op.Op == "add" && op.Path == $"{basePath}/3");
        bool hasReplaceOrRemove = operations.Exists(op => op.Op == "replace" || op.Op == "remove");
        hasAdd.ShouldBeTrue();
        hasReplaceOrRemove.ShouldBeTrue();
    }

    [Fact]
    public void GenerateArrayPatch_FastDiff_ShouldReturnReplaceOperation()
    {
        // Arrange: two JSON arrays with one element changed.
        string originalArrayJson = "[\"a\", \"b\", \"c\"]";
        string updatedArrayJson = "[\"a\", \"x\", \"c\"]";
        using var originalDoc = JsonDocument.Parse(originalArrayJson);
        using var updatedDoc = JsonDocument.Parse(updatedArrayJson);
        JsonElement originalElement = originalDoc.RootElement;
        JsonElement updatedElement = updatedDoc.RootElement;
        string basePath = "/data";

        // Act: generate patch operations using fast diff mode.
        List<JsonPatchOperation>? operations = AdvancedArrayDiffer.GenerateArrayPatch(
            basePath, originalElement, updatedElement, ArrayDiffMode.Fast);

        // Assert: operations should detect a replacement at index 1.
        operations.ShouldNotBeNull();
        operations.Count.ShouldBeGreaterThan(0);
        bool hasReplace = operations.Exists(op => op.Op == "replace" && op.Path == $"{basePath}/1");
        hasReplace.ShouldBeTrue();
    }

    [Fact]
    public void GenerateArrayPatch_IdenticalArrays_ShouldReturnEmptyPatch()
    {
        // Arrange
        string json = "[\"x\", \"y\", \"z\"]";
        using var doc1 = JsonDocument.Parse(json);
        using var doc2 = JsonDocument.Parse(json);
        string basePath = "/data";

        // Act
        List<JsonPatchOperation>? operations = AdvancedArrayDiffer.GenerateArrayPatch(
            basePath, doc1.RootElement, doc2.RootElement, ArrayDiffMode.Full);

        // Assert: no operations are needed.
        operations.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateArrayPatch_EmptyArrays_ShouldReturnEmptyPatch()
    {
        // Arrange
        string json1 = "[]";
        string json2 = "[]";
        using var doc1 = JsonDocument.Parse(json1);
        using var doc2 = JsonDocument.Parse(json2);
        string basePath = "/list";

        // Act
        List<JsonPatchOperation>? operations = AdvancedArrayDiffer.GenerateArrayPatch(
            basePath, doc1.RootElement, doc2.RootElement, ArrayDiffMode.Full);

        // Assert: the patch operations list should be empty.
        operations.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateArrayPatch_FullDiff_MoveOptimization_ShouldGenerateMoveOp()
    {
        // Arrange:
        // Original and updated arrays are reordered such that a matching element is removed and then added,
        // which should be optimized to a "move" operation.
        string originalJson = "[1, 2, 3]";
        string updatedJson = "[2, 1, 3]";
        using var origDoc = JsonDocument.Parse(originalJson);
        using var updDoc = JsonDocument.Parse(updatedJson);
        string basePath = "/array";

        // Act
        List<JsonPatchOperation>? ops = AdvancedArrayDiffer.GenerateArrayPatch(
            basePath, origDoc.RootElement, updDoc.RootElement, ArrayDiffMode.Full);

        // Assert: expect at least one "move" operation.
        ops.ShouldNotBeNull();
        bool hasMove = ops.Exists(op => op.Op == "move");
        hasMove.ShouldBeTrue("Expected a move operation due to reordered elements.");

        // Optionally, check that the move op has plausible 'From' and 'Path' values.
        var moveOp = ops.FirstOrDefault(op => op.Op == "move");
        moveOp.ShouldNotBeNull();
        // We expect a move between different indices. For example, if the LCS selects [1,3] then:
        //   remove op at index 1 (value 2) and add op at index 0 (value 2) are optimized.
        // The expected move might be from "/array/1" to "/array/0".
        // Adjust these expectations if your LCS produces different indices.
        moveOp.From.ShouldBe("/array/1");
        moveOp.Path.ShouldBe("/array/0");
    }


    [Fact]
    public void GenerateArrayPatch_InvalidInput_ShouldThrowJsonArgumentException()
    {
        // Arrange: Provide JSON that is not an array.
        string notAnArrayJson = "{\"key\":\"value\"}";
        using var doc = JsonDocument.Parse(notAnArrayJson);
        JsonElement notArray = doc.RootElement;
        string basePath = "/items";

        // Act & Assert: Expect a JsonArgumentException because both inputs must be arrays.
        Should.Throw<JsonArgumentException>(() =>
            AdvancedArrayDiffer.GenerateArrayPatch(basePath, notArray, notArray));
    }
}