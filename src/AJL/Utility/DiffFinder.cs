using System.Text.Json;
using AJL.Infra;


namespace AJL.Utility;

    public static partial class JsonTools
    {
       
        
        /// <summary>
        /// Finds the differences between two JSON strings represented as dictionaries.
        /// </summary>
        /// <param name="originalJson">The original JSON string.</param>
        /// <param name="newJson">The new JSON string.</param>
        /// <returns>A dictionary containing the key-value pairs that are different or new in the new JSON.</returns>
        public static Dictionary<string, object> FindDifferences(this string originalJson, string newJson)
        {
            var comparer = new JsonElementComparer();
            var originalDict = originalJson.FromJson<Dictionary<string, JsonElement>>();
            var newDict = newJson.FromJson<Dictionary<string, JsonElement>>();

            var result = new Dictionary<string, object>();

            foreach (var pair in newDict!)
            {
                if (!originalDict!.ContainsKey(pair.Key))
                {
                    // Convert JsonElement to its value type
                    result[pair.Key] = comparer.ConvertToValueType(pair.Value);
                }
                else if (!comparer.Equals(originalDict[pair.Key], pair.Value))
                {
                    // Convert JsonElement to its value type
                    result[pair.Key] = comparer.ConvertToValueType(pair.Value);  // Changed key-value pair
                }
            }

            return result;
        }
        
        
    }
