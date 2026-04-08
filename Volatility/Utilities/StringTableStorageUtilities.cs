using System.Text;

using Newtonsoft.Json;

using Volatility.Operations.StringTables;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Volatility.Utilities;

internal static class StringTableStorageUtilities
{
    public static async Task WriteYamlAsync(string yamlFile, Dictionary<string, Dictionary<string, StringTableResourceEntry>> entries)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        string yaml = serializer.Serialize(entries);
        await File.WriteAllTextAsync(yamlFile, yaml, Encoding.UTF8);
    }

    public static async Task<Dictionary<string, string>> LoadJsonAsync(string jsonFile)
    {
        if (!File.Exists(jsonFile))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        string content = await File.ReadAllTextAsync(jsonFile);
        Dictionary<string, string>? result = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);

        return result != null
            ? new Dictionary<string, string>(result, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public static async Task WriteJsonAsync(string jsonFile, Dictionary<string, string> entries)
    {
        string json = JsonConvert.SerializeObject(entries, Formatting.Indented);
        await File.WriteAllTextAsync(jsonFile, json, Encoding.UTF8);
    }

    public static void MergeLegacyEntries(Dictionary<string, string> target, Dictionary<string, Dictionary<string, StringTableResourceEntry>> source, bool overwrite)
    {
        foreach ((string _, Dictionary<string, StringTableResourceEntry> resourceEntries) in source)
        {
            foreach ((string resourceKey, StringTableResourceEntry entry) in resourceEntries)
            {
                string normalizedKey = NormalizeResourceID(resourceKey);

                if (!target.ContainsKey(normalizedKey) || overwrite)
                {
                    target[normalizedKey] = entry.Name;
                }
            }
        }
    }

    private static string NormalizeResourceID(string resourceID)
    {
        return resourceID.Replace("_", "").ToLowerInvariant();
    }
}
