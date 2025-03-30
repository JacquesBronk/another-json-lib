using System.Text.Json;
using AnotherJsonLib.Domain;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Helper;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Comparison;

/// <summary>
/// Provides functionality to compute differences between JSON documents, identifying added, 
/// modified, and removed properties.
/// 
/// JSON diffing is essential for tracking changes between document versions, implementing
/// undo/redo operations, creating audit trails, and building synchronization systems. 
/// This utility helps with:
/// 
/// - Detecting what fields changed between two JSON versions
/// - Tracking the old and new values of modified properties
/// - Creating detailed reports of document changes
/// - Enabling intelligent merging or conflict resolution
/// - Supporting data synchronization and versioning scenarios
/// 
/// <example>
/// <code>
/// // Original JSON representing a user profile
/// string originalJson = @"{
///   ""name"": ""John Smith"",
///   ""email"": ""john@example.com"",
///   ""age"": 30,
///   ""address"": {
///     ""city"": ""New York"",
///     ""zip"": ""10001""
///   }
/// }";
/// 
/// // Updated JSON with some changes
/// string updatedJson = @"{
///   ""name"": ""John Smith"",
///   ""email"": ""john.smith@example.com"",
///   ""age"": 31,
///   ""address"": {
///     ""city"": ""Boston"",
///     ""zip"": ""02108""
///   },
///   ""phone"": ""555-1234""
/// }";
/// 
/// // Compute the differences
/// var diff = JsonDiffer.ComputeDiff(originalJson, updatedJson);
/// 
/// // The diff contains:
/// // - Added: {"phone": "555-1234"}
/// // - Removed: {}
/// // - Modified: {
/// //     "email": { OldValue: "john@example.com", NewValue: "john.smith@example.com" },
/// //     "age": { OldValue: 30, NewValue: 31 },
/// //     "address": { ... nested differences ... }
/// //   }
/// </code>
/// </example>
/// </summary>
public static class JsonDiffer
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonDiffer));

    /// <summary>
    /// Computes a bidirectional diff between two JSON strings.
    /// Returns keys added, removed, or modified (with old and new values).
    /// 
    /// <para>
    /// The resulting JsonDiffResult contains three dictionaries:
    /// - Added: Properties present in the new JSON but not in the original
    /// - Removed: Properties present in the original JSON but not in the new
    /// - Modified: Properties present in both but with different values
    /// </para>
    /// 
    /// <example>
    /// <code>
    /// // Simple document comparison
    /// string originalJson = @"{""name"":""John"",""age"":30,""active"":true}";
    /// string newJson = @"{""name"":""John"",""age"":31,""status"":""active""}";
    /// 
    /// JsonDiffResult diff = JsonDiffer.ComputeDiff(originalJson, newJson);
    /// 
    /// // diff.Added contains {"status": "active"}
    /// // diff.Removed contains {"active": true}
    /// // diff.Modified contains {"age": { OldValue = 30, NewValue = 31 }}
    /// 
    /// // You can check if a specific property was modified:
    /// if (diff.Modified.TryGetValue("age", out var ageChange))
    /// {
    ///     Console.WriteLine($"Age changed from {ageChange.OldValue} to {ageChange.NewValue}");
    /// }
    /// 
    /// // Or enumerate all changes:
    /// foreach (var added in diff.Added)
    ///     Console.WriteLine($"Added: {added.Key} = {added.Value}");
    /// 
    /// foreach (var removed in diff.Removed)
    ///     Console.WriteLine($"Removed: {removed.Key} = {removed.Value}");
    /// 
    /// foreach (var modified in diff.Modified)
    ///     Console.WriteLine($"Modified: {modified.Key} from {modified.Value.OldValue} to {modified.Value.NewValue}");
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="newJson">The new JSON string to compare against the original.</param>
    /// <returns>A JsonDiffResult containing the differences between the two JSON strings.</returns>
    /// <exception cref="JsonArgumentException">Thrown when the input JSON is null, empty, or not valid JSON.</exception>
    /// <exception cref="JsonOperationException">Thrown when an operation fails during the diff process.</exception>
    /// <exception cref="JsonParsingException">Thrown when the JSON cannot be parsed.</exception>
    public static JsonDiffResult ComputeDiff(this string originalJson, string newJson)
    {
        using var performance = new PerformanceTracker(Logger, nameof(ComputeDiff));

        return ExceptionHelpers.SafeExecute(() =>
            {
                ExceptionHelpers.ThrowIfNullOrWhiteSpace(originalJson, nameof(originalJson));
                ExceptionHelpers.ThrowIfNullOrWhiteSpace(newJson, nameof(newJson));

                var result = new JsonDiffResult();
                var comparer = new JsonElementComparer();

                Logger.LogDebug(
                    "Computing diff between original JSON ({OriginalLength} chars) and new JSON ({NewLength} chars)",
                    originalJson.Length, newJson.Length);

                var originalDict = originalJson.FromJson<Dictionary<string, JsonElement>>(
                    options: new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var newDict = newJson.FromJson<Dictionary<string, JsonElement>>(
                    options: new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Validate parsed results
                if (originalDict == null || newDict == null)
                {
                    throw new JsonParsingException("Failed to parse one or both JSON inputs as dictionaries");
                }

                // Process added or modified properties
                foreach (var kvp in newDict)
                {
                    if (!originalDict.ContainsKey(kvp.Key))
                    {
                        // Property exists in new but not in original - it was added
                        var addedValue = comparer.ConvertToValueType(kvp.Value) ?? new JsonElement();
                        result.Added[kvp.Key] = addedValue;
                        Logger.LogTrace("Added property: {Property} = {Value}", kvp.Key, addedValue);
                    }
                    else if (!comparer.Equals(originalDict[kvp.Key], kvp.Value))
                    {
                        // Property exists in both but values differ - it was modified
                        var oldValue = comparer.ConvertToValueType(originalDict[kvp.Key]) ?? new JsonElement();
                        var newValue = comparer.ConvertToValueType(kvp.Value) ?? new JsonElement();

                        // For nested objects, compute a nested diff
                        JsonDiffResult? nestedDiff = null;
                        if (originalDict[kvp.Key].ValueKind == JsonValueKind.Object &&
                            kvp.Value.ValueKind == JsonValueKind.Object)
                        {
                            string oldJson = JsonSerializer.Serialize(originalDict[kvp.Key]);
                            string newJsonString = JsonSerializer.Serialize(kvp.Value);
                            nestedDiff = ComputeDiff(oldJson, newJsonString);
                        }
                        // NEW CODE: Add handling for arrays
                        else if (originalDict[kvp.Key].ValueKind == JsonValueKind.Array &&
                                 kvp.Value.ValueKind == JsonValueKind.Array)
                        {
                            nestedDiff = new JsonDiffResult();
                            var originalArray = originalDict[kvp.Key].EnumerateArray().ToList();
                            var newArray = kvp.Value.EnumerateArray().ToList();

                            // Compare array elements
                            for (int i = 0; i < Math.Max(originalArray.Count, newArray.Count); i++)
                            {
                                // Element exists in both arrays and can be compared
                                if (i < originalArray.Count && i < newArray.Count)
                                {
                                    var origElement = originalArray[i];
                                    var newElement = newArray[i];

                                    if (!comparer.Equals(origElement, newElement))
                                    {
                                        // Element at position i was modified
                                        var elemOldValue = comparer.ConvertToValueType(origElement) ??
                                                           new JsonElement();
                                        var elemNewValue = comparer.ConvertToValueType(newElement) ?? new JsonElement();

                                        // Handle nested objects within array elements
                                        JsonDiffResult? elemNestedDiff = null;
                                        if (origElement.ValueKind == JsonValueKind.Object &&
                                            newElement.ValueKind == JsonValueKind.Object)
                                        {
                                            string elemOldJson = JsonSerializer.Serialize(origElement);
                                            string elemNewJson = JsonSerializer.Serialize(newElement);
                                            elemNestedDiff = ComputeDiff(elemOldJson, elemNewJson);
                                        }

                                        nestedDiff.Modified[i.ToString()] = new DiffEntry
                                        {
                                            OldValue = elemOldValue,
                                            NewValue = elemNewValue,
                                            NestedDiff = elemNestedDiff ?? new JsonDiffResult()
                                        };

                                        Logger.LogTrace(
                                            "Modified array element at index {Index} in property {Property}",
                                            i, kvp.Key);
                                    }
                                }
                                // Element exists only in original array (was removed)
                                else if (i < originalArray.Count)
                                {
                                    var removedValue = comparer.ConvertToValueType(originalArray[i]) ??
                                                       new JsonElement();
                                    nestedDiff.Removed[i.ToString()] = removedValue;
                                    Logger.LogTrace("Removed array element at index {Index} in property {Property}",
                                        i, kvp.Key);
                                }
                                // Element exists only in new array (was added)
                                else if (i < newArray.Count)
                                {
                                    var addedValue = comparer.ConvertToValueType(newArray[i]) ?? new JsonElement();
                                    nestedDiff.Added[i.ToString()] = addedValue;
                                    Logger.LogTrace("Added array element at index {Index} in property {Property}",
                                        i, kvp.Key);
                                }
                            }
                        }

                        result.Modified[kvp.Key] = new DiffEntry
                        {
                            OldValue = oldValue,
                            NewValue = newValue,
                            NestedDiff = nestedDiff ?? new JsonDiffResult()
                        };

                        Logger.LogTrace("Modified property: {Property} from {OldValue} to {NewValue}",
                            kvp.Key, oldValue, newValue);
                    }
                }

                // Process removed properties
                foreach (var kvp in originalDict)
                {
                    if (!newDict.ContainsKey(kvp.Key))
                    {
                        var removedValue = comparer.ConvertToValueType(kvp.Value) ?? new JsonElement();
                        result.Removed[kvp.Key] = removedValue;
                        Logger.LogTrace("Removed property: {Property} = {Value}", kvp.Key, removedValue);
                    }
                }

                Logger.LogDebug(
                    "Computed JSON diff: {AddedCount} keys added, {RemovedCount} keys removed, {ModifiedCount} keys modified",
                    result.Added.Count, result.Removed.Count, result.Modified.Count);

                return result;
            },
            (ex, msg) =>
            {
                // Handle specific exception types with appropriate custom exceptions
                if (ex is ArgumentException argEx)
                    return new JsonArgumentException($"Invalid argument in JSON diff computation: {argEx.Message}",
                        argEx);

                if (ex is JsonException jsonEx)
                    return new JsonParsingException("Invalid JSON format during diff computation", jsonEx);

                if (ex is JsonLibException)
                    return (JsonLibException)ex; // Pass through our custom exceptions

                return new JsonOperationException($"Failed to compute JSON diff: {msg}", ex);
            },
            "Failed to compute JSON diff between the provided JSON strings") ?? new JsonDiffResult();
    }

    /// <summary>
    /// Attempts to compute a bidirectional diff between two JSON strings.
    /// Returns false if the operation fails and doesn't throw exceptions.
    /// 
    /// <example>
    /// <code>
    /// // Safe comparison that won't throw exceptions
    /// string originalJson = @"{""name"":""John""}";
    /// string newJson = @"{""name"":""John"",""age"":30}";
    /// 
    /// if (JsonDiffer.TryComputeDiff(originalJson, newJson, out var diff))
    /// {
    ///     // Process the diff safely
    ///     Console.WriteLine($"Found {diff.Added.Count} added properties");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Failed to compute diff, possibly invalid JSON");
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="newJson">The new JSON string to compare against the original.</param>
    /// <param name="result">When successful, contains the diff result; otherwise, null.</param>
    /// <returns>True if the diff was successfully computed; otherwise, false.</returns>
    public static bool TryComputeDiff(this string originalJson, string newJson, out JsonDiffResult? result)
    {
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => ComputeDiff(originalJson, newJson),
            null,
            "Failed to compute JSON diff"
        );

        return result != null;
    }

    /// <summary>
    /// Computes a diff between two JSON strings and returns it as a string-based report.
    /// This is useful for logging, display, or text-based output of differences.
    /// 
    /// <example>
    /// <code>
    /// // Generate a human-readable diff report
    /// string originalJson = @"{""user"":{""name"":""Alice"",""role"":""Admin""}}";
    /// string newJson = @"{""user"":{""name"":""Alice"",""role"":""User""}}";
    /// 
    /// string report = JsonDiffer.ComputeDiffReport(originalJson, newJson);
    /// Console.WriteLine(report);
    /// 
    /// // Output:
    /// // JSON Diff Report:
    /// // ----------------
    /// // Modified:
    /// //   user.role: "Admin" -> "User"
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="newJson">The new JSON string to compare.</param>
    /// <param name="includeUnchanged">Whether to include unchanged properties in the report.</param>
    /// <returns>A formatted string report of the differences.</returns>
    /// <exception cref="JsonArgumentException">Thrown when the input is invalid.</exception>
    /// <exception cref="JsonOperationException">Thrown when the diff operation fails.</exception>
    public static string ComputeDiffReport(this string originalJson, string newJson, bool includeUnchanged = false)
    {
        using var performance = new PerformanceTracker(Logger, nameof(ComputeDiffReport));

        ExceptionHelpers.ThrowIfNullOrWhiteSpace(originalJson, nameof(originalJson));
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(newJson, nameof(newJson));

        return ExceptionHelpers.SafeExecute(() =>
            {
                // Compute the diff
                var diff = ComputeDiff(originalJson, newJson);

                // Build a report
                var report = new System.Text.StringBuilder();
                report.AppendLine("JSON Diff Report:");
                report.AppendLine("----------------");

                // Added properties
                if (diff.Added.Count > 0)
                {
                    report.AppendLine("Added:");
                    foreach (var added in diff.Added.OrderBy(a => a.Key))
                    {
                        report.AppendLine($"  {added.Key}: {FormatValue(added.Value)}");
                    }

                    report.AppendLine();
                }

                // Removed properties
                if (diff.Removed.Count > 0)
                {
                    report.AppendLine("Removed:");
                    foreach (var removed in diff.Removed.OrderBy(r => r.Key))
                    {
                        report.AppendLine($"  {removed.Key}: {FormatValue(removed.Value)}");
                    }

                    report.AppendLine();
                }

                // Modified properties
                if (diff.Modified.Count > 0)
                {
                    report.AppendLine("Modified:");
                    foreach (var modified in diff.Modified.OrderBy(m => m.Key))
                    {
                        report.AppendLine(
                            $"  {modified.Key}: {FormatValue(modified.Value.OldValue)} -> {FormatValue(modified.Value.NewValue)}");

                        // If there's a nested diff with changes, include it
                        if (HasNestedChanges(modified.Value.NestedDiff))
                        {
                            AppendNestedDiff(report, modified.Key, modified.Value.NestedDiff, "    ");
                        }
                    }

                    report.AppendLine();
                }

                // No changes
                if (diff.Added.Count == 0 && diff.Removed.Count == 0 && diff.Modified.Count == 0)
                {
                    report.AppendLine("No differences found.");
                }

                return report.ToString();
            },
            (ex, msg) => new JsonOperationException($"Failed to generate diff report: {msg}", ex),
            "Failed to generate JSON diff report") ?? string.Empty;
    }

    /// <summary>
    /// Appends nested diff information to a report with proper indentation.
    /// </summary>
    private static void AppendNestedDiff(System.Text.StringBuilder report, string parentPath, JsonDiffResult nestedDiff,
        string indent)
    {
        // Added properties
        foreach (var added in nestedDiff.Added.OrderBy(a => a.Key))
        {
            report.AppendLine($"{indent}{parentPath}.{added.Key} (Added): {FormatValue(added.Value)}");
        }

        // Removed properties
        foreach (var removed in nestedDiff.Removed.OrderBy(r => r.Key))
        {
            report.AppendLine($"{indent}{parentPath}.{removed.Key} (Removed): {FormatValue(removed.Value)}");
        }

        // Modified properties
        foreach (var modified in nestedDiff.Modified.OrderBy(m => m.Key))
        {
            report.AppendLine(
                $"{indent}{parentPath}.{modified.Key}: {FormatValue(modified.Value.OldValue)} -> {FormatValue(modified.Value.NewValue)}");

            // Recursive handling of nested diffs
            if (HasNestedChanges(modified.Value.NestedDiff))
            {
                AppendNestedDiff(report, $"{parentPath}.{modified.Key}", modified.Value.NestedDiff, indent + "  ");
            }
        }
    }

    /// <summary>
    /// Checks if a nested diff contains any changes.
    /// </summary>
    private static bool HasNestedChanges(JsonDiffResult? nestedDiff)
    {
        return nestedDiff != null &&
               (nestedDiff.Added.Count > 0 || nestedDiff.Removed.Count > 0 || nestedDiff.Modified.Count > 0);
    }

    /// <summary>
    /// Formats a value for display in the diff report.
    /// </summary>
    private static string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        if (value is string stringValue)
            return $"\"{stringValue}\"";

        if (value is bool boolValue)
            return boolValue ? "true" : "false";

        if (value is Dictionary<string, object?> dict)
        {
            // Format dictionary with actual content
            if (dict.Count == 0)
                return "{}";

            var entries = dict.Select(kvp => $"\"{kvp.Key}\": {FormatValue(kvp.Value)}");
            return $"{{{string.Join(", ", entries)}}}";
        }

        if (value is List<object?> list)
        {
            // Format list with actual content
            if (list.Count == 0)
                return "[]";

            var items = list.Select(FormatValue);
            return $"[{string.Join(", ", items)}]";
        }

        // For numeric types and others, use their string representation
        return value.ToString() ?? "null";
    }

    /// <summary>
    /// Computes a diff and determines if there are any differences between two JSON strings.
    /// This is optimized for quickly checking if documents are different without generating a full diff report.
    /// 
    /// <example>
    /// <code>
    /// // Quick check if two documents are different
    /// string doc1 = @"{""id"":123,""data"":{""value"":50}}";
    /// string doc2 = @"{""id"":123,""data"":{""value"":50}}";
    /// 
    /// bool hasDifferences = JsonDiffer.HasDifferences(doc1, doc2);
    /// Console.WriteLine(hasDifferences ? "Documents are different" : "Documents are identical");
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="newJson">The new JSON string to compare.</param>
    /// <returns>True if the documents have differences; otherwise, false.</returns>
    /// <exception cref="JsonArgumentException">Thrown when input is invalid.</exception>
    /// <exception cref="JsonOperationException">Thrown when the comparison fails.</exception>
    /// <exception cref="JsonParsingException">Thrown when the JSON cannot be parsed.</exception>
    public static bool HasDifferences(this string originalJson, string newJson)
    {
        using var performance = new PerformanceTracker(Logger, nameof(HasDifferences));

        ExceptionHelpers.ThrowIfNullOrWhiteSpace(originalJson, nameof(originalJson));
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(newJson, nameof(newJson));

        return ExceptionHelpers.SafeExecute(() =>
            {
                // For simple string equality, we can quickly return
                if (originalJson == newJson)
                {
                    Logger.LogTrace("JSON strings are identical, no differences");
                    return false;
                }

                // Parse and compare the JSON
                using var originalDoc = JsonDocument.Parse(originalJson);
                using var newDoc = JsonDocument.Parse(newJson);

                // Use JsonElementComparer to perform fast comparison
                var comparer = new JsonElementComparer();
                bool areEqual = comparer.Equals(originalDoc.RootElement, newDoc.RootElement);

                Logger.LogDebug("JSON comparison result: documents are {Result}",
                    areEqual ? "identical" : "different");

                return !areEqual; // Return true if they are different
            },
            (ex, msg) =>
            {
                if (ex is JsonException)
                    return new JsonParsingException("Invalid JSON format during comparison.", ex);

                return new JsonOperationException($"Failed to compare JSON documents: {msg}", ex);
            },
            "Failed to compare JSON documents");
    }

    /// <summary>
    /// Attempts to determine if there are differences between two JSON strings without throwing exceptions.
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="newJson">The new JSON string to compare.</param>
    /// <param name="hasDifferences">When successful, indicates whether the documents have differences; otherwise, null.</param>
    /// <returns>True if the comparison was performed successfully; otherwise, false.</returns>
    public static bool TryHasDifferences(this string originalJson, string newJson, out bool? hasDifferences)
    {
        if (string.IsNullOrWhiteSpace(originalJson) || string.IsNullOrWhiteSpace(newJson))
        {
            hasDifferences = null;
            return false;
        }

        try
        {
            hasDifferences = HasDifferences(originalJson, newJson);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error checking for differences between JSON documents");
            hasDifferences = null;
            return false;
        }
    }
}