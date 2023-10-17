using System.Text.Json;

namespace AnotherJsonLib.Utility;

public static partial class JsonTools
{
    private static readonly JsonSerializerOptions DefaultPrettifyOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };
    
    

    public static string Prettify<T>(this T data)
    {
        return JsonSerializer.Serialize(data, DefaultPrettifyOptions);
    }
}