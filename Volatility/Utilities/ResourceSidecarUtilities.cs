using Volatility.Resources;

namespace Volatility.Utilities;

internal static class ResourceSidecarUtilities
{
    public static string GetSidecarPath(string resourcePath, string suffix)
    {
        string? directory = Path.GetDirectoryName(resourcePath);
        string fileName = Path.GetFileNameWithoutExtension(resourcePath) + suffix;

        return string.IsNullOrEmpty(directory)
            ? fileName
            : Path.Combine(directory, fileName);
    }

    public static string GetRelativeSidecarName(string resourcePath, string suffix)
    {
        return Path.GetFileName(GetSidecarPath(resourcePath, suffix));
    }

    public static string ResolveSidecarPath(Resource resource, string sidecarPath)
    {
        if (Path.IsPathRooted(sidecarPath))
        {
            return sidecarPath;
        }

        string? baseDirectory = !string.IsNullOrWhiteSpace(resource.ImportedFileName)
            ? Path.GetDirectoryName(resource.ImportedFileName)
            : null;

        return string.IsNullOrWhiteSpace(baseDirectory)
            ? Path.GetFullPath(sidecarPath)
            : Path.Combine(baseDirectory, sidecarPath);
    }

    public static string GetSecondaryResourcePath(string headerPath, Unpacker unpacker, string rawSecondaryNameSuffix)
    {
        string? directory = Path.GetDirectoryName(headerPath);
        string baseName = GetHeaderBaseName(Path.GetFileName(headerPath), unpacker);
        string secondarySuffix = GetSecondaryResourceSuffix(unpacker, rawSecondaryNameSuffix);

        return string.IsNullOrEmpty(directory)
            ? baseName + secondarySuffix
            : Path.Combine(directory, baseName + secondarySuffix);
    }

    public static void WriteSidecarBytes(string resourcePath, string suffix, byte[] data, bool overwrite)
    {
        string path = GetSidecarPath(resourcePath, suffix);
        if (!overwrite && File.Exists(path))
        {
            throw new IOException($"The file '{path}' already exists.");
        }

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(path, data);
    }

    public static byte[] ReadSidecarBytes(Resource resource, string? sidecarPath)
    {
        if (string.IsNullOrWhiteSpace(sidecarPath))
        {
            return [];
        }

        string path = ResolveSidecarPath(resource, sidecarPath);
        return File.Exists(path) ? File.ReadAllBytes(path) : [];
    }

    private static string GetHeaderBaseName(string headerFileName, Unpacker unpacker)
    {
        string primarySuffix = GetPrimaryResourceSuffix(unpacker);
        return headerFileName.EndsWith(primarySuffix, StringComparison.OrdinalIgnoreCase)
            ? headerFileName[..^primarySuffix.Length]
            : Path.GetFileNameWithoutExtension(headerFileName);
    }

    private static string GetPrimaryResourceSuffix(Unpacker unpacker)
    {
        return unpacker switch
        {
            Unpacker.Bnd2Manager => "_1.bin",
            Unpacker.DGI => ".dat",
            Unpacker.YAP => "_primary.dat",
            Unpacker.Raw => ".dat",
            Unpacker.Volatility => ".dat",
            _ => ".dat",
        };
    }

    private static string GetSecondaryResourceSuffix(Unpacker unpacker, string rawSecondaryNameSuffix)
    {
        return unpacker switch
        {
            Unpacker.Bnd2Manager => "_2.bin",
            Unpacker.YAP => "_secondary.dat",
            Unpacker.Raw or Unpacker.DGI or Unpacker.Volatility => rawSecondaryNameSuffix + ".dat",
            _ => rawSecondaryNameSuffix + ".dat",
        };
    }
}
