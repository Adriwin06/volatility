using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.IdList)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class IdList : Resource
{
    private const int HeaderSize = 0x10;
    private const int EntrySize = 0x10;

    public uint IdsOffset = HeaderSize;
    public byte Pad0 = 0x80;
    public byte Pad1 = 0x96;
    public List<ResourceImport> Ids = [];

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        Ids.Clear();

        IdsOffset = reader.ReadUInt32();
        uint count = reader.ReadUInt32();
        Pad0 = reader.ReadByte();
        Pad1 = reader.ReadByte();
        reader.BaseStream.Seek(0x6, SeekOrigin.Current);

        reader.ParseSection(IdsOffset, (int)count, ReadIdListEntry, Ids);
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        IdsOffset = HeaderSize;
        writer.Write(IdsOffset);
        writer.Write((uint)Ids.Count);
        writer.Write(Pad0);
        writer.Write(Pad1);
        writer.WriteFixedBytes(null, 0x6);
        writer.WriteSection(IdsOffset, Ids, WriteIdListEntry);
    }

    private static ResourceImport ReadIdListEntry(ResourceBinaryReader reader)
    {
        ulong id = reader.ReadUInt64();
        reader.BaseStream.Seek(0x8, SeekOrigin.Current);
        return new ResourceImport(id, externalImport: false);
    }

    private static void WriteIdListEntry(ResourceBinaryWriter writer, ResourceImport value)
    {
        writer.Write((ulong)ResourceUtilities.ResolveResourceID(value));
        writer.Write(0ul);
    }

    public IdList() : base() { }
    public IdList(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
