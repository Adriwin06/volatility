using System.Numerics;

using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.PropInstanceData)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class PropInstanceData : Resource
{
    private const int HeaderSize = 0x20;
    private const int InstanceSize = 0x50;
    private const int CellSize = 0xC;
    private const int SectionAlignment = 0x10;

    public uint MapCellsOffset;
    public uint NumCells;
    public uint PropInstanceDataOffset;
    public uint DataLength;
    public uint NumberOfPropInstanceDataPlusPropParts;
    public uint NumberOfPropInstanceData;
    public uint ZoneNumber;
    public uint UnknownHeader;
    public List<PropInstance> Instances = [];
    public List<PropCellData> Cells = [];

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        Instances.Clear();
        Cells.Clear();

        MapCellsOffset = reader.ReadUInt32();
        NumCells = reader.ReadUInt32();
        PropInstanceDataOffset = reader.ReadUInt32();
        DataLength = reader.ReadUInt32();
        NumberOfPropInstanceDataPlusPropParts = reader.ReadUInt32();
        NumberOfPropInstanceData = reader.ReadUInt32();
        ZoneNumber = reader.ReadUInt32();
        UnknownHeader = reader.ReadUInt32();

        reader.ParseSection(PropInstanceDataOffset, (int)NumberOfPropInstanceData, ReadPropInstance, Instances);
        reader.ParseSection(MapCellsOffset, (int)NumCells, ReadPropCellData, Cells);
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        NumberOfPropInstanceData = (uint)Instances.Count;
        if (NumberOfPropInstanceDataPlusPropParts < NumberOfPropInstanceData)
        {
            NumberOfPropInstanceDataPlusPropParts = NumberOfPropInstanceData;
        }

        long currentOffset = HeaderSize;
        PropInstanceDataOffset = (uint)ResourceUtilities.GetSectionOffset(ref currentOffset, Instances.Count, InstanceSize, SectionAlignment);
        MapCellsOffset = (uint)ResourceUtilities.GetSectionOffset(ref currentOffset, Cells.Count, CellSize, SectionAlignment);
        NumCells = (uint)Cells.Count;
        DataLength = currentOffset > HeaderSize ? (uint)(currentOffset - HeaderSize) : 0;

        writer.Write(MapCellsOffset);
        writer.Write(NumCells);
        writer.Write(PropInstanceDataOffset);
        writer.Write(DataLength);
        writer.Write(NumberOfPropInstanceDataPlusPropParts);
        writer.Write(NumberOfPropInstanceData);
        writer.Write(ZoneNumber);
        writer.Write(UnknownHeader);

        writer.WriteSection(PropInstanceDataOffset, Instances, WritePropInstance);
        writer.WriteSection(MapCellsOffset, Cells, WritePropCellData);
    }

    private static PropInstance ReadPropInstance(ResourceBinaryReader reader)
    {
        return new PropInstance
        {
            WorldTransform = MatrixUtilities.ReadMatrix44(reader),
            TypeId = reader.ReadUInt16(),
            Unknown0 = reader.ReadByte(),
            Flags = reader.ReadByte(),
            InstanceId = reader.ReadUInt32(),
            AlternativeType = reader.ReadUInt16(),
            RotSpeed = reader.ReadSByte(),
            MaxAngle = reader.ReadByte(),
            MinAngle = reader.ReadByte(),
            Padding = reader.ReadBytes(3),
        };
    }

    private static void WritePropInstance(ResourceBinaryWriter writer, PropInstance value)
    {
        MatrixUtilities.WriteMatrix44(writer, value.WorldTransform);
        writer.Write(value.TypeId);
        writer.Write(value.Unknown0);
        writer.Write(value.Flags);
        writer.Write(value.InstanceId);
        writer.Write(value.AlternativeType);
        writer.Write(value.RotSpeed);
        writer.Write(value.MaxAngle);
        writer.Write(value.MinAngle);
        writer.WriteFixedBytes(value.Padding, 3);
    }

    private static PropCellData ReadPropCellData(ResourceBinaryReader reader)
    {
        return new PropCellData
        {
            First = reader.ReadUInt16(),
            Count = reader.ReadUInt16(),
            RunningValue = reader.ReadUInt16(),
            Unknown0 = reader.ReadUInt16(),
            Unknown1 = reader.ReadUInt16(),
            Unknown2 = reader.ReadUInt16(),
        };
    }

    private static void WritePropCellData(ResourceBinaryWriter writer, PropCellData value)
    {
        writer.Write(value.First);
        writer.Write(value.Count);
        writer.Write(value.RunningValue);
        writer.Write(value.Unknown0);
        writer.Write(value.Unknown1);
        writer.Write(value.Unknown2);
    }

    public PropInstanceData() : base() { }
    public PropInstanceData(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public struct PropInstance
{
    public Matrix4x4 WorldTransform;
    public ushort TypeId;
    public byte Unknown0;
    public byte Flags;
    public uint InstanceId;
    public ushort AlternativeType;
    public sbyte RotSpeed;
    public byte MaxAngle;
    public byte MinAngle;
    public byte[] Padding;
}

public struct PropCellData
{
    public ushort First;
    public ushort Count;
    public ushort RunningValue;
    public ushort Unknown0;
    public ushort Unknown1;
    public ushort Unknown2;
}
