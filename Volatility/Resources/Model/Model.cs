using Volatility.Utilities;

namespace Volatility.Resources;

// The Model resource type links top-level resources (like InstanceList)
// to Renderable resources that contain the 3D geometry, while including
// Level of Detail (LOD) information.
//
// Learn More:
// https://burnout.wiki/wiki/Model

public class Model : Resource
{
    private const int HeaderSize = 0x14;
    private const int RenderableOffsetSize = sizeof(uint);
    private const int StateSize = sizeof(byte);
    private const int LodDistanceSize = sizeof(float);
    private const int ImportEntrySize = 0x10;

    [EditorCategory("Model Container"), EditorLabel("Flags")]
    public byte Flags;

    [EditorCategory("Model Container"), EditorLabel("Models")]
    public List<ModelData> ModelDatas = [];

    public override ResourceType ResourceType => ResourceType.Model;
    public override Platform ResourcePlatform => Platform.Agnostic;

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        int modelCount = ModelDatas.Count;
        if (modelCount > byte.MaxValue)
        {
            throw new InvalidDataException("Model resources cannot store more than 255 renderables.");
        }

        long currentOffset = HeaderSize;
        long renderablesOffset = ResourceUtilities.GetSectionOffset(
            ref currentOffset,
            modelCount * RenderableOffsetSize,
            1);
        long statesOffset = ResourceUtilities.GetSectionOffset(
            ref currentOffset,
            modelCount * StateSize,
            1);
        long lodDistancesOffset = ResourceUtilities.GetSectionOffset(
            ref currentOffset,
            modelCount * LodDistanceSize,
            1);
        long importsOffset = ResourceUtilities.GetSectionOffset(
            ref currentOffset,
            modelCount * ImportEntrySize,
            1);

        writer.Write((uint)renderablesOffset);
        writer.Write((uint)statesOffset);
        writer.Write((uint)lodDistancesOffset);
        writer.Write(-1);
        writer.Write((byte)modelCount);
        writer.Write(Flags);
        writer.Write((byte)modelCount);
        writer.Write((byte)0x02);

        writer.WriteSection<ModelData>(renderablesOffset, ModelDatas, static (w, _, index) => w.Write((uint)(index * ImportEntrySize)));
        writer.WriteSection(statesOffset, ModelDatas, (w, modelData) => w.Write((byte)modelData.State));
        writer.WriteSection(lodDistancesOffset, ModelDatas, (w, modelData) => w.Write(modelData.LODDistance));
        writer.WriteSection<ModelData>(importsOffset, ModelDatas, WriteImportEntry);
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        reader.BaseStream.Seek(0x13, SeekOrigin.Begin);
        if (reader.ReadByte() != 0x2)
        {
            throw new InvalidDataException("Version mismatch!");
        }

        reader.BaseStream.Seek(0x0, SeekOrigin.Begin);

        uint renderablesPtr = reader.ReadUInt32();
        uint renderableStatesPtr = reader.ReadUInt32();
        uint lodDistancesPtr = reader.ReadUInt32();

        _ = reader.ReadInt32();

        byte numRenderables = reader.ReadByte();
        if (numRenderables == 0)
        {
            Console.WriteLine("WARNING: Found no renderables in this model!");
        }

        Flags = reader.ReadByte();

        long importsOffset = Math.Max(
            lodDistancesPtr + (numRenderables * LodDistanceSize),
            Math.Max(
                renderablesPtr + (numRenderables * RenderableOffsetSize),
                renderableStatesPtr + (numRenderables * StateSize)));

