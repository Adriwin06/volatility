using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.Operations.Resources;

internal class TextureToDDSOperation
{
    public async Task ExecuteAsync(IEnumerable<string> sourceFiles, Platform platform, bool isX64, string? outputPath, bool overwrite, bool verbose)
    {
        string[] files = sourceFiles.ToArray();
        bool multipleInputs = files.Length > 1;

        List<Task> tasks = new();
        foreach (string sourceFile in files)
        {
            tasks.Add(Task.Run(async () =>
            {
                TextureBase texture = (TextureBase)ResourceFactory.CreateResource(ResourceType.Texture, platform, sourceFile, isX64);
                string sourceBitmapPath = TextureBitmapUtilities.GetSecondaryBitmapPath(sourceFile, texture.Unpacker);

                if (!File.Exists(sourceBitmapPath))
                {
                    throw new FileNotFoundException($"Failed to find associated bitmap data at path '{sourceBitmapPath}'.");
                }

                byte[] bitmapData = TextureBitmapUtilities.ReadNormalizedBitmapData(texture, sourceBitmapPath);
                byte[] ddsData = DDSTextureUtilities.CreateDDSFile(texture, bitmapData);
                string destinationPath = ResolveOutputPath(sourceFile, texture.Unpacker, outputPath, multipleInputs);

                string? destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                if (!overwrite && File.Exists(destinationPath))
                {
                    throw new IOException($"The file '{destinationPath}' already exists.");
                }

                if (verbose) Console.WriteLine($"Writing DDS texture data to {destinationPath}...");
                await File.WriteAllBytesAsync(destinationPath, ddsData);
                Console.WriteLine($"Wrote DDS for {Path.GetFileName(sourceFile)} to {destinationPath}.");
            }));
        }

        await Task.WhenAll(tasks);
    }

    private static string ResolveOutputPath(string sourceFile, Unpacker unpacker, string? outputPath, bool multipleInputs)
    {
        string outputName = TextureBitmapUtilities.GetResourceBaseName(sourceFile, unpacker) + ".dds";

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Path.Combine(Path.GetDirectoryName(sourceFile) ?? string.Empty, outputName);
        }

        bool outputLooksLikeFile = Path.HasExtension(outputPath)
            && string.Equals(Path.GetExtension(outputPath), ".dds", StringComparison.OrdinalIgnoreCase);

        if (!multipleInputs && outputLooksLikeFile)
        {
            return outputPath;
        }

        return Path.Combine(outputPath, outputName);
    }
}
