using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.PropGraphicsList)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class PropGraphicsList : Resource
{
    private const int HeaderSize = 0x20;
    private const int PropGraphicsSize = 0xC;
    private const int SectionAlignment = 0x10;

    public uint SizeInBytes;
    public uint ZoneNumber;
    public List<PropGraphics> PropModels = [];
    public List<PropPartGraphics> PropPartModels = [];

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        PropModels.Clear();
        PropPartModels.Clear();

        SizeInBytes = reader.ReadUInt32();
        ZoneNumber = reader.ReadUInt32();
        uint propModelCount = reader.ReadUInt32();
        uint propPartModelCount = reader.ReadUInt32();
        int propGraphicsOffset = reader.ReadInt32();
        int propPartGraphicsOffset = reader.ReadInt32();

        long importBlockOffset = ResourceUtilities.AlignOffset(
            Math.Max(
                propGraphicsOffset + ((long)propModelCount * PropGraphicsSize),
                propPartGraphicsOffset + ((long)propPartModelCount * PropGraphicsSize)),
            SectionAlignment);

        for (int i = 0; i < propModelCount; i++)
        {
            long entryOffset = propGraphicsOffset + (i * PropGraphicsSize);
            reader.ParseSection(entryOffset, ReadPropGraphics, out PropGraphics prop);
            ResourceImport.ReadExternalImport(entryOffset + 0x4, reader, importBlockOffset, out prop.ModelReference);
            PropModels.Add(prop);
        }

        for (int i = 0; i < propPartModelCount; i++)
        {
            long entryOffset = propPartGraphicsOffset + (i * PropGraphicsSize);
            reader.ParseSection(entryOffset, ReadPropPartGraphics, out PropPartGraphics propPart);
            ResourceImport.ReadExternalImport(entryOffset + 0x8, reader, importBlockOffset, out propPart.ModelReference);
            PropPartModels.Add(propPart);
        }
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        long currentOffset = HeaderSize;
        long propGraphicsOffset = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, PropModels.Count, PropGraphicsSize, SectionAlignment);
        long propPartGraphicsOffset = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, PropPartModels.Count, PropGraphicsSize, SectionAlignment);
        SizeInBytes = (uint)ResourceUtilities.AlignOffset(currentOffset, SectionAlignment);

        writer.Write(SizeInBytes);
        writer.Write(ZoneNumber);
        writer.Write((uint)PropModels.Count);
        writer.Write((uint)PropPartModels.Count);
        writer.Write((int)propGraphicsOffset);
        writer.Write((int)propPartGraphicsOffset);
        writer.WriteFixedBytes(null, HeaderSize - (int)writer.BaseStream.Position);

        writer.WriteSection(propGraphicsOffset, PropModels, WritePropGraphics);
        writer.WriteSection(propPartGraphicsOffset, PropPartModels, WritePropPartGraphics);
        long paddingLength = SizeInBytes - writer.BaseStream.Position;
        if (paddingLength > 0)
        {
            writer.WriteFixedBytes(null, (int)paddingLength);
        }
    }

    public override IEnumerable<KeyValuePair<long, ResourceImport>> GetExternalImports()
    {
        long currentOffset = HeaderSize;
        long propGraphicsOffset = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, PropModels.Count, PropGraphicsSize, SectionAlignment);
        long propPartGraphicsOffset = (long)ResourceUtilities.GetSectionOffset(ref currentOffset, PropPartModels.Count, PropGraphicsSize, SectionAlignment);

        for (int i = 0; i < PropModels.Count; i++)
        {
            if (PropModels[i].ModelReference.ExternalImport)
            {
                yield return new KeyValuePair<long, ResourceImport>(
                    propGraphicsOffset + (i * PropGraphicsSize) + 0x4,
                    PropModels[i].ModelReference);
            }
        }

        for (int i = 0; i < PropPartModels.Count; i++)
        {
            if (PropPartModels[i].ModelReference.ExternalImport)
            {
                yield return new KeyValuePair<long, ResourceImport>(
                    propPartGraphicsOffset + (i * PropGraphicsSize) + 0x8,
                    PropPartModels[i].ModelReference);
            }
        }
    }

    private static PropGraphics ReadPropGraphics(ResourceBinaryReader reader)
    {
        return new PropGraphics
        {
            TypeId = reader.ReadUInt32(),
            PropModelPointer = reader.ReadInt32(),
            PartsPointer = reader.ReadInt32(),
        };
    }

    private static PropPartGraphics ReadPropPartGraphics(ResourceBinaryReader reader)
    {
        return new PropPartGraphics
        {
            TypeId = reader.ReadUInt32(),
            PartId = reader.ReadUInt32(),
            PropModelPointer = reader.ReadInt32(),
        };
    }

    private static void WritePropGraphics(ResourceBinaryWriter writer, PropGraphics value)
    {
        writer.Write(value.TypeId);
        writer.Write(value.PropModelPointer);
        writer.Write(value.PartsPointer);
    }

    private static void WritePropPartGraphics(ResourceBinaryWriter writer, PropPartGraphics value)
    {
        writer.Write(value.TypeId);
        writer.Write(value.PartId);
        writer.Write(value.PropModelPointer);
    }

    public PropGraphicsList() : base() { }
    public PropGraphicsList(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public struct PropGraphics
{
    public uint TypeId;
    public int PropModelPointer;
    public int PartsPointer;
    public ResourceImport ModelReference;
}

public struct PropPartGraphics
{
    public uint TypeId;
    public uint PartId;
    public int PropModelPointer;
    public ResourceImport ModelReference;
}
