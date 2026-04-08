using Volatility.Operations.StringTables;
using Volatility.Utilities;

using static Volatility.Utilities.EnvironmentUtilities;

namespace Volatility.CLI.Commands;

internal class ImportStringTableCommand : ICommand
{
    public static string CommandToken => "ImportStringTable";
    public static string CommandDescription => "Imports entries into the ResourceDB from files containing a ResourceStringTable.";
    public static string CommandParameters => "[--verbose] [--overwrite] [--recurse] [--endian=<le,be>] [--version=<v1,v2>] --path=<file path>";

    public string? Endian { get; set; }
    public string? ImportPath { get; set; }
    public string? Version { get; set; }
    public bool Overwrite { get; set; }
    public bool Recursive { get; set; }
    public bool Verbose { get; set; }

    public async Task Execute()
    {
        if (string.IsNullOrEmpty(ImportPath))
        {
            Console.WriteLine("Error: No import path specified! (--path)");
            return;
        }

        var filePaths = ICommand.GetFilePathsInDirectory(ImportPath, ICommand.TargetFileType.Any, Recursive);
        if (filePaths.Length == 0)
        {
            Console.WriteLine("Error: No files or folders found within the specified path!");
            return;
        }

        Console.WriteLine("Importing data from ResourceStringTables into the ResourceDB... this may take a while!");

        string directoryPath = GetEnvironmentDirectory(EnvironmentDirectory.ResourceDB);
        Directory.CreateDirectory(directoryPath);

        var loadOperation = new LoadResourceDictionaryOperation();
        var mergeOperation = new MergeStringTableEntriesOperation();
        var importOperation = new ImportStringTableOperation(mergeOperation);
        string version = Version ?? "v2";

        if (version == "v1")
        {
            string jsonFile = Path.Combine(directoryPath, "ResourceDB.json");
            var importedEntries = new Dictionary<string, Dictionary<string, StringTableResourceEntry>>(StringComparer.OrdinalIgnoreCase);
            await importOperation.ExecuteAsync(filePaths, importedEntries, Endian ?? "le", Overwrite, Verbose);

            var legacyEntries = await StringTableStorageUtilities.LoadJsonAsync(jsonFile);
            StringTableStorageUtilities.MergeLegacyEntries(legacyEntries, importedEntries, Overwrite);
            await StringTableStorageUtilities.WriteJsonAsync(jsonFile, legacyEntries);

            Console.WriteLine($"Finished importing all ResourceDB (v1) data at {jsonFile}.");
            return;
        }

        if (version != "v2")
        {
            Console.WriteLine("Error: Invalid version specified! (--version must be v1 or v2)");
            return;
        }

        string yamlFile = Path.Combine(directoryPath, "ResourceDB.yaml");
        var allEntries = await loadOperation.ExecuteAsync(yamlFile);
        await importOperation.ExecuteAsync(filePaths, allEntries, Endian ?? "le", Overwrite, Verbose);
        await StringTableStorageUtilities.WriteYamlAsync(yamlFile, allEntries);

        Console.WriteLine($"Finished importing all ResourceDB (v2) data at {yamlFile}.");
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Endian = (args.TryGetValue("endian", out object? format) ? format as string : "le")?.ToLowerInvariant() ?? "le";
        ImportPath = args.TryGetValue("path", out object? path) ? path as string : "";
        Version = (args.TryGetValue("version", out object? version) ? version as string : "v2")?.ToLowerInvariant() ?? "v2";
        Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
        Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
        Verbose = args.TryGetValue("verbose", out var ve) && (bool)ve;
    }

    public ImportStringTableCommand() { }
}
