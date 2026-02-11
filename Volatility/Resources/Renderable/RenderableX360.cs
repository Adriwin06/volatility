namespace Volatility.Resources;

public class RenderableX360 : RenderableBase
{
    public override Endian GetResourceEndian() => Endian.BE;
    public override Platform GetResourcePlatform() => Platform.X360;

    protected override DrawIndexedParameters ParseDrawIndexedParameters(ResourceBinaryReader reader)
    {
        return new DrawIndexedParameters
        {
            GeometryPrimitiveType = GeometryPrimitiveTypeConverter.ToKind((D3DPRIMITIVETYPE)reader.ReadUInt32()),
            BaseVertexIndex = reader.ReadInt32(),
            StartIndex = reader.ReadUInt32(),
            IndexCount = reader.ReadUInt32(),
        };
    }
    
    protected override Matrix44 ParseOobb(ResourceBinaryReader reader)
    {
        return PackedOobb.ToMatrix(reader.ReadBytes(0x10));
    }

    public RenderableX360(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
