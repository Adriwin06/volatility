namespace Volatility.Resources;

public class RenderablePC : RenderableBase
{
    public override Endian GetResourceEndian() => Endian.LE;
    public override Platform GetResourcePlatform() => Platform.TUB;

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        reader.SetEndianness(GetResourceEndian());

        reader.BaseStream.Seek(0x20, SeekOrigin.Begin);

        IndexBuffer = reader.ReadUInt32();
        VertexBuffer = reader.ReadUInt32();

        reader.BaseStream.Seek(0x0, SeekOrigin.Begin);

        base.ParseFromStream(reader, endianness);
    }

    protected override DrawIndexedParameters ParseDrawIndexedParameters(ResourceBinaryReader reader)
    {
        return new DrawIndexedParameters
        {
            GeometryPrimitiveType = GeometryPrimitiveTypeConverter.ToKind((D3DPRIMITIVETYPE)reader.ReadUInt32()),
            BaseVertexIndex = reader.ReadInt32(),
            StartIndex = reader.ReadUInt32(),
            IndexCount = reader.ReadUInt32(),
            MinimumIndex = reader.ReadUInt32(),
            NumPrimitives = reader.ReadUInt32(),
        };
    }

    public RenderablePC(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
