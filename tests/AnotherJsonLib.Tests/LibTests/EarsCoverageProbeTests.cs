using AnotherJsonLib.Utility;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class EarsCoverageProbeTests
{
    [Fact(DisplayName = "[E-01] library assembly loads and a JsonObject round-trips")]
    public void LibraryAssemblyLoadsAndJsonObjectRoundTrips()
    {
        var original = new { Name = "Alice", Score = 42 };
        var json = original.ToJson();
        var restored = json.FromJson<Dictionary<string, object>>();

        restored.ShouldNotBeNull();
        restored["Name"].ToString().ShouldBe("Alice");
        restored["Score"].ToString().ShouldBe("42");
    }
}
