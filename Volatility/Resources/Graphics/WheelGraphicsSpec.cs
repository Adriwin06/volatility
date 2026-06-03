namespace Volatility.Resources;

[ResourceDefinition(ResourceType.WheelGraphicsSpec)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class WheelGraphicsSpec : Resource
{
    public uint Version = 1;
    public int WheelModelIndex;
    public int CaliperModelIndex = -1;
    public int Unknown0;
    public ResourceImport WheelModelReference;
    public ResourceImport CaliperModelReference;

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        Version = reader.ReadUInt32();
        WheelModelIndex = reader.ReadInt32();
        CaliperModelIndex = reader.ReadInt32();
        Unknown0 = reader.ReadInt32();

        const long importBlockOffset = 0x10;
        ResourceImport.ReadExternalImport(0x4L, reader, importBlockOffset, out WheelModelReference);
        if (CaliperModelIndex != -1)
        {
            ResourceImport.ReadExternalImport(0x8L, reader, importBlockOffset, out CaliperModelReference);
        }
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        writer.Write(Version == 0 ? 1u : Version);
        writer.Write(WheelModelIndex);
        writer.Write(CaliperModelIndex);
        writer.Write(Unknown0);
    }

    public override IEnumerable<KeyValuePair<long, ResourceImport>> GetExternalImports()
    {
        if (WheelModelReference.ExternalImport)
        {
            yield return new KeyValuePair<long, ResourceImport>(0x4, WheelModelReference);
        }

        if (CaliperModelIndex != -1 && CaliperModelReference.ExternalImport)
        {
            yield return new KeyValuePair<long, ResourceImport>(0x8, CaliperModelReference);
        }
    }

    public WheelGraphicsSpec() : base() { }
    public WheelGraphicsSpec(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
