using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.VertexDescriptor)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class VertexDescriptor : SidecarBackedResource
{
    public int Unknown0;
    public int AttributeFlags;
    public ushort AttributeCount;
    public byte StreamCount;
    public ushort ElementsHash;
    public byte VertexSize;
    public List<VertexAttribute> Attributes = [];

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        Attributes.Clear();
        CaptureBody(reader, reader.BaseStream.Length);

        if (reader.Endianness == Endian.BE)
        {
            ParseBigEndianDescriptor(reader);
        }
        else if (reader.BaseStream.Length >= 0x10)
        {
            ParseLittleEndianDescriptor(reader);
        }
    }

    private void ParseLittleEndianDescriptor(ResourceBinaryReader reader)
    {
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        Unknown0 = reader.ReadInt32();
        int second = reader.ReadInt32();
        int third = reader.ReadInt32();

        bool remasterLayout = reader.BaseStream.Length >= 0x10 && third == 0;
        if (remasterLayout)
        {
            AttributeFlags = second;
            AttributeCount = reader.ReadByte();
            StreamCount = reader.ReadByte();
            ElementsHash = reader.ReadUInt16();

            for (int i = 0; i < AttributeCount && reader.BaseStream.Position + 0x14 <= reader.BaseStream.Length; i++)
            {
                Attributes.Add(new VertexAttribute
                {
                    Semantic = reader.ReadByte(),
                    SemanticIndex = reader.ReadByte(),
                    InputSlot = reader.ReadByte(),
                    ElementClass = reader.ReadByte(),
                    DataType = reader.ReadInt32(),
                    DataOffset = reader.ReadInt32(),
                    StepRate = reader.ReadInt32(),
                    VertexSize = reader.ReadInt32(),
                });
            }
        }
        else
        {
            AttributeFlags = third;
            AttributeCount = reader.ReadByte();
            StreamCount = reader.ReadByte();
            ElementsHash = reader.ReadUInt16();

            for (int i = 0; i < AttributeCount && reader.BaseStream.Position + 0x10 <= reader.BaseStream.Length; i++)
            {
                Attributes.Add(new VertexAttribute
                {
                    InputSlot = reader.ReadByte(),
                    VertexSize = reader.ReadByte(),
                    DataOffset = reader.ReadUInt16(),
                    DataType = reader.ReadByte(),
                    Padding = reader.ReadBytes(3),
                    TessellationMethod = reader.ReadByte(),
                    Semantic = reader.ReadByte(),
                    SemanticIndex = reader.ReadByte(),
                    IndexedUsage = reader.ReadByte(),
                    ElementClass = reader.ReadInt32(),
                });
            }
        }
    }

    private void ParseBigEndianDescriptor(ResourceBinaryReader reader)
    {
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        Unknown0 = reader.ReadInt32();
        AttributeFlags = reader.ReadInt32();

        ushort fieldA = reader.ReadUInt16();
        ushort fieldB = reader.ReadUInt16();
        ushort fieldC = reader.ReadUInt16();
        ushort fieldD = reader.ReadUInt16();

        bool ps3Layout = fieldB > 0 && fieldD == 0;
        AttributeCount = ps3Layout ? fieldB : fieldA;
        ElementsHash = fieldC;

        for (int i = 0; i < AttributeCount && reader.BaseStream.Position + (ps3Layout ? 0x8 : 0x10) <= reader.BaseStream.Length; i++)
        {
            if (ps3Layout)
            {
                byte dataType0 = reader.ReadByte();
                byte dataType1 = reader.ReadByte();
                Attributes.Add(new VertexAttribute
                {
                    DataType = (dataType0 << 8) | dataType1,
                    DataOffset = reader.ReadUInt16(),
                    VertexSize = reader.ReadUInt16(),
                    Semantic = reader.ReadByte(),
                    UnknownByte = reader.ReadByte(),
                });
            }
            else
            {
                Attributes.Add(new VertexAttribute
                {
                    DataOffset = reader.ReadInt32(),
                    UnknownByte = reader.ReadByte(),
                    DataType = (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte(),
                    Padding = reader.ReadBytes(3),
                    Semantic = reader.ReadByte(),
                    ElementClass = reader.ReadInt32(),
                });
            }
        }

        if (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            VertexSize = reader.ReadByte();
        }
    }

    public VertexDescriptor() : base() { }
    public VertexDescriptor(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public class VertexAttribute
{
    public byte Semantic;
    public byte SemanticIndex;
    public byte InputSlot;
    public byte ElementClassByte;
    public int ElementClass;
    public int DataType;
    public int DataOffset;
    public int StepRate;
    public int VertexSize;
    public byte TessellationMethod;
    public byte IndexedUsage;
    public byte UnknownByte;
    public byte[] Padding = [];
}
