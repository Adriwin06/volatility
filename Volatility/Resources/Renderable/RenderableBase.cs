using System.Collections;

using Volatility.Utilities;

namespace Volatility.Resources;

// The Renderable resource type contains all the 3D data used by each
// Model resource in Burnout Paradise. Essentially, Renderables hold the
// geometric and visual information needed for rendering models in-game.

// Learn More:
// https://burnout.wiki/wiki/Renderable

public abstract class RenderableBase : Resource
{
    public Vector3Plus BoundingSphere;
    public ushort Version;
    ushort NumMeshes;
    uint MeshesPtr;
    public List<RenderableMesh> Meshes;
    public BitArray Flags = new BitArray(16);
    public uint IndexBuffer;                    // Only on PC platforms
    public uint VertexBuffer;                   // Only on PC platforms

    public override ResourceType GetResourceType() => ResourceType.Renderable;

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        // TODO: ReadVector3Plus
        BoundingSphere[0] = reader.ReadSingle();            // X
        BoundingSphere[1] = reader.ReadSingle();            // Y
        BoundingSphere[2] = reader.ReadSingle();            // Z
        BoundingSphere[3] = reader.ReadSingle();            // Plus

        Version = reader.ReadUInt16();

        NumMeshes = reader.ReadUInt16();
        uint MeshesPtr = reader.ReadUInt32();               // Pointer to a pointer
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);    // mpObjectScopeTextureInfo
        using BitReader bitReader = new(reader.ReadBytes(4));
        Flags = bitReader.ReadBitsToBitArray(16);

        ParseRenderableMeshes(reader);
    }

    protected void ParseRenderableMeshes(ResourceBinaryReader reader)
    {
        reader.BaseStream.Seek(MeshesPtr, SeekOrigin.Begin);

        for (int i = 0; i < NumMeshes; i++)
        {
            Console.WriteLine($"{MeshesPtr + (i * 0x4)}");
            reader.BaseStream.Seek(MeshesPtr + (i * 0x4), SeekOrigin.Begin);
            reader.BaseStream.Seek(reader.ReadUInt32(), SeekOrigin.Begin);
            RenderableMesh mesh = new();
            mesh.BoundingBox = MatrixUtilities.ReadMatrix44(reader);
            Meshes.Add(mesh);
        }
    }

    public struct RenderableMesh
    {
        public Matrix44 BoundingBox;
    }

    public RenderableBase(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public class DrawIndexedParametersBase
{

}