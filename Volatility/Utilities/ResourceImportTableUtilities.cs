using Volatility.Resources;

namespace Volatility.Utilities;

internal static class ResourceImportTableUtilities
{
    public static long GetBodyLengthWithoutTrailingImports(
        EndianAwareBinaryReader reader,
        int importCount,
        Func<int, long>? expectedKeyAtIndex = null)
    {
        if (importCount <= 0)
        {
            return reader.BaseStream.Length;
        }

        long importBlockOffset = reader.BaseStream.Length - ((long)importCount * ResourceImport.ImportEntrySize);
        if (importBlockOffset < 0)
        {
            return reader.BaseStream.Length;
        }

        long originalPosition = reader.BaseStream.Position;
        try
        {
            reader.BaseStream.Seek(importBlockOffset, SeekOrigin.Begin);
            for (int i = 0; i < importCount; i++)
            {
                ulong referenceId = reader.ReadUInt64();
                uint key = reader.ReadUInt32();
                uint padding = reader.ReadUInt32();

                if (referenceId == 0 || padding != 0 || key >= importBlockOffset)
                {
                    return reader.BaseStream.Length;
                }

                if (expectedKeyAtIndex != null && key != expectedKeyAtIndex(i))
                {
                    return reader.BaseStream.Length;
                }
            }

            return importBlockOffset;
        }
        finally
        {
            reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
        }
    }

    public static List<KeyValuePair<long, ResourceImport>> ReadTrailingImports(
        EndianAwareBinaryReader reader,
        int importCount)
    {
        List<KeyValuePair<long, ResourceImport>> imports = [];
        if (importCount <= 0)
        {
            return imports;
        }

        long importBlockOffset = reader.BaseStream.Length - ((long)importCount * ResourceImport.ImportEntrySize);
        if (importBlockOffset < 0)
        {
            return imports;
        }

        long bodyLength = GetBodyLengthWithoutTrailingImports(reader, importCount);
        if (bodyLength != importBlockOffset)
        {
            return imports;
        }

        long originalPosition = reader.BaseStream.Position;
        try
        {
            reader.BaseStream.Seek(importBlockOffset, SeekOrigin.Begin);
            for (int i = 0; i < importCount; i++)
            {
                ulong referenceId = reader.ReadUInt64();
                uint key = reader.ReadUInt32();
                reader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);
                imports.Add(new KeyValuePair<long, ResourceImport>(key, new ResourceImport(referenceId, externalImport: true)));
            }
        }
        finally
        {
            reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
        }

        return imports;
    }
}