        ModelDatas.Clear();
        for (int i = 0; i < numRenderables; i++)
        {
            ModelDatas.Add(ReadModelData(
                reader,
                i,
                numRenderables,
                renderablesPtr,
                renderableStatesPtr,
                lodDistancesPtr,
                importsOffset));
        }
    }

    public Model() : base() { }

    public Model(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

    private static ModelData ReadModelData(
        ResourceBinaryReader reader,
        int index,
        byte numRenderables,
        uint renderablesPtr,
        uint renderableStatesPtr,
        uint lodDistancesPtr,
        long importsOffset)
    {
        ModelData modelData = new();

        reader.ParseSection(renderablesPtr + (index * RenderableOffsetSize), r => r.ReadUInt32(), out uint importRelativeOffset);
        reader.ParseSection(renderableStatesPtr + index, r => (State)r.ReadByte(), out modelData.State);
        reader.ParseSection(lodDistancesPtr + (index * LodDistanceSize), r => r.ReadSingle(), out modelData.LODDistance);

        reader.BaseStream.Seek(
            importRelativeOffset +
            renderablesPtr +
            (numRenderables * (RenderableOffsetSize + StateSize + LodDistanceSize)) +
            (reader.Endianness == Endian.BE ? 0x4 : 0x0),
            SeekOrigin.Begin);

        ResourceImport.ReadExternalImport(index, reader, importsOffset, out modelData.ResourceReference);
        return modelData;
    }

    private static void WriteImportEntry(ResourceBinaryWriter writer, ModelData modelData, int index)
    {
        _ = index;
        writer.Write(ResourceUtilities.ResolveResourceID(modelData.ResourceReference));
        writer.Write(0u);
        writer.Write(0u);
    }

    public struct ModelData
    {
        [EditorCategory("Model Data"), EditorLabel("Resource Reference")]
        public ResourceImport ResourceReference;

        [EditorCategory("Model Data"), EditorLabel("Model State")]
        public State State;

        [EditorCategory("Model Data"), EditorLabel("LOD Render Distance")]
        public float LODDistance;
    }

    public enum State : byte
    {
        [EditorLabel("LOD 0")]
        E_STATE_LOD_0 = 0,
        [EditorLabel("LOD 1")]
        E_STATE_LOD_1 = 1,
        [EditorLabel("LOD 2")]
        E_STATE_LOD_2 = 2,
        [EditorLabel("LOD 3")]
        E_STATE_LOD_3 = 3,
        [EditorLabel("LOD 4")]
        E_STATE_LOD_4 = 4,
        [EditorLabel("LOD 5")]
        E_STATE_LOD_5 = 5,
        [EditorLabel("LOD 6")]
        E_STATE_LOD_6 = 6,
        [EditorLabel("LOD 7")]
        E_STATE_LOD_7 = 7,
        [EditorLabel("LOD 8")]
        E_STATE_LOD_8 = 8,
        [EditorLabel("LOD 9")]
        E_STATE_LOD_9 = 9,
        [EditorLabel("LOD 10")]
        E_STATE_LOD_10 = 10,
        [EditorLabel("LOD 11")]
        E_STATE_LOD_11 = 11,
        [EditorLabel("LOD 12")]
        E_STATE_LOD_12 = 12,
        [EditorLabel("LOD 13")]
        E_STATE_LOD_13 = 13,
        [EditorLabel("LOD 14")]
        E_STATE_LOD_14 = 14,
        [EditorLabel("LOD 15")]
        E_STATE_LOD_15 = 15,
        [EditorLabel("Game Specific 0")]
        E_STATE_GAME_SPECIFIC_0 = 16,
        [EditorLabel("Game Specific 1")]
        E_STATE_GAME_SPECIFIC_1 = 17,
        [EditorLabel("Game Specific 2")]
        E_STATE_GAME_SPECIFIC_2 = 18,
        [EditorLabel("Game Specific 3")]
        E_STATE_GAME_SPECIFIC_3 = 19,
        [EditorLabel("Game Specific 4")]
        E_STATE_GAME_SPECIFIC_4 = 20,
        [EditorLabel("Game Specific 5")]
        E_STATE_GAME_SPECIFIC_5 = 21,
        [EditorLabel("Game Specific 6")]
        E_STATE_GAME_SPECIFIC_6 = 22,
        [EditorLabel("Game Specific 7")]
        E_STATE_GAME_SPECIFIC_7 = 23,
        [EditorLabel("Game Specific 8")]
        E_STATE_GAME_SPECIFIC_8 = 24,
        [EditorLabel("Game Specific 9")]
        E_STATE_GAME_SPECIFIC_9 = 25,
        [EditorLabel("Game Specific 10")]
        E_STATE_GAME_SPECIFIC_10 = 26,
        [EditorLabel("Game Specific 11")]
        E_STATE_GAME_SPECIFIC_11 = 27,
        [EditorLabel("Game Specific 12")]
        E_STATE_GAME_SPECIFIC_12 = 28,
        [EditorLabel("Game Specific 13")]
        E_STATE_GAME_SPECIFIC_13 = 29,
        [EditorLabel("Game Specific 14")]
        E_STATE_GAME_SPECIFIC_14 = 30,
        [EditorLabel("Game Specific 15")]
        E_STATE_GAME_SPECIFIC_15 = 31,
        [EditorLabel("Invalid")]
        E_STATE_INVALID = 32,
        [EditorHidden]
        E_STATE_COUNT = 32,
    }
}
