using System.Numerics;

using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.GraphicsSpec)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class GraphicsSpec : Resource
{
    private const int HeaderSize = 0x30;
    private const int SectionAlignment = 0x10;

    public uint Version = 3;
    public List<GraphicsPart> Parts = [];
    public List<ShatteredGlassPart> ShatteredGlassParts = [];

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        Parts.Clear();
        ShatteredGlassParts.Clear();

        Version = reader.ReadUInt32();
        uint partCount = reader.ReadUInt32();
        int partModelsOffset = reader.ReadInt32();
        uint shatteredGlassCount = reader.ReadUInt32();
        int shatteredGlassOffset = reader.ReadInt32();
        int partLocatorsOffset = reader.ReadInt32();
        int partVolumeIdsOffset = reader.ReadInt32();
        int numRigidBodiesOffset = reader.ReadInt32();
        int rigidBodyTransformPointersOffset = reader.ReadInt32();

        List<int> rigidBodyTransformPointers = [];
        for (int i = 0; i < partCount; i++)
        {
            reader.ParseSection(rigidBodyTransformPointersOffset + (i * sizeof(uint)), r => r.ReadInt32(), out int transformPointer);
            rigidBodyTransformPointers.Add(transformPointer);
        }

        long importBlockOffset = CalculateImportBlockOffset(
            reader,
            rigidBodyTransformPointers,
            partModelsOffset,
            (int)partCount,
            shatteredGlassOffset,
            (int)shatteredGlassCount,
            partLocatorsOffset,
            partVolumeIdsOffset,
            numRigidBodiesOffset,
            rigidBodyTransformPointersOffset);

        for (int i = 0; i < partCount; i++)
        {
            GraphicsPart part = new();
            ResourceImport.ReadExternalImport((long)partModelsOffset + (i * sizeof(uint)), reader, importBlockOffset, out part.ModelReference);
            reader.ParseSection(partLocatorsOffset + (i * 0x40), MatrixUtilities.ReadMatrix44, out part.PartLocator);
            reader.ParseSection(partVolumeIdsOffset + i, r => r.ReadByte(), out part.PartVolumeId);
            reader.ParseSection(numRigidBodiesOffset + i, r => r.ReadByte(), out part.NumRigidBodiesForPart);

            int transformCount = Math.Max(0, (int)part.NumRigidBodiesForPart);
            int transformPointer = i < rigidBodyTransformPointers.Count ? rigidBodyTransformPointers[i] : 0;
            if (transformPointer > 0)
            {
                reader.ParseSection(transformPointer, transformCount, MatrixUtilities.ReadMatrix44, part.RigidBodyToSkinMatrixTransforms);
            }

            Parts.Add(part);
        }

        for (int i = 0; i < shatteredGlassCount; i++)
        {
            long entryOffset = shatteredGlassOffset + (i * ShatteredGlassPart.Size);
            reader.ParseSection(entryOffset, ReadShatteredGlassPart, out ShatteredGlassPart part);
            ResourceImport.ReadExternalImport(entryOffset, reader, importBlockOffset, out part.ModelReference);
            ShatteredGlassParts.Add(part);
        }
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        long currentOffset = HeaderSize;
        long partModelsOffset = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, Parts.Count, sizeof(uint), SectionAlignment);
        long shatteredGlassOffset = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, ShatteredGlassParts.Count, ShatteredGlassPart.Size, SectionAlignment);
        long partLocatorsOffset = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, Parts.Count, 0x40, SectionAlignment);
        long partVolumeIdsOffset = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, Parts.Count, sizeof(byte), 1);
        long numRigidBodiesOffset = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, Parts.Count, sizeof(byte), SectionAlignment);
        long rigidBodyTransformPointersOffset = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, Parts.Count, sizeof(uint), SectionAlignment);

        long[] rigidBodyTransformOffsets = new long[Parts.Count];
        for (int i = 0; i < Parts.Count; i++)
        {
            int count = GetRigidBodyTransformCount(Parts[i]);
            rigidBodyTransformOffsets[i] = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, count, 0x40, SectionAlignment);
        }

        long importBlockOffset = ResourceUtilities.AlignOffset(currentOffset, SectionAlignment);

        writer.Write(Version == 0 ? 3u : Version);
        writer.Write((uint)Parts.Count);
        writer.Write((int)partModelsOffset);
        writer.Write((uint)ShatteredGlassParts.Count);
        writer.Write((int)shatteredGlassOffset);
        writer.Write((int)partLocatorsOffset);
        writer.Write((int)partVolumeIdsOffset);
        writer.Write((int)numRigidBodiesOffset);
        writer.Write((int)rigidBodyTransformPointersOffset);
        writer.WriteFixedBytes(null, HeaderSize - (int)writer.BaseStream.Position);

        writer.WriteSection<GraphicsPart>(partModelsOffset, Parts, (w, _, index) => w.Write(index));
        writer.WriteSection(shatteredGlassOffset, ShatteredGlassParts, WriteShatteredGlassPart);
        writer.WriteSection(partLocatorsOffset, Parts, (w, part) => MatrixUtilities.WriteMatrix44(w, part.PartLocator));
        writer.WriteSection(partVolumeIdsOffset, Parts, (w, part) => w.Write(part.PartVolumeId));
        writer.WriteSection(numRigidBodiesOffset, Parts, (w, part) => w.Write((byte)GetRigidBodyTransformCount(part)));
        writer.WriteSection<GraphicsPart>(rigidBodyTransformPointersOffset, Parts, (w, _, index) => w.Write((uint)rigidBodyTransformOffsets[index]));

        for (int i = 0; i < Parts.Count; i++)
        {
            if (rigidBodyTransformOffsets[i] == 0)
            {
                continue;
            }

            writer.BaseStream.Position = rigidBodyTransformOffsets[i];
            int transformCount = GetRigidBodyTransformCount(Parts[i]);
            for (int j = 0; j < transformCount; j++)
            {
                MatrixUtilities.WriteMatrix44(
                    writer,
                    j < Parts[i].RigidBodyToSkinMatrixTransforms.Count
                        ? Parts[i].RigidBodyToSkinMatrixTransforms[j]
                        : Matrix4x4.Identity);
            }
        }

        long paddingLength = importBlockOffset - writer.BaseStream.Position;
        if (paddingLength > 0)
        {
            writer.WriteFixedBytes(null, (int)paddingLength);
        }
    }

    public override IEnumerable<KeyValuePair<long, ResourceImport>> GetExternalImports()
    {
        long currentOffset = HeaderSize;
        long partModelsOffset = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, Parts.Count, sizeof(uint), SectionAlignment);
        long shatteredGlassOffset = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, ShatteredGlassParts.Count, ShatteredGlassPart.Size, SectionAlignment);

        for (int i = 0; i < Parts.Count; i++)
        {
            if (Parts[i].ModelReference.ExternalImport)
            {
                yield return new KeyValuePair<long, ResourceImport>(partModelsOffset + (i * sizeof(uint)), Parts[i].ModelReference);
            }
        }

        for (int i = 0; i < ShatteredGlassParts.Count; i++)
        {
            if (ShatteredGlassParts[i].ModelReference.ExternalImport)
            {
                yield return new KeyValuePair<long, ResourceImport>(shatteredGlassOffset + (i * ShatteredGlassPart.Size), ShatteredGlassParts[i].ModelReference);
            }
        }
    }

    private static int GetRigidBodyTransformCount(GraphicsPart part)
    {
        return part.RigidBodyToSkinMatrixTransforms.Count > 0
            ? part.RigidBodyToSkinMatrixTransforms.Count
            : Math.Max(0, (int)part.NumRigidBodiesForPart);
    }

    private static long CalculateImportBlockOffset(
        ResourceBinaryReader reader,
        List<int> rigidBodyTransformPointers,
        int partModelsOffset,
        int partCount,
        int shatteredGlassOffset,
        int shatteredGlassCount,
        int partLocatorsOffset,
        int partVolumeIdsOffset,
        int numRigidBodiesOffset,
        int rigidBodyTransformPointersOffset)
    {
        long importBlockOffset = HeaderSize;
        importBlockOffset = Math.Max(importBlockOffset, partModelsOffset + (partCount * sizeof(uint)));
        importBlockOffset = Math.Max(importBlockOffset, shatteredGlassOffset + (shatteredGlassCount * ShatteredGlassPart.Size));
        importBlockOffset = Math.Max(importBlockOffset, partLocatorsOffset + (partCount * 0x40L));
        importBlockOffset = Math.Max(importBlockOffset, partVolumeIdsOffset + partCount);
        importBlockOffset = Math.Max(importBlockOffset, numRigidBodiesOffset + partCount);
        importBlockOffset = Math.Max(importBlockOffset, rigidBodyTransformPointersOffset + (partCount * sizeof(uint)));

        for (int i = 0; i < partCount; i++)
        {
            reader.ParseSection(numRigidBodiesOffset + i, r => r.ReadByte(), out byte transformCount);
            int transformPointer = i < rigidBodyTransformPointers.Count ? rigidBodyTransformPointers[i] : 0;
            if (transformPointer > 0)
            {
                importBlockOffset = Math.Max(importBlockOffset, transformPointer + (Math.Max(1, (int)transformCount) * 0x40L));
            }
        }

        return ResourceUtilities.AlignOffset(importBlockOffset, SectionAlignment);
    }

    private static ShatteredGlassPart ReadShatteredGlassPart(ResourceBinaryReader reader)
    {
        return new ShatteredGlassPart
        {
            ModelIndex = reader.ReadInt32(),
            BodyPartIndex = reader.ReadUInt32(),
            BodyPartType = reader.ReadUInt32(),
        };
    }

    private static void WriteShatteredGlassPart(ResourceBinaryWriter writer, ShatteredGlassPart part)
    {
        writer.Write(part.ModelIndex);
        writer.Write(part.BodyPartIndex);
        writer.Write(part.BodyPartType);
    }

    public GraphicsSpec() : base() { }
    public GraphicsSpec(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public class GraphicsPart
{
    public ResourceImport ModelReference;
    public Matrix4x4 PartLocator;
    public byte PartVolumeId;
    public byte NumRigidBodiesForPart;
    public List<Matrix4x4> RigidBodyToSkinMatrixTransforms = [];
}

public struct ShatteredGlassPart
{
    public const int Size = 0xC;

    public ResourceImport ModelReference;
    public int ModelIndex;
    public uint BodyPartIndex;
    public uint BodyPartType;
}
