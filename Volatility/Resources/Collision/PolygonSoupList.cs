using System.Numerics;

using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.PolygonSoupList)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class PolygonSoupList : Resource
{
    private const int HeaderSize = 0x30;
    private const int SoupHeaderSize = 0x20;
    private const int SoupBoxGroupSize = 0x70;

    public Vector4 Min;
    public Vector4 Max;
    public int DataSize = HeaderSize;
    public List<PolygonSoup> PolygonSoups = [];

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        PolygonSoups.Clear();

        Min = reader.ReadVector4();
        Max = reader.ReadVector4();
        uint soupPointerTableOffset = reader.ReadUInt32();
        uint soupBoxesOffset = reader.ReadUInt32();
        int soupCount = reader.ReadInt32();
        DataSize = reader.ReadInt32();

        if (soupCount <= 0)
        {
            return;
        }

        List<uint> soupOffsets = reader.ParseSection(soupPointerTableOffset, soupCount, r => r.ReadUInt32());
        for (int i = 0; i < soupCount; i++)
        {
            PolygonSoup soup = new()
            {
                Box = ReadSoupBox(reader, soupBoxesOffset, i),
            };

            reader.ParseSection(soupOffsets[i], r => ReadPolygonSoupHeader(r, soup), out _);
            reader.ParseSection(soup.VerticesOffset, soup.VertexCount, ReadPolygonSoupVertex, soup.Vertices);
            reader.ParseSection(soup.PolygonsOffset, soup.TotalPolygonCount, r => ReadPolygonSoupPolygon(r, soup.QuadCount), soup.Polygons);
            PolygonSoups.Add(soup);
        }
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        if (PolygonSoups.Count == 0)
        {
            writer.Write(Min);
            writer.Write(Max);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0);
            writer.Write(HeaderSize);
            return;
        }

        int soupCount = PolygonSoups.Count;
        uint soupPointerTableOffset = HeaderSize;
        uint soupBoxesOffset = (uint)ResourceUtilities.AlignOffset(soupPointerTableOffset + (soupCount * sizeof(uint)), 0x10);
        uint soupBoxesEnd = soupBoxesOffset + (uint)(((soupCount + 3) / 4) * SoupBoxGroupSize);
        uint firstSoupOffset = CalculateFirstSoupOffset(soupCount, soupBoxesEnd);

        uint[] soupOffsets = new uint[soupCount];
        uint currentSoupOffset = firstSoupOffset;
        for (int i = 0; i < soupCount; i++)
        {
            PolygonSoup soup = PolygonSoups[i];
            soup.VertexCount = (byte)Math.Min(byte.MaxValue, soup.Vertices.Count);
            soup.TotalPolygonCount = (byte)Math.Min(byte.MaxValue, soup.Polygons.Count);
            soup.QuadCount = (byte)Math.Min(byte.MaxValue, soup.Polygons.Count(p => p.IsQuad));
            soup.VerticesOffset = currentSoupOffset + SoupHeaderSize;
            soup.PolygonsOffset = (uint)ResourceUtilities.AlignOffset(soup.VerticesOffset + (soup.VertexCount * 0x6), 0x10);
            soup.DataSize = (ushort)((soup.PolygonsOffset + (soup.TotalPolygonCount * 0xC)) - currentSoupOffset);

            soupOffsets[i] = currentSoupOffset;
            currentSoupOffset = (uint)ResourceUtilities.AlignOffset(currentSoupOffset + soup.DataSize, 0x80);
        }

        uint soupDataEnd = soupOffsets[^1] + PolygonSoups[^1].DataSize;
        DataSize = (int)(soupDataEnd + 0x60);

        writer.Write(Min);
        writer.Write(Max);
        writer.Write(soupPointerTableOffset);
        writer.Write(soupBoxesOffset);
        writer.Write(soupCount);
        writer.Write(DataSize);

        writer.WriteSection(soupPointerTableOffset, soupOffsets, (w, offset) => w.Write(offset));

        for (int i = 0; i < soupCount; i++)
        {
            WriteSoupBox(writer, soupBoxesOffset, i, PolygonSoups[i].Box);
        }

        for (int i = 0; i < soupCount; i++)
        {
            PolygonSoup soup = PolygonSoups[i];
            writer.BaseStream.Position = soupOffsets[i];
            WritePolygonSoupHeader(writer, soup);

            writer.WriteSection(soup.VerticesOffset, soup.Vertices, WritePolygonSoupVertex);
            writer.BaseStream.Position = soup.PolygonsOffset;
            for (int j = 0; j < soup.Polygons.Count; j++)
            {
                WritePolygonSoupPolygon(writer, soup.Polygons[j]);
            }
        }

        writer.BaseStream.Position = soupDataEnd;
        writer.WriteFixedBytes(null, 0x60);
    }

    private static uint CalculateFirstSoupOffset(int soupCount, uint soupBoxesEnd)
    {
        long offset = ResourceUtilities.AlignOffset(soupBoxesEnd, 0x80);
        for (int i = 0; i < (soupCount + 3) / 4; i++)
        {
            offset += ((i % 8) % 3) == 0 ? 0x100 : 0x180;
        }

        return (uint)offset;
    }

    private static PolygonSoupBox ReadSoupBox(ResourceBinaryReader reader, uint soupBoxesOffset, int index)
    {
        long baseOffset = soupBoxesOffset + (SoupBoxGroupSize * (index / 4)) + (sizeof(float) * (index % 4));
        long originalPosition = reader.BaseStream.Position;

        reader.BaseStream.Seek(baseOffset, SeekOrigin.Begin);
        float minX = reader.ReadSingle();
        reader.BaseStream.Seek(baseOffset + 0x10, SeekOrigin.Begin);
        float minY = reader.ReadSingle();
        reader.BaseStream.Seek(baseOffset + 0x20, SeekOrigin.Begin);
        float minZ = reader.ReadSingle();
        reader.BaseStream.Seek(baseOffset + 0x30, SeekOrigin.Begin);
        float maxX = reader.ReadSingle();
        reader.BaseStream.Seek(baseOffset + 0x40, SeekOrigin.Begin);
        float maxY = reader.ReadSingle();
        reader.BaseStream.Seek(baseOffset + 0x50, SeekOrigin.Begin);
        float maxZ = reader.ReadSingle();
        reader.BaseStream.Seek(baseOffset + 0x60, SeekOrigin.Begin);
        int validMasks = reader.ReadInt32();

        reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
        return new PolygonSoupBox
        {
            Min = new Vector3(minX, minY, minZ),
            Max = new Vector3(maxX, maxY, maxZ),
            ValidMasks = validMasks,
        };
    }

    private static void WriteSoupBox(ResourceBinaryWriter writer, uint soupBoxesOffset, int index, PolygonSoupBox box)
    {
        long baseOffset = soupBoxesOffset + (SoupBoxGroupSize * (index / 4)) + (sizeof(float) * (index % 4));
        writer.BaseStream.Position = baseOffset;
        writer.Write(box.Min.X);
        writer.BaseStream.Position = baseOffset + 0x10;
        writer.Write(box.Min.Y);
        writer.BaseStream.Position = baseOffset + 0x20;
        writer.Write(box.Min.Z);
        writer.BaseStream.Position = baseOffset + 0x30;
        writer.Write(box.Max.X);
        writer.BaseStream.Position = baseOffset + 0x40;
        writer.Write(box.Max.Y);
        writer.BaseStream.Position = baseOffset + 0x50;
        writer.Write(box.Max.Z);
        writer.BaseStream.Position = baseOffset + 0x60;
        writer.Write(box.ValidMasks);
    }

    private static int ReadPolygonSoupHeader(ResourceBinaryReader reader, PolygonSoup soup)
    {
        soup.VertexOffsets = [reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()];
        soup.CompressionGranularity = reader.ReadSingle();
        soup.PolygonsOffset = reader.ReadUInt32();
        soup.VerticesOffset = reader.ReadUInt32();
        soup.DataSize = reader.ReadUInt16();
        soup.TotalPolygonCount = reader.ReadByte();
        soup.QuadCount = reader.ReadByte();
        soup.VertexCount = reader.ReadByte();
        reader.BaseStream.Seek(0x3, SeekOrigin.Current);
        return 0;
    }

    private static void WritePolygonSoupHeader(ResourceBinaryWriter writer, PolygonSoup soup)
    {
        for (int i = 0; i < 3; i++)
        {
            writer.Write(i < soup.VertexOffsets.Length ? soup.VertexOffsets[i] : 0);
        }

        writer.Write(soup.CompressionGranularity);
        writer.Write(soup.PolygonsOffset);
        writer.Write(soup.VerticesOffset);
        writer.Write(soup.DataSize);
        writer.Write(soup.TotalPolygonCount);
        writer.Write(soup.QuadCount);
        writer.Write(soup.VertexCount);
        writer.WriteFixedBytes(null, 0x3);
    }

    private static PolygonSoupVertex ReadPolygonSoupVertex(ResourceBinaryReader reader)
    {
        return new PolygonSoupVertex
        {
            X = reader.ReadUInt16(),
            Y = reader.ReadUInt16(),
            Z = reader.ReadUInt16(),
        };
    }

    private static void WritePolygonSoupVertex(ResourceBinaryWriter writer, PolygonSoupVertex vertex)
    {
        writer.Write(vertex.X);
        writer.Write(vertex.Y);
        writer.Write(vertex.Z);
    }

    private static PolygonSoupPolygon ReadPolygonSoupPolygon(ResourceBinaryReader reader, byte quadCount)
    {
        ushort tag0 = reader.ReadUInt16();
        ushort tag1 = reader.ReadUInt16();
        byte a = reader.ReadByte();
        byte b = reader.ReadByte();
        byte c = reader.ReadByte();
        byte d = reader.ReadByte();
        byte[] edgeCosines = reader.ReadBytes(4);

        return new PolygonSoupPolygon
        {
            CollisionTag0 = tag0,
            CollisionTag1 = tag1,
            VertexIndices = d == 0xFF ? [a, b, c] : [a, b, c, d],
            EdgeCosines = edgeCosines,
        };
    }

    private static void WritePolygonSoupPolygon(ResourceBinaryWriter writer, PolygonSoupPolygon polygon)
    {
        writer.Write(polygon.CollisionTag0);
        writer.Write(polygon.CollisionTag1);

        byte[] indices = polygon.VertexIndices ?? [];
        writer.Write(indices.Length > 0 ? indices[0] : (byte)0);
        writer.Write(indices.Length > 1 ? indices[1] : (byte)0);
        writer.Write(indices.Length > 2 ? indices[2] : (byte)0);
        writer.Write(polygon.IsQuad && indices.Length > 3 ? indices[3] : (byte)0xFF);
        writer.WriteFixedBytes(polygon.EdgeCosines, 4);
    }

    public PolygonSoupList() : base() { }
    public PolygonSoupList(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public class PolygonSoup
{
    public PolygonSoupBox Box;
    public int[] VertexOffsets = [0, 0, 0];
    public float CompressionGranularity;
    public uint PolygonsOffset;
    public uint VerticesOffset;
    public ushort DataSize;
    public byte TotalPolygonCount;
    public byte QuadCount;
    public byte VertexCount;
    public List<PolygonSoupVertex> Vertices = [];
    public List<PolygonSoupPolygon> Polygons = [];
}

public struct PolygonSoupBox
{
    public Vector3 Min;
    public Vector3 Max;
    public int ValidMasks;
}

public struct PolygonSoupVertex
{
    public ushort X;
    public ushort Y;
    public ushort Z;
}

public class PolygonSoupPolygon
{
    public ushort CollisionTag0;
    public ushort CollisionTag1;
    public byte[] VertexIndices = [];
    public byte[] EdgeCosines = [];
    public bool IsQuad => VertexIndices.Length >= 4;
}
