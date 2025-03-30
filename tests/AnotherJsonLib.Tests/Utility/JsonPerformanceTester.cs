using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace AnotherJsonLib.Tests.Utility;

/// <summary>
/// Provides utilities for performance testing of JSON operations
/// </summary>
public class JsonPerformanceTester
{
    private readonly JsonFaker _faker;

    public JsonPerformanceTester(int? seed = null)
    {
        _faker = new JsonFaker(seed);
    }

    /// <summary>
    /// Measures parsing performance for different sizes of JSON data
    /// </summary>
    public Dictionary<string, long> MeasureParsingPerformance(
        Func<string, object> parseFunction,
        int iterations = 100)
    {
        var results = new Dictionary<string, long>();

        // Test different JSON sizes
        foreach (var size in new[] { "Small", "Medium", "Large", "VeryLarge" })
        {
            string json = GetTestJson(size);
            var stopwatch = new Stopwatch();

            // Warm up
            parseFunction(json);

            // Measure
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                parseFunction(json);
            }

            stopwatch.Stop();

            results[size] = stopwatch.ElapsedMilliseconds / iterations; // Average ms per operation
        }

        return results;
    }

    /// <summary>
    /// Measures serialization performance for different sizes of JSON data
    /// </summary>
    public Dictionary<string, long> MeasureSerializationPerformance(
        Func<JsonNode, string> serializeFunction,
        int iterations = 100)
    {
        var results = new Dictionary<string, long>();

        // Test different JSON sizes
        foreach (var size in new[] { "Small", "Medium", "Large", "VeryLarge" })
        {
            JsonNode json = GetTestJsonNode(size);
            var stopwatch = new Stopwatch();

            // Warm up
            serializeFunction(json);

            // Measure
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                serializeFunction(json);
            }

            stopwatch.Stop();

            results[size] = stopwatch.ElapsedMilliseconds / iterations; // Average ms per operation
        }

        return results;
    }

    /// <summary>
    /// Measures memory usage during JSON parsing
    /// </summary>
    public Dictionary<string, long> MeasureMemoryUsage(
        Func<string, object> parseFunction)
    {
        var results = new Dictionary<string, long>();

        // Test different JSON sizes
        foreach (var size in new[] { "Small", "Medium", "Large", "VeryLarge" })
        {
            string json = GetTestJson(size);

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long startMemory = GC.GetTotalMemory(true);

            // Parse JSON
            var result = parseFunction(json);

            // Measure memory after parsing
            long endMemory = GC.GetTotalMemory(false);

            results[size] = endMemory - startMemory;

            // Keep reference to result to prevent premature collection
            GC.KeepAlive(result);
        }

        return results;
    }

    /// <summary>
    /// Compares performance between two different JSON parsing implementations
    /// </summary>
    public Dictionary<string, Dictionary<string, long>> CompareImplementations(
        Func<string, object> implementation1,
        Func<string, object> implementation2,
        string impl1Name = "Implementation1",
        string impl2Name = "Implementation2",
        int iterations = 100)
    {
        var results = new Dictionary<string, Dictionary<string, long>>();

        // Test different JSON sizes
        foreach (var size in new[] { "Small", "Medium", "Large" })
        {
            var sizeResults = new Dictionary<string, long>();
            string json = GetTestJson(size);

            // Test implementation 1
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                implementation1(json);
            }

            stopwatch.Stop();
            sizeResults[impl1Name] = stopwatch.ElapsedMilliseconds / iterations;

            // Test implementation 2
            stopwatch.Reset();
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                implementation2(json);
            }

            stopwatch.Stop();
            sizeResults[impl2Name] = stopwatch.ElapsedMilliseconds / iterations;

            results[size] = sizeResults;
        }

        return results;
    }

    /// <summary>
    /// Gets a test JSON string of the specified size
    /// </summary>
    public string GetTestJson(string size)
    {
        var jsonNode = GetTestJsonNode(size);
        return jsonNode.ToJsonString();
    }

    /// <summary>
    /// Gets a test JsonNode of the specified size
    /// </summary>
    public JsonNode GetTestJsonNode(string size)
    {
        return size switch
        {
            "Small" => _faker.GenerateSimpleObject(5),
            "Medium" => _faker.GenerateComplexObject(2, 5),
            "Large" => _faker.GenerateComplexObject(3, 10),
            "VeryLarge" => _faker.GenerateComplexObject(4, 15),
            _ => throw new ArgumentException($"Unknown size: {size}")
        };
    }

    /// <summary>
    /// Generates large JSON files for stress testing
    /// </summary>
    public void GenerateTestFiles(string directory)
    {
        Directory.CreateDirectory(directory);

        // Create test files of different sizes
        foreach (var size in new[] { "Small", "Medium", "Large", "VeryLarge" })
        {
            string json = GetTestJson(size);
            File.WriteAllText(Path.Combine(directory, $"{size}.json"), json);
        }

        // Create special test files
        File.WriteAllText(Path.Combine(directory, "DeepNesting.json"),
            CreateNestedObject(50).ToJsonString());

        File.WriteAllText(Path.Combine(directory, "WideObject.json"),
            CreateWideObject(1000).ToJsonString());

        File.WriteAllText(Path.Combine(directory, "LongArray.json"),
            CreateLongArray(5000).ToJsonString());
    }

    public JsonNode CreateNestedObject(int depth)
    {
        if (depth <= 0)
            return JsonValue.Create("leaf");

        var obj = new JsonObject();
        obj.Add("nested", CreateNestedObject(depth - 1));
        return obj;
    }

    public JsonObject CreateWideObject(int propertyCount)
    {
        var obj = new JsonObject();
        for (int i = 0; i < propertyCount; i++)
        {
            obj.Add($"prop{i}", _faker.GenerateSimpleObject(1));
        }

        return obj;
    }

    public JsonNode CreateLongArray(int elementCount)
    {
        var array = new JsonArray();
        for (int i = 0; i < elementCount; i++)
        {
            array.Add(_faker.GenerateSimpleObject(2));
        }

        return array;
    }
}

/// <summary>
/// Captures and reports performance metrics for JSON operations
/// </summary>
public class PerformanceReport
{
    private readonly Dictionary<string, List<OperationMetric>> _metrics = new();

    public void AddMetric(string operation, long duration, long memoryUsed, int dataSize)
    {
        if (!_metrics.ContainsKey(operation))
        {
            _metrics[operation] = new List<OperationMetric>();
        }

        _metrics[operation].Add(new OperationMetric
        {
            Duration = duration,
            MemoryUsed = memoryUsed,
            DataSize = dataSize
        });
    }

    public string GenerateReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# JSON Performance Report");
        sb.AppendLine();

        foreach (var operation in _metrics.Keys)
        {
            sb.AppendLine($"## {operation}");
            sb.AppendLine();
            sb.AppendLine("| Data Size | Avg Duration (ms) | Avg Memory (KB) |");
            sb.AppendLine("|-----------|-----------------|---------------|");

            var groupedMetrics = _metrics[operation]
                .GroupBy(m => m.DataSize)
                .OrderBy(g => g.Key);

            foreach (var group in groupedMetrics)
            {
                double avgDuration = group.Average(m => m.Duration);
                double avgMemory = group.Average(m => m.MemoryUsed) / 1024.0;

                sb.AppendLine($"| {group.Key} | {avgDuration:F2} | {avgMemory:F2} |");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private class OperationMetric
    {
        public long Duration { get; set; }
        public long MemoryUsed { get; set; }
        public int DataSize { get; set; }
    }
}