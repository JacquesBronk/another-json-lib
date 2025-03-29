namespace AnotherJsonLib.Domain;

public class DiffEntry
{
    public object OldValue { get; set; }
    public object NewValue { get; set; }
    public JsonDiffResult NestedDiff { get; set; }
}