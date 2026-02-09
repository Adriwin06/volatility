namespace Volatility.Resources;

public class RenderableBPR : RenderableBase
{
    public override Endian GetResourceEndian() => Endian.LE;
    public override Platform GetResourcePlatform() => Platform.BPR;

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        reader.SetEndianness(GetResourceEndian());

        reader.BaseStream.Seek(0x20, SeekOrigin.Begin);

        IndexBuffer = reader.ReadUInt32();
        VertexBuffer = reader.ReadUInt32();

        reader.BaseStream.Seek(0x0, SeekOrigin.Begin);

        base.ParseFromStream(reader, endianness);
    }

    public override DrawIndexedParameters ParseDrawIndexedParameters(ResourceBinaryReader reader)
    {
        GeometryPrimitiveType kind = GeometryPrimitiveTypeConverter.ToKind((D3D11_PRIMITIVE_TOPOLOGY)reader.ReadUInt32());
        int baseVertexIndex = reader.ReadInt32();
        uint startIndex = reader.ReadUInt32();
        uint indexCount = reader.ReadUInt32();
        uint numPrimitives = GeometryPrimitiveTypeConverter.PrimitiveCountFromIndices(kind, indexCount);

        return new DrawIndexedParameters
        {
            GeometryPrimitiveType = kind,
            BaseVertexIndex = baseVertexIndex,
            StartIndex = startIndex,
            IndexCount = indexCount,
            MinimumIndex = 0,
            NumPrimitives = numPrimitives
        };
    }

    public RenderableBPR(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}