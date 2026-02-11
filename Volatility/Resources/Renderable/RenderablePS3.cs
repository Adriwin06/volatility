namespace Volatility.Resources;

public class RenderablePS3 : RenderableBase
{
    public override Endian GetResourceEndian() => Endian.BE;
    public override Platform GetResourcePlatform() => Platform.PS3;

    protected override DrawIndexedParameters ParseDrawIndexedParameters(ResourceBinaryReader reader)
    {
        uint startIndex = reader.ReadUInt32();
        uint indexCount = reader.ReadUInt32();
        CELL_GCM_PRIMITIVE_TYPE cellPrimitiveType = (CELL_GCM_PRIMITIVE_TYPE)reader.ReadByte();
        GeometryPrimitiveType geometryPrimitiveType = GeometryPrimitiveTypeConverter.ToKind(cellPrimitiveType);
        
        reader.BaseStream.Seek(0x3, SeekOrigin.Current);

        return new DrawIndexedParameters
        {
            StartIndex = startIndex,
            IndexCount = indexCount,
            GeometryPrimitiveType = geometryPrimitiveType
        };
    }
    
    protected override Matrix44 ParseOobb(ResourceBinaryReader reader)
    {
        return PackedOobb.ToMatrix(reader.ReadBytes(0x10));
    }
    
    public RenderablePS3(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
