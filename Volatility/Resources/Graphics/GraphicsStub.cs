using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.GraphicsStub)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class GraphicsStub : Resource
{
    public int VehicleGraphicsIndex;
    public int WheelGraphicsIndex;
    public int Unknown0;
    public int Unknown1;
    public ResourceImport GraphicsSpecReference;
    public ResourceImport WheelGraphicsSpecReference;

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        VehicleGraphicsIndex = reader.ReadInt32();
        WheelGraphicsIndex = reader.ReadInt32();
        Unknown0 = reader.ReadInt32();
        Unknown1 = reader.ReadInt32();

        const long importBlockOffset = 0x10;
        ResourceImport.ReadExternalImport(0, reader, importBlockOffset, out ResourceImport firstReference);
        ResourceImport.ReadExternalImport(1, reader, importBlockOffset, out ResourceImport secondReference);

        if (VehicleGraphicsIndex == 1)
        {
            GraphicsSpecReference = firstReference;
            WheelGraphicsSpecReference = secondReference;
        }
        else
        {
            WheelGraphicsSpecReference = firstReference;
            GraphicsSpecReference = secondReference;
        }
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        writer.Write(VehicleGraphicsIndex);
        writer.Write(WheelGraphicsIndex);
        writer.Write(Unknown0);
        writer.Write(Unknown1);
    }

    public override IEnumerable<KeyValuePair<long, ResourceImport>> GetExternalImports()
    {
        if (VehicleGraphicsIndex == 1)
        {
            if (GraphicsSpecReference.ExternalImport)
                yield return new KeyValuePair<long, ResourceImport>(0x0, GraphicsSpecReference);
            if (WheelGraphicsSpecReference.ExternalImport)
                yield return new KeyValuePair<long, ResourceImport>(0x4, WheelGraphicsSpecReference);
        }
        else
        {
            if (WheelGraphicsSpecReference.ExternalImport)
                yield return new KeyValuePair<long, ResourceImport>(0x0, WheelGraphicsSpecReference);
            if (GraphicsSpecReference.ExternalImport)
                yield return new KeyValuePair<long, ResourceImport>(0x4, GraphicsSpecReference);
        }
    }

    public GraphicsStub() : base() { }
    public GraphicsStub(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
