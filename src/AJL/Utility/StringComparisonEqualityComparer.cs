namespace AJL.Utility;

public class StringComparisonEqualityComparer : IEqualityComparer<byte>
{
    private readonly StringComparison comparison;

    public StringComparisonEqualityComparer(StringComparison comparison)
    {
        this.comparison = comparison;
    }

    public bool Equals(string x, string y)
    {
        return string.Equals(x, y, comparison);
    }
    
    public bool Equals(byte x, byte y)
    {
        return x.Equals(y);
    }

    public int GetHashCode(byte obj)
    {
        return obj.GetHashCode();
    }
    
    public int GetHashCode(string obj)
    {
        return obj?.GetHashCode() ?? 0;
    }

    public bool Equals(byte[] x, byte[] y)
    {
        if (x == y)
        {
            return true;
        }

        if (x.Length != y.Length)
        {
            return false;
        }

        return x.SequenceEqual(y, new StringComparisonEqualityComparer(comparison));
    }

    public int GetHashCode(byte[] obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        int hash = 17;
        foreach (byte b in obj)
        {
            hash = (hash * 31) + b.GetHashCode();
        }

        return hash;
    }
}