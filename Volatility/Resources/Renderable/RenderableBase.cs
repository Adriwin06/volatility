using System.Collections;

using Volatility.Utilities;

namespace Volatility.Resources;

// The Renderable resource type contains all the 3D data used by each
// Model resource in Burnout Paradise. Essentially, Renderables hold the
// geometric and visual information needed for rendering models in-game.

// Learn More:
// https://burnout.wiki/wiki/Renderable

[ResourceDefinition(ResourceType.Renderable)]
public abstract class RenderableBase : Resource, ISidecarPayloadResource
{
    public Vector3Plus BoundingSphere;
    public ushort Version;
    public ushort NumMeshes;
    public uint Meshes;
    public uint ObjectScopeTextureInfo;
    public BitArray Flags = new(16);
    public uint IndexBuffer;
    public uint VertexBuffer;
    public List<RenderableMesh> MeshesData = [];
    public RenderableBufferInfo BufferInfo = new();
    public string? BodyPath { get; set; }

    public string PayloadSuffix => ".RenderableBody";

    public byte[] GetPayloadBytes()
    {
        byte[] sidecarData = ResourceSidecarUtilities.ReadSidecarBytes(this, BodyPath);
        if (sidecarData.Length > 0)
        {
            return sidecarData;
        }

        if (!string.IsNullOrWhiteSpace(ImportedFileName))
        {
            string importedBodyPath = ResourceSidecarUtilities.GetSecondaryResourcePath(ImportedFileName, Unpacker, "_model");
            if (File.Exists(importedBodyPath))
            {
                return File.ReadAllBytes(importedBodyPath);
            }
        }

        return [];
    }

    public void SetPayloadPath(string path)
    {
        BodyPath = path;
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        MeshesData.Clear();

        BoundingSphere = reader.ReadVector4();
        Version = reader.ReadUInt16();
        NumMeshes = reader.ReadUInt16();
        Meshes = reader.ReadUInt32();
        ObjectScopeTextureInfo = reader.ReadUInt32();
        using BitReader bitReader = new(reader.ReadBytes(4));
        Flags = bitReader.ReadBitsToBitArray(16);

        if (reader.Endianness == Endian.LE && reader.BaseStream.Position + 0x8 <= reader.BaseStream.Length)
        {
            IndexBuffer = reader.ReadUInt32();
            VertexBuffer = reader.ReadUInt32();
            reader.BaseStream.Seek(0x8, SeekOrigin.Current);
        }

        List<uint> meshPointers = reader.ParseSection(Meshes, NumMeshes, r => r.ReadUInt32());
        ParseBufferInfo(reader, meshPointers);
        ParseMeshes(reader, meshPointers);
        ParseMeshImports(reader);
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        NumMeshes = (ushort)MeshesData.Count;
        uint meshTableOffset = 0x30;
        uint bufferInfoOffset = (uint)(meshTableOffset + (NumMeshes * sizeof(uint)));
        uint meshDataOffset = (uint)ResourceUtilities.AlignOffset(bufferInfoOffset + RenderableBufferInfo.Size, 0x10);
        uint meshHeaderSize = GetMeshHeaderSizeForWrite();

        uint[] meshPointers = new uint[NumMeshes];
        for (int i = 0; i < NumMeshes; i++)
        {
            meshPointers[i] = meshDataOffset + ((uint)i * meshHeaderSize);
        }

        writer.Write(BoundingSphere);
        writer.Write(Version == 0 ? (ushort)0xB : Version);
        writer.Write(NumMeshes);
        writer.Write(meshTableOffset);
        writer.Write(ObjectScopeTextureInfo);
        writer.Write(BitArrayToUInt32(Flags));
        writer.Write(IndexBuffer == 0 ? bufferInfoOffset : IndexBuffer);
        writer.Write(VertexBuffer == 0 ? bufferInfoOffset + 0x18u : VertexBuffer);
        writer.WriteFixedBytes(null, 0x30 - (int)writer.BaseStream.Position);

        writer.WriteSection(meshTableOffset, meshPointers, (w, pointer) => w.Write(pointer));
        writer.WriteSection(bufferInfoOffset, BufferInfo, WriteBufferInfo);

        for (int i = 0; i < MeshesData.Count; i++)
        {
            writer.WriteSection(meshPointers[i], MeshesData[i], (w, mesh) => WriteRenderableMesh(w, mesh, meshHeaderSize));
        }
    }

    public override IEnumerable<KeyValuePair<long, ResourceImport>> GetExternalImports()
    {
        uint meshHeaderSize = GetMeshHeaderSizeForWrite();
        uint meshDataOffset = (uint)ResourceUtilities.AlignOffset(
            0x30 + (MeshesData.Count * sizeof(uint)) + RenderableBufferInfo.Size,
            0x10);

        for (int i = 0; i < MeshesData.Count; i++)
        {
            long meshOffset = meshDataOffset + (i * meshHeaderSize);
            RenderableMesh mesh = MeshesData[i];

            if (mesh.MaterialReference.ExternalImport)
            {
                yield return new KeyValuePair<long, ResourceImport>(meshOffset + 0x50, mesh.MaterialReference);
            }

            for (int j = 0; j < mesh.VertexDescriptorReferences.Count; j++)
            {
                ResourceImport reference = mesh.VertexDescriptorReferences[j];
                if (reference.ExternalImport)
                {
                    yield return new KeyValuePair<long, ResourceImport>(meshOffset + 0x60 + (j * sizeof(uint)), reference);
                }
            }
        }
    }

    public void WriteModelBodySidecar(string primaryOutputPath)
    {
        byte[] data = GetPayloadBytes();
        if (data.Length == 0)
        {
            return;
        }

        string outputPath = ResourceSidecarUtilities.GetSecondaryResourcePath(primaryOutputPath, Unpacker, "_model");
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(outputPath, data);
    }

    private void ParseBufferInfo(ResourceBinaryReader reader, List<uint> meshPointers)
    {
        if (Meshes == 0 || meshPointers.Count == 0 || reader.Endianness == Endian.BE)
        {
            return;
        }

        long bufferInfoOffset = Meshes + ((long)meshPointers.Count * sizeof(uint));
        if (bufferInfoOffset + RenderableBufferInfo.Size > reader.BaseStream.Length)
        {
            return;
        }

        reader.ParseSection(bufferInfoOffset, ReadBufferInfo, out BufferInfo);
    }

    private void ParseMeshes(ResourceBinaryReader reader, List<uint> meshPointers)
    {
        for (int i = 0; i < meshPointers.Count; i++)
        {
            uint meshOffset = meshPointers[i];
            uint nextMeshOffset = i + 1 < meshPointers.Count ? meshPointers[i + 1] : 0;
            uint meshHeaderSize = nextMeshOffset > meshOffset ? nextMeshOffset - meshOffset : 0x80;
            if (meshOffset == 0 || meshOffset + 0x20 > reader.BaseStream.Length)
            {
                continue;
            }

            reader.ParseSection(meshOffset, r => ReadRenderableMesh(r, meshHeaderSize, reader.Endianness), out RenderableMesh mesh);
            mesh.MeshOffset = meshOffset;
            mesh.MeshHeaderSize = meshHeaderSize;
            MeshesData.Add(mesh);
        }
    }

    private void ParseMeshImports(ResourceBinaryReader reader)
    {
        int importCount = MeshesData.Sum(mesh => 1 + Math.Max(0, mesh.VertexDescriptorCount));
        long bodyLength = ResourceImportTableUtilities.GetBodyLengthWithoutTrailingImports(reader, importCount);
        List<KeyValuePair<long, ResourceImport>> imports = ResourceImportTableUtilities.ReadTrailingImports(reader, importCount);

        int importIndex = 0;
        foreach (RenderableMesh mesh in MeshesData)
        {
            if (importIndex < imports.Count)
            {
                mesh.MaterialReference = imports[importIndex++].Value;
            }
            else
            {
                ResourceImport.ReadExternalImport(mesh.MeshOffset + 0x50, reader, bodyLength, out mesh.MaterialReference);
            }

            mesh.VertexDescriptorReferences.Clear();
            for (int i = 0; i < mesh.VertexDescriptorCount; i++)
            {
                if (importIndex < imports.Count)
                {
                    mesh.VertexDescriptorReferences.Add(imports[importIndex++].Value);
                }
                else if (ResourceImport.ReadExternalImport(mesh.MeshOffset + 0x60 + (i * sizeof(uint)), reader, bodyLength, out ResourceImport reference))
                {
                    mesh.VertexDescriptorReferences.Add(reference);
                }
            }
        }
    }

    private static RenderableBufferInfo ReadBufferInfo(ResourceBinaryReader reader)
    {
        return new RenderableBufferInfo
        {
            Null0 = reader.ReadInt32(),
            Unknown0 = reader.ReadInt32(),
            Unknown1 = reader.ReadInt32(),
            IndicesDataOffset = reader.ReadInt32(),
            IndicesDataSize = reader.ReadInt32(),
            Unknown2 = reader.ReadInt32(),
            Null1 = reader.ReadInt32(),
            Unknown3 = reader.ReadInt32(),
            Unknown4 = reader.ReadInt32(),
            VertexDataOffset = reader.ReadInt32(),
            VertexDataSize = reader.ReadInt32(),
        };
    }

    private static void WriteBufferInfo(ResourceBinaryWriter writer, RenderableBufferInfo value)
    {
        writer.Write(value.Null0);
        writer.Write(value.Unknown0);
        writer.Write(value.Unknown1);
        writer.Write(value.IndicesDataOffset);
        writer.Write(value.IndicesDataSize);
        writer.Write(value.Unknown2);
        writer.Write(value.Null1);
        writer.Write(value.Unknown3);
        writer.Write(value.Unknown4);
        writer.Write(value.VertexDataOffset);
        writer.Write(value.VertexDataSize);
    }

    private static RenderableMesh ReadRenderableMesh(ResourceBinaryReader reader, uint meshHeaderSize, Endian endian)
    {
        RenderableMesh mesh = new();

        if (endian == Endian.LE && meshHeaderSize >= 0x70)
        {
            mesh.Transform = MatrixUtilities.ReadMatrix44(reader);
        }
        else
        {
            reader.BaseStream.Seek(0x10, SeekOrigin.Current);
        }

        mesh.PrimitiveType = reader.ReadInt32();
        mesh.Unknown0 = reader.ReadInt32();
        mesh.IndicesBufferStart = reader.ReadInt32();
        mesh.IndicesBufferSize = reader.ReadInt32();

        if (endian == Endian.LE && meshHeaderSize >= 0x80)
        {
            mesh.MinimumIndex = reader.ReadInt32();
            mesh.NumberOfPrimitives = reader.ReadInt32();
        }

        mesh.Unknown1 = reader.ReadInt32();
        mesh.VertexDescriptorCount = reader.ReadByte();
        mesh.Unknown2 = reader.ReadByte();
        mesh.Unknown3 = reader.ReadByte();
        mesh.SubPartCode = reader.ReadByte();

        if (reader.BaseStream.Position + 0x8 <= reader.BaseStream.Length)
        {
            mesh.IndexBufferPointer = reader.ReadInt32();
            mesh.VertexBufferPointer = reader.ReadInt32();
        }

        return mesh;
    }

    private static void WriteRenderableMesh(ResourceBinaryWriter writer, RenderableMesh mesh, uint meshHeaderSize)
    {
        MatrixUtilities.WriteMatrix44(writer, mesh.Transform);
        writer.Write(mesh.PrimitiveType);
        writer.Write(mesh.Unknown0);
        writer.Write(mesh.IndicesBufferStart);
        writer.Write(mesh.IndicesBufferSize);

        if (meshHeaderSize >= 0x80)
        {
            writer.Write(mesh.MinimumIndex);
            writer.Write(mesh.NumberOfPrimitives);
        }

        writer.Write(mesh.Unknown1);
        writer.Write((byte)mesh.VertexDescriptorReferences.Count);
        writer.Write(mesh.Unknown2);
        writer.Write(mesh.Unknown3);
        writer.Write(mesh.SubPartCode);
        writer.Write(mesh.IndexBufferPointer);
        writer.Write(mesh.VertexBufferPointer);
    }

    private uint GetMeshHeaderSizeForWrite()
    {
        uint parsedHeaderSize = MeshesData.FirstOrDefault(mesh => mesh.MeshHeaderSize != 0)?.MeshHeaderSize ?? 0;
        return parsedHeaderSize >= 0x70 ? parsedHeaderSize : 0x80;
    }

    private static uint BitArrayToUInt32(BitArray bits)
    {
        uint value = 0;
        int count = Math.Min(32, bits.Count);
        for (int i = 0; i < count; i++)
        {
            if (bits[i])
            {
                value |= 1u << i;
            }
        }

        return value;
    }

    protected RenderableBase() : base() { }

    protected RenderableBase(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public class RenderableMesh
{
    public uint MeshOffset;
    public uint MeshHeaderSize = 0x80;
    public Matrix44 Transform;
    public int PrimitiveType = 4;
    public int Unknown0;
    public int IndicesBufferStart;
    public int IndicesBufferSize;
    public int MinimumIndex;
    public int NumberOfPrimitives = 1;
    public int Unknown1;
    public int VertexDescriptorCount;
    public byte Unknown2;
    public byte Unknown3 = 1;
    public byte SubPartCode;
    public int IndexBufferPointer;
    public int VertexBufferPointer;
    public ResourceImport MaterialReference;
    public List<ResourceImport> VertexDescriptorReferences = [];
}

public class RenderableBufferInfo
{
    public const int Size = 0x2C;

    public int Null0;
    public int Unknown0;
    public int Unknown1;
    public int IndicesDataOffset;
    public int IndicesDataSize;
    public int Unknown2;
    public int Null1;
    public int Unknown3;
    public int Unknown4;
    public int VertexDataOffset;
    public int VertexDataSize;
}
