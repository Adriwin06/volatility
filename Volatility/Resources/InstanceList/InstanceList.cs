using Volatility.Utilities;

using static Volatility.Utilities.MatrixUtilities;

namespace Volatility.Resources;

// The Instance List resource type contains lists of models along with their
// respective locations in the game world. It serves as one of the top-level
// resource types for track unit loading.
//
// Learn More:
// https://burnout.wiki/wiki/Instance_List

public class InstanceList : Resource
{
    private const int HeaderSize = 0x10;
    private const int SectionAlignment = 0x10;
    private const int ImportEntrySize = 0x10;
    private const int ResourceIdEntrySize = 0x10;
    private const int InstanceBodySize = 0x4C;

    public override ResourceType ResourceType => ResourceType.InstanceList;

    [EditorLabel("Number of instances"), EditorCategory("Instance List"), EditorReadOnly, EditorTooltip("The amount of instances that have a model assigned, but NOT the size of the entire instance array.")]
    public uint NumInstances;

    [EditorLabel("Instances"), EditorCategory("Instance List"), EditorTooltip("The list of instances in this list.")]
    public List<Instance> Instances = [];

    public InstanceList() : base() { }

    public InstanceList(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        Arch arch = ResourceArch;
        int instanceBlockSize = GetInstanceBlockSize(arch);
        uint entryCount = (uint)Instances.Count;
        uint numInstances = NumInstances == 0 ? entryCount : Math.Min(NumInstances, entryCount);

        long currentOffset = HeaderSize;
        long instanceListOffset = ResourceUtilities.GetSectionOffset(
            ref currentOffset,
            checked((int)(entryCount * instanceBlockSize)),
            SectionAlignment);
        long importBlockOffset = ResourceUtilities.GetSectionOffset(
            ref currentOffset,
            checked((int)(entryCount * ImportEntrySize)),
            SectionAlignment);
        long resourceIdBlockOffset = ResourceUtilities.GetSectionOffset(
            ref currentOffset,
            checked((int)(entryCount * ResourceIdEntrySize)),
            SectionAlignment);

        writer.Write((int)instanceListOffset);
        writer.Write(entryCount);
        writer.Write(numInstances);
        writer.Write(1u);

        writer.WriteSection(instanceListOffset, Instances, (w, instance) => WriteInstanceBlock(w, instance, arch));
        writer.WriteSection(importBlockOffset, Instances, (w, instance, index) =>
            WriteImportEntry(w, instance, instanceListOffset + ((long)index * instanceBlockSize)));
        writer.WriteSection(resourceIdBlockOffset, Instances, WriteResourceIdEntry);
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        long instanceListPtr = reader.ReadInt32();
        uint entries = reader.ReadUInt32();
        NumInstances = reader.ReadUInt32();

        if (reader.ReadUInt32() != 1)
        {
            throw new InvalidDataException("Version mismatch!");
        }

        Instances.Clear();

        long instanceBlockSize = GetInstanceBlockSize(ResourceArch);
        long importBlockOffset = instanceListPtr + (instanceBlockSize * entries);
        long resourceIdBlockOffset = importBlockOffset + (ImportEntrySize * entries);

        for (int i = 0; i < entries; i++)
        {
            long instanceOffset = instanceListPtr + (instanceBlockSize * i);
            long resourceIdOffset = resourceIdBlockOffset + (ResourceIdEntrySize * i);

            reader.ParseSection(instanceOffset, r => ReadInstance(r, ResourceArch, importBlockOffset, resourceIdOffset), out Instance instance);
            Instances.Add(instance);
        }
    }

    private static int GetInstanceBlockSize(Arch arch)
    {
        return arch == Arch.x64 ? 0x60 : 0x50;
    }

    private static Instance ReadInstance(
        ResourceBinaryReader reader,
        Arch arch,
        long importBlockOffset,
        long resourceIdOffset)
    {
        long blockStart = reader.BaseStream.Position;

        ResourceImport.ReadExternalImport(blockStart, reader, importBlockOffset, out ResourceImport modelReference);

        short backdropZoneId = reader.ReadInt16();
        reader.BaseStream.Seek(0x6, SeekOrigin.Current);
        float maxVisibleDistanceSquared = reader.ReadSingle();
        Transform transform = Matrix44AffineToTransform(ReadMatrix44Affine(reader));

        ResourceImport resourceId = default;
        reader.ParseSection(resourceIdOffset, ReadResourceId, out resourceId);

        return new Instance
        {
            ModelReference = modelReference,
            BackdropZoneID = backdropZoneId,
            MaxVisibleDistanceSquared = maxVisibleDistanceSquared,
            Transform = transform,
            ResourceId = resourceId,
        };
    }

    private static ResourceImport ReadResourceId(ResourceBinaryReader reader)
    {
        ResourceImport resourceId = new()
        {
            ReferenceID = reader.ReadUInt32(),
            ExternalImport = false
        };

        reader.BaseStream.Seek(0xC, SeekOrigin.Current);
        return resourceId;
    }

    private static void WriteInstanceBlock(ResourceBinaryWriter writer, Instance instance, Arch arch)
    {
        writer.Write(instance.BackdropZoneID);
        writer.Write(new byte[0x6]);
        writer.Write(instance.MaxVisibleDistanceSquared);
        WriteMatrix44Affine(writer, TransformToMatrix44Affine(instance.Transform));
        writer.Write(new byte[GetInstanceBlockSize(arch) - InstanceBodySize]);
    }

    private static void WriteImportEntry(ResourceBinaryWriter writer, Instance instance, long fileOffset)
    {
        writer.Write(ResourceUtilities.ResolveResourceID(instance.ModelReference));
        writer.Write((uint)fileOffset);
        writer.Write(0u);
    }

    private static void WriteResourceIdEntry(ResourceBinaryWriter writer, Instance instance, int index)
    {
        _ = index;
        writer.Write((uint)ResourceUtilities.ResolveResourceID(instance.ResourceId));
        writer.Write(new byte[0xC]);
    }
}

public struct Instance
{
    [EditorLabel("Resource ID"), EditorCategory("InstanceList/Instances"), EditorTooltip("The reference to the resource placed by this instance.")]
    public ResourceImport ResourceId;

    [EditorLabel("Transform"), EditorCategory("InstanceList/Instances"), EditorTooltip("The location, rotation, and scale of this instance.")]
    public Transform Transform;

    [EditorLabel("Transform"), EditorCategory("InstanceList/Instances"), EditorTooltip("If this is a backdrop, the PVS Zone ID that this backdrop represents.")]
    public short BackdropZoneID;

    [EditorLabel("Max Visible Distance Squared"), EditorCategory("InstanceList/Instances"), EditorTooltip("The maximum distance that this instance can be seen (in meters), squared.")]
    public float MaxVisibleDistanceSquared;

    [EditorHidden]
    public ResourceImport ModelReference;
}
