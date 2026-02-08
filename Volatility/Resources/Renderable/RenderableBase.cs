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
    public List<RenderableMesh> Meshes = new();
    public BitArray Flags = new(16);
    public uint IndexBuffer;                    // Only on PC platforms
    public uint VertexBuffer;                   // Only on PC platforms

    public override ResourceType GetResourceType() => ResourceType.Renderable;

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        // TODO: implement ReadVector3Plus function
        BoundingSphere[0] = reader.ReadSingle();            // X
        BoundingSphere[1] = reader.ReadSingle();            // Y
        BoundingSphere[2] = reader.ReadSingle();            // Z
        BoundingSphere[3] = reader.ReadSingle();            // Plus

        Version = reader.ReadUInt16();
        uint numMeshes = reader.ReadUInt16();
        uint meshesPtr = reader.ReadUInt32();               // Pointer to a pointer
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);    // mpObjectScopeTextureInfo
        using BitReader bitReader = new(reader.ReadBytes(4));
        Flags = bitReader.ReadBitsToBitArray(16);

        ParseRenderableMeshes(reader, numMeshes, meshesPtr);
    }

    protected void ParseRenderableMeshes(ResourceBinaryReader reader, uint numMeshes, uint meshesPtr)
    {
        reader.BaseStream.Seek(meshesPtr, SeekOrigin.Begin);

        for (int i = 0; i < numMeshes; i++)
        {
            reader.BaseStream.Seek(meshesPtr + (i * 0x4), SeekOrigin.Begin);
            reader.BaseStream.Seek(reader.ReadUInt32(), SeekOrigin.Begin);
            RenderableMesh mesh = new()
            {
                BoundingBox = MatrixUtilities.ReadMatrix44(reader),
                // DrawIndexedParameters
                // MaterialAssembly - ResourceImport.ReadExternalImport
            };
            Meshes.Add(mesh);
        }
    }

    public struct RenderableMesh
    {
        public Matrix44 BoundingBox;
        public DrawIndexedParameters DrawIndexedParameters;
        public ResourceImport MaterialAssembly;
        public byte NumVertexDescriptors;
        public byte InstanceCount;
        public byte NumVertexBuffers;
        public byte Flags;
    }

    public struct DrawIndexedParameters
    {
        public GeometryPrimitiveType GeometryPrimitiveType;
        public int BaseVertexIndex;
        public uint StartIndex;
        public uint IndexCount;     // number of indices in docs
        public uint MinimumIndex;
        public uint NumPrimitives;
    }

    public RenderableBase(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

