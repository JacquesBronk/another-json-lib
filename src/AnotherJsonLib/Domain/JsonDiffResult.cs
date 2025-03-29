namespace AnotherJsonLib.Domain;

public class JsonDiffResult
{
    public Dictionary<string, object> Added { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Removed { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, DiffEntry> Modified { get; set; } = new Dictionary<string, DiffEntry>();
}