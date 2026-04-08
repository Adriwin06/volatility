using Volatility.Resources;

namespace Volatility.Utilities;

internal static class TextureBitmapUtilities
{
    public static string GetResourceBaseName(string headerPath, Unpacker unpacker)
    {
        return GetHeaderBaseName(Path.GetFileName(headerPath), unpacker);
    }

    public static string GetSecondaryBitmapPath(string headerPath, Unpacker unpacker)
    {
        string? directory = Path.GetDirectoryName(headerPath);
        string baseName = GetResourceBaseName(headerPath, unpacker);
        string secondarySuffix = GetSecondaryResourceSuffix(unpacker);

        return string.IsNullOrEmpty(directory)
            ? baseName + secondarySuffix
            : Path.Combine(directory, baseName + secondarySuffix);
    }

    public static byte[] ReadNormalizedBitmapData(TextureBase texture, string bitmapPath)
    {
        return NormalizeBitmapData(texture, File.ReadAllBytes(bitmapPath));
    }

    public static void WriteNormalizedBitmapFile(TextureBase texture, string sourceBitmapPath, string outputPath, bool overwrite = true)
    {
        if (!overwrite && File.Exists(outputPath))
        {
            throw new IOException($"The file '{outputPath}' already exists.");
        }

        File.WriteAllBytes(outputPath, ReadNormalizedBitmapData(texture, sourceBitmapPath));
    }

    public static byte[] NormalizeBitmapData(TextureBase texture, byte[] bitmapData)
    {
        return texture switch
        {
            TextureX360 x360 when x360.Format.Tiled => X360TextureUtilities.GetUntiled360TextureData(x360, bitmapData),
            TexturePS3 ps3 when ps3.Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8
                => PS3TextureUtilities.DecodePS3A8R8G8B8(bitmapData, ps3.Width, ps3.Height, ps3.MipmapLevels),
            _ => bitmapData,
        };
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
            Unpacker.Volatility => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };
    }

    private static string GetSecondaryResourceSuffix(Unpacker unpacker)
    {
        return unpacker switch
        {
            Unpacker.Bnd2Manager => "_2.bin",
            Unpacker.DGI => "_texture.dat",
            Unpacker.YAP => "_secondary.dat",
            Unpacker.Raw => "_texture.dat",
            Unpacker.Volatility => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };
    }
}
