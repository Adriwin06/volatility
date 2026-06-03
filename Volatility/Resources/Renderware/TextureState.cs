using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.TextureState)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class TextureState : SidecarBackedResource
{
    public int[] AddressingMode = [0, 0, 0];
    public int[] FilterTypes = [1, 1, 1];
    public float[] MinMaxLod = [0, 0];
    public uint MaxAnisotropy;
    public float MipmapLodBias;
    public int ComparisonFunction;
    public bool BorderColorWhite;
    public int Unknown1;
    public ResourceImport RasterReference;
    public long RasterReferenceOffset = 0x38;

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        long bodyLength = ResourceImportTableUtilities.GetBodyLengthWithoutTrailingImports(reader, 1);
        CaptureBody(reader, bodyLength);
        ParseFields(reader, bodyLength);

        List<KeyValuePair<long, ResourceImport>> trailingImports = ResourceImportTableUtilities.ReadTrailingImports(reader, 1);
        if (trailingImports.Count == 1)
        {
            RasterReferenceOffset = trailingImports[0].Key;
            RasterReference = trailingImports[0].Value;
            return;
        }

        if (!ResourceImport.ReadExternalImport(RasterReferenceOffset, reader, bodyLength, out RasterReference))
        {
            ResourceImport.ReadExternalImport(0, reader, bodyLength, out RasterReference);
        }
    }

    public override IEnumerable<KeyValuePair<long, ResourceImport>> GetExternalImports()
    {
        if (RasterReference.ExternalImport)
        {
            yield return new KeyValuePair<long, ResourceImport>(RasterReferenceOffset, RasterReference);
        }
    }

    private void ParseFields(ResourceBinaryReader reader, long bodyLength)
    {
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        if (reader.Endianness == Endian.BE)
        {
            if (bodyLength >= sizeof(float))
            {
                MipmapLodBias = reader.ReadSingle();
            }

            if (bodyLength > 0x17)
            {
                reader.BaseStream.Seek(0x17, SeekOrigin.Begin);
                MaxAnisotropy = reader.ReadByte();
            }

            return;
        }

        if (bodyLength >= 0x38)
        {
            AddressingMode = [reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()];
            FilterTypes = [reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()];

            if (bodyLength == 0x38)
            {
                MinMaxLod = [reader.ReadSingle(), reader.ReadSingle()];
                MaxAnisotropy = reader.ReadUInt32();
                MipmapLodBias = reader.ReadSingle();
                ComparisonFunction = reader.ReadInt32();
                BorderColorWhite = reader.ReadByte() != 0;
                reader.BaseStream.Seek(0x3, SeekOrigin.Current);
                Unknown1 = reader.ReadInt32();
            }
            else
            {
                MinMaxLod = [0, reader.ReadSingle()];
                MaxAnisotropy = reader.ReadUInt32();
                MipmapLodBias = reader.ReadSingle();
                byte b = reader.ReadByte();
                byte g = reader.ReadByte();
                byte r = reader.ReadByte();
                reader.ReadByte();
                BorderColorWhite = r == 255 && g == 255 && b == 255;
            }
        }
    }

    public TextureState() : base() { }
    public TextureState(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
