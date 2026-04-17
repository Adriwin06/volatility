using Volatility.Utilities;

using static Volatility.Utilities.MatrixUtilities;

namespace Volatility.Resources;

// The Instance List resource type contains lists of models along with their
// respective locations in the game world. It serves as one of the top-level
// resource types for track unit loading.
//
// Learn More:
// https://burnout.wiki/wiki/Instance_List

[ResourceDefinition(ResourceType.InstanceList)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class InstanceList : Resource
{
    private const int HeaderSize = 0x10;
    private const int SectionAlignment = 0x10;

    [EditorLabel("Number of instances"), EditorCategory("Instance List"), EditorReadOnly, EditorTooltip("The amount of instances that have a model assigned, but NOT the size of the entire instance array.")]
    public uint NumInstances;

    [EditorLabel("Instances"), EditorCategory("Instance List"), EditorTooltip("The list of instances in this list.")]
    public List<Instance> Instances = [];

    public InstanceList() : base() { }

    public InstanceList(string path, Endian endianness = Endian.Agnostic)
        : base(path, endianness) { }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        Arch arch = ResourceArch;
        int instanceBlockSize = GetInstanceBlockSize(arch);
        uint entryCount = (uint)Instances.Count;

        long currentOffset = HeaderSize;
        long instanceListOffset = ResourceUtilities.GetSectionOffset(
            ref currentOffset,
            checked((int)(entryCount * instanceBlockSize)),
            SectionAlignment);

        writer.Write((int)instanceListOffset);
        writer.Write(entryCount);
        writer.Write(NumInstances);
        writer.Write(1u);

        writer.WriteSection(instanceListOffset, Instances, (w, instance) => WriteInstanceBlock(w, instance, arch));
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

        for (int i = 0; i < entries; i++)
        {
            long instanceOffset = instanceListPtr + (instanceBlockSize * i);

            reader.ParseSection(instanceOffset, r => ReadInstance(r, ResourceArch, importBlockOffset), out Instance instance);
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
        long importBlockOffset)
    {
        long blockStart = reader.BaseStream.Position;

        ResourceImport.ReadExternalImport(blockStart, reader, importBlockOffset, out ResourceImport modelReference);
        reader.BaseStream.Seek(blockStart + GetImportPlaceholderSize(arch), SeekOrigin.Begin);

        short backdropZoneId = reader.ReadInt16();
        reader.BaseStream.Seek(0x2, SeekOrigin.Current);
        float maxVisibleDistanceSquared = reader.ReadSingle();
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);
        Matrix44Affine transformMatrix = ReadMatrix44Affine(reader);
        Transform transform = Matrix44AffineToTransform(transformMatrix);

        return new Instance
        {
            ModelReference = modelReference,
            BackdropZoneID = backdropZoneId,
            MaxVisibleDistanceSquared = maxVisibleDistanceSquared,
            Transform = transform,
            TransformMatrix = transformMatrix,
        };
    }

    private static int GetImportPlaceholderSize(Arch arch)
    {
        return arch == Arch.x64 ? sizeof(ulong) : sizeof(uint);
    }

    private static void WriteInstanceBlock(ResourceBinaryWriter writer, Instance instance, Arch arch)
    {
        long blockStart = writer.BaseStream.Position;

        writer.WritePointer(0, arch);
        writer.Write(instance.BackdropZoneID);
        writer.Write(new byte[0x2]);
        writer.Write(instance.MaxVisibleDistanceSquared);
        writer.Write(new byte[0x4]);

        Matrix44Affine transformMatrix = instance.TransformMatrix != default
            ? instance.TransformMatrix
            : TransformToMatrix44Affine(instance.Transform);
        WriteMatrix44Affine(writer, transformMatrix);

        int remaining = GetInstanceBlockSize(arch) - (int)(writer.BaseStream.Position - blockStart);
        if (remaining < 0)
        {
            throw new InvalidDataException(
                $"Instance block overflow. Wrote 0x{writer.BaseStream.Position - blockStart:X} bytes into a 0x{GetInstanceBlockSize(arch):X} byte block.");
        }

        if (remaining > 0)
        {
            writer.Write(new byte[remaining]);
        }
    }

    public IEnumerable<KeyValuePair<long, ResourceImport>> GetExternalImports()
    {
        int instanceBlockSize = GetInstanceBlockSize(ResourceArch);
        for (int i = 0; i < Instances.Count; i++)
        {
            ResourceImport modelReference = Instances[i].ModelReference;
            if (!modelReference.ExternalImport)
            {
                continue;
            }

            yield return new KeyValuePair<long, ResourceImport>(
                HeaderSize + (i * instanceBlockSize),
                modelReference);
        }
    }
}

public struct Instance
{
    [EditorLabel("Resource ID"), EditorCategory("InstanceList/Instances"), EditorTooltip("The reference to the resource placed by this instance.")]
    public ResourceImport ResourceId;

    [EditorLabel("Transform"), EditorCategory("InstanceList/Instances"), EditorTooltip("The location, rotation, and scale of this instance.")]
    public Transform Transform;

    [EditorHidden]
    public Matrix44Affine TransformMatrix;

    [EditorLabel("Transform"), EditorCategory("InstanceList/Instances"), EditorTooltip("If this is a backdrop, the PVS Zone ID that this backdrop represents.")]
    public short BackdropZoneID;

    [EditorLabel("Max Visible Distance Squared"), EditorCategory("InstanceList/Instances"), EditorTooltip("The maximum distance that this instance can be seen (in meters), squared.")]
    public float MaxVisibleDistanceSquared;

    [EditorHidden]
    public ResourceImport ModelReference;
}
