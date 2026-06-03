using System.Numerics;

using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.StaticSoundMap)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class StaticSoundMap : Resource
{
    private const int HeaderSize = 0x40;
    private const int EntitySize = 0x10;
    private const int SubRegionSize = 0x4;

    public Vector2 Min;
    public Vector2 Max;
    public float SubRegionWorldSize;
    public uint SubRegionsOffset = HeaderSize;
    public int NumSubRegionsX;
    public int NumSubRegionsZ;
    public uint EntitiesOffset = HeaderSize;
    public int RootType;
    public List<StaticSoundEntity> Entities = [];
    public List<StaticSoundSubRegion> SubRegions = [];

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        Entities.Clear();
        SubRegions.Clear();

        Min = reader.ReadVector2Literal();
        reader.BaseStream.Seek(0x8, SeekOrigin.Current);
        Max = reader.ReadVector2Literal();
        reader.BaseStream.Seek(0x8, SeekOrigin.Current);
        SubRegionWorldSize = reader.ReadSingle();
        SubRegionsOffset = reader.ReadUInt32();
        NumSubRegionsX = reader.ReadInt32();
        NumSubRegionsZ = reader.ReadInt32();
        EntitiesOffset = reader.ReadUInt32();
        int entityCount = reader.ReadInt32();
        RootType = reader.ReadInt32();

        reader.ParseSection(EntitiesOffset, entityCount, ReadStaticSoundEntity, Entities);
        reader.ParseSection(SubRegionsOffset, NumSubRegionsX * NumSubRegionsZ, ReadStaticSoundSubRegion, SubRegions);
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        EntitiesOffset = HeaderSize;
        SubRegionsOffset = (uint)(EntitiesOffset + (Entities.Count * EntitySize));

        writer.Write(Min.X);
        writer.Write(Min.Y);
        writer.WriteFixedBytes(null, 0x8);
        writer.Write(Max.X);
        writer.Write(Max.Y);
        writer.WriteFixedBytes(null, 0x8);
        writer.Write(SubRegionWorldSize);
        writer.Write(SubRegionsOffset);
        writer.Write(NumSubRegionsX);
        writer.Write(NumSubRegionsZ);
        writer.Write(EntitiesOffset);
        writer.Write(Entities.Count);
        writer.Write(RootType);
        writer.WriteFixedBytes(null, HeaderSize - (int)writer.BaseStream.Position);

        writer.WriteSection(EntitiesOffset, Entities, WriteStaticSoundEntity);
        writer.WriteSection(SubRegionsOffset, SubRegions, WriteStaticSoundSubRegion);
    }

    private static StaticSoundEntity ReadStaticSoundEntity(ResourceBinaryReader reader)
    {
        return new StaticSoundEntity
        {
            Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
            Unknown0 = reader.ReadUInt16(),
            Unknown1 = reader.ReadUInt16(),
        };
    }

    private static void WriteStaticSoundEntity(ResourceBinaryWriter writer, StaticSoundEntity value)
    {
        writer.Write(value.Position.X);
        writer.Write(value.Position.Y);
        writer.Write(value.Position.Z);
        writer.Write(value.Unknown0);
        writer.Write(value.Unknown1);
    }

    private static StaticSoundSubRegion ReadStaticSoundSubRegion(ResourceBinaryReader reader)
    {
        return new StaticSoundSubRegion
        {
            First = reader.ReadInt16(),
            Count = reader.ReadInt16(),
        };
    }

    private static void WriteStaticSoundSubRegion(ResourceBinaryWriter writer, StaticSoundSubRegion value)
    {
        writer.Write(value.First);
        writer.Write(value.Count);
    }

    public StaticSoundMap() : base() { }
    public StaticSoundMap(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public struct StaticSoundEntity
{
    public Vector3 Position;
    public ushort Unknown0;
    public ushort Unknown1;
}

public struct StaticSoundSubRegion
{
    public short First;
    public short Count;
}
