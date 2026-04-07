using System.Numerics;

namespace Volatility.Resources;

// The StreamedDeformationSpec resource type defines per-vehicle
// deformation data such as tag points, IK body parts, wheels, sensors,
// locator tags, and glass panes.
//
// Learn More:
// https://burnout.wiki/wiki/Streamed_Deformation

public struct DeformationVector3Plus
{
    public Vector3 Vector;
    public float Extra;

    public DeformationVector3Plus(ResourceBinaryReader reader)
    {
        Vector = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        Extra = reader.ReadSingle();
    }
}

public struct WheelSpec
{
    public const int Size = 0x30;

    public Vector3 Position;
    public Vector3 Scale;
    public int TagPointIndex;

    public WheelSpec(ResourceBinaryReader reader)
    {
        Position = reader.ReadVector3();
        Scale = reader.ReadVector3();
        TagPointIndex = reader.ReadInt32();
        reader.BaseStream.Seek(0xC, SeekOrigin.Current);
    }
}

public struct SensorSpec
{
    public const int Size = 0x40;

    public Vector3 InitialOffset;
    public float[] DirectionParams;
    public float Radius;
    public byte[] NextSensor;
    public byte SceneIndex;
    public byte AbsorbtionLevel;
    public byte[] NextBoundarySensor;

    public SensorSpec(ResourceBinaryReader reader)
    {
        InitialOffset = reader.ReadVector3();

        DirectionParams = new float[6];
        for (int i = 0; i < DirectionParams.Length; i++)
        {
            DirectionParams[i] = reader.ReadSingle();
        }

        Radius = reader.ReadSingle();
        NextSensor = reader.ReadBytes(6);
        SceneIndex = reader.ReadByte();
        AbsorbtionLevel = reader.ReadByte();
        NextBoundarySensor = reader.ReadBytes(2);
        reader.BaseStream.Seek(0xA, SeekOrigin.Current);
    }
}

public struct TagPointSpec
{
    public const int Size = 0x50;

    public DeformationVector3Plus OffsetFromAAndWeightA;
    public DeformationVector3Plus OffsetFromBAndWeightB;
    public DeformationVector3Plus InitialPositionAndDetachThreshold;
    public float WeightA;
    public float WeightB;
    public float DetachThresholdSquared;
    public short DeformationSensorA;
    public short DeformationSensorB;
    public sbyte JointIndex;
    public bool SkinnedPoint;

    public TagPointSpec(ResourceBinaryReader reader)
    {
        OffsetFromAAndWeightA = new DeformationVector3Plus(reader);
        OffsetFromBAndWeightB = new DeformationVector3Plus(reader);
        InitialPositionAndDetachThreshold = new DeformationVector3Plus(reader);
        WeightA = reader.ReadSingle();
        WeightB = reader.ReadSingle();
        DetachThresholdSquared = reader.ReadSingle();
        DeformationSensorA = reader.ReadInt16();
        DeformationSensorB = reader.ReadInt16();
        JointIndex = reader.ReadSByte();
        SkinnedPoint = reader.ReadBoolean();
        reader.BaseStream.Seek(0xE, SeekOrigin.Current);
    }
}

public struct IKDrivenPointSpec
{
    public const int Size = 0x20;

    public Vector3 InitialPos;
    public float DistanceFromA;
    public float DistanceFromB;
    public short TagPointIndexA;
    public short TagPointIndexB;

    public IKDrivenPointSpec(ResourceBinaryReader reader)
    {
        InitialPos = reader.ReadVector3();
        DistanceFromA = reader.ReadSingle();
        DistanceFromB = reader.ReadSingle();
        TagPointIndexA = reader.ReadInt16();
        TagPointIndexB = reader.ReadInt16();
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);
    }
}

public struct LocatorPointSpec
{
    public const int Size = 0x50;

    public Matrix4x4 LocatorMatrix;
    public int TagPointType;
    public short IkPartIndex;
    public byte SkinPoint;

    public LocatorPointSpec(ResourceBinaryReader reader)
    {
        LocatorMatrix = reader.ReadMatrix4x4();
        TagPointType = reader.ReadInt32();
        IkPartIndex = reader.ReadInt16();
        SkinPoint = reader.ReadByte();
        reader.BaseStream.Seek(0x9, SeekOrigin.Current);
    }
}

public struct LocatorPointSpecList
{
    public uint Count;
    public ulong Offset;

    public LocatorPointSpecList(ResourceBinaryReader reader, Arch arch)
    {
        Count = reader.ReadUInt32();
        if (arch == Arch.x64)
        {
            reader.BaseStream.Seek(0x4, SeekOrigin.Current);
            Offset = reader.ReadUInt64();
        }
        else
        {
            Offset = reader.ReadUInt32();
        }
    }
}

public struct BBoxPointSkinData
{
    public const int Size = 0x20;

    public Vector3 Vertex;
    public float[] Weights;
    public byte[] BoneIndices;

    public BBoxPointSkinData(ResourceBinaryReader reader)
    {
        Vertex = reader.ReadVector3();
        Weights =
        [
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle()
        ];
        BoneIndices = reader.ReadBytes(3);
        reader.BaseStream.Seek(0x1, SeekOrigin.Current);
    }
}

public struct BodyPartBBoxSpec
{
    public const int Size = 0x180;

    public Matrix4x4 Orientation;
    public List<BBoxPointSkinData> CornerSkinData;
    public BBoxPointSkinData CentreSkinData;
    public BBoxPointSkinData JointSkinData;

    public BodyPartBBoxSpec(ResourceBinaryReader reader)
    {
        Orientation = reader.ReadMatrix4x4();

        CornerSkinData = new List<BBoxPointSkinData>(8);
        for (int i = 0; i < 8; i++)
        {
            CornerSkinData.Add(new BBoxPointSkinData(reader));
        }

        CentreSkinData = new BBoxPointSkinData(reader);
        JointSkinData = new BBoxPointSkinData(reader);
    }
}

public struct DeformationJointSpec
{
    public const int Size = 0x40;

    public Vector3 JointPosition;
    public Vector3 JointAxis;
    public Vector3 JointDefaultDirection;
    public float MaxJointAngle;
    public float JointDetachThreshold;
    public int JointType;

    public DeformationJointSpec(ResourceBinaryReader reader)
    {
        JointPosition = reader.ReadVector3();
        JointAxis = reader.ReadVector3();
        JointDefaultDirection = reader.ReadVector3();
        MaxJointAngle = reader.ReadSingle();
        JointDetachThreshold = reader.ReadSingle();
        JointType = reader.ReadInt32();
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);
    }
}

public struct IKBodyPartSpec
{
    public static int GetSize(Arch arch) => arch == Arch.x64 ? 0x1E4 : 0x1E0;

    public Matrix4x4 GraphicsTransform;
    public BodyPartBBoxSpec BBoxSkinData;
    public ulong JointSpecsOffset;
    public int NumJoints;
    public int PartGraphics;
    public int StartIndexOfDrivenPoints;
    public int NumberOfDrivenPoints;
    public int StartIndexOfTagPoints;
    public int NumberOfTagPoints;
    public int PartType;
    public List<DeformationJointSpec> JointSpecs;

    public IKBodyPartSpec(ResourceBinaryReader reader, Arch arch)
    {
        long structStart = reader.BaseStream.Position;

        GraphicsTransform = reader.ReadMatrix4x4();
        BBoxSkinData = new BodyPartBBoxSpec(reader);
        JointSpecsOffset = StreamedDeformationSpec.ReadPointer(reader, arch);
        NumJoints = reader.ReadInt32();
        PartGraphics = reader.ReadInt32();
        StartIndexOfDrivenPoints = reader.ReadInt32();
        NumberOfDrivenPoints = reader.ReadInt32();
        StartIndexOfTagPoints = reader.ReadInt32();
        NumberOfTagPoints = reader.ReadInt32();
        PartType = reader.ReadInt32();

        JointSpecs = new List<DeformationJointSpec>(Math.Max(NumJoints, 0));

        long structEnd = structStart + GetSize(arch);
        long originalPosition = reader.BaseStream.Position;

        if (NumJoints > 0 && JointSpecsOffset != 0)
        {
            reader.BaseStream.Seek((long)JointSpecsOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumJoints; i++)
            {
                JointSpecs.Add(new DeformationJointSpec(reader));
            }
        }

        reader.BaseStream.Seek(Math.Max(structEnd, originalPosition), SeekOrigin.Begin);
    }
}

public struct GlassPaneSpec
{
    public const int Size = 0x70;

    public Vector3 Normal;
    public Vector3[] CornerPositionOffsets;
    public short[] PointIndices;
    public bool[] SkinToControlPoint;
    public short ParentBodyPart;
    public short CrackSensor;
    public short SmashSensor;
    public int PartType;

    public GlassPaneSpec(ResourceBinaryReader reader)
    {
        Normal = reader.ReadVector3();

        CornerPositionOffsets = new Vector3[4];
        for (int i = 0; i < CornerPositionOffsets.Length; i++)
        {
            CornerPositionOffsets[i] = reader.ReadVector3();
        }

        PointIndices = new short[4];
        for (int i = 0; i < PointIndices.Length; i++)
        {
            PointIndices[i] = reader.ReadInt16();
        }

        SkinToControlPoint = new bool[4];
        for (int i = 0; i < SkinToControlPoint.Length; i++)
        {
            SkinToControlPoint[i] = reader.ReadByte() != 0;
        }

        ParentBodyPart = reader.ReadInt16();
        CrackSensor = reader.ReadInt16();
        SmashSensor = reader.ReadInt16();
        reader.BaseStream.Seek(0x2, SeekOrigin.Current);
        PartType = reader.ReadInt32();
        reader.BaseStream.Seek(0x8, SeekOrigin.Current);
    }
}

public class StreamedDeformationSpec : Resource
{
    public const int HeaderSize32 = 0x6B0;
    public const int HeaderSize64 = 0x6F0;

    public override ResourceType GetResourceType() => ResourceType.StreamedDeformationSpec;

    public int VersionNumber { get; set; }
    public ulong TagPointDataOffset { get; set; }
    public int NumberOfTagPoints { get; set; }
    public ulong DrivenPointDataOffset { get; set; }
    public int NumberOfDrivenPoints { get; set; }
    public ulong IKPartDataOffset { get; set; }
    public int NumberOfIKParts { get; set; }
    public ulong GlassPaneDataOffset { get; set; }
    public int NumGlassPanes { get; set; }
    public LocatorPointSpecList GenericTagsInfo { get; set; }
    public LocatorPointSpecList CameraTagsInfo { get; set; }
    public LocatorPointSpecList LightTagsInfo { get; set; }
    public Vector3 HandlingBodyDimensions { get; set; }
    public List<WheelSpec> WheelSpecs { get; set; } = new();
    public List<SensorSpec> DeformationSensorSpecs { get; set; } = new();
    public Matrix4x4 CarModelSpaceToHandlingBodySpaceTransform { get; set; }
    public byte SpecID { get; set; }
    public byte NumVehicleBodies { get; set; }
    public byte NumDeformationSensors { get; set; }
    public byte NumGraphicsParts { get; set; }
    public Vector3 CurrentCOMOffset { get; set; }
    public Vector3 MeshOffset { get; set; }
    public Vector3 RigidBodyOffset { get; set; }
    public Vector3 CollisionOffset { get; set; }
    public Vector3 InertiaTensor { get; set; }
    public List<TagPointSpec> TagPointSpecs { get; set; } = new();
    public List<IKDrivenPointSpec> DrivenPoints { get; set; } = new();
    public List<LocatorPointSpec> GenericTags { get; set; } = new();
    public List<LocatorPointSpec> CameraTags { get; set; } = new();
    public List<LocatorPointSpec> LightTags { get; set; } = new();
    public List<IKBodyPartSpec> IKParts { get; set; } = new();
    public List<GlassPaneSpec> GlassPanes { get; set; } = new();

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        WheelSpecs.Clear();
        DeformationSensorSpecs.Clear();
        TagPointSpecs.Clear();
        DrivenPoints.Clear();
        GenericTags.Clear();
        CameraTags.Clear();
        LightTags.Clear();
        IKParts.Clear();
        GlassPanes.Clear();

        Arch arch = GetResourceArch();

        VersionNumber = reader.ReadInt32();
        if (VersionNumber != 1)
        {
            throw new InvalidDataException($"Version mismatch! Version should be 1. (Found version {VersionNumber})");
        }

        if (arch == Arch.x64)
        {
            reader.BaseStream.Seek(0x4, SeekOrigin.Current);
        }

        TagPointDataOffset = ReadPointer(reader, arch);
        NumberOfTagPoints = ReadCount(reader, arch);
        DrivenPointDataOffset = ReadPointer(reader, arch);
        NumberOfDrivenPoints = ReadCount(reader, arch);
        IKPartDataOffset = ReadPointer(reader, arch);
        NumberOfIKParts = ReadCount(reader, arch);
        GlassPaneDataOffset = ReadPointer(reader, arch);
        NumGlassPanes = ReadCount(reader, arch);
        GenericTagsInfo = new LocatorPointSpecList(reader, arch);
        CameraTagsInfo = new LocatorPointSpecList(reader, arch);
        LightTagsInfo = new LocatorPointSpecList(reader, arch);

        reader.BaseStream.Seek(arch == Arch.x64 ? 0x8 : 0x4, SeekOrigin.Current);

        HandlingBodyDimensions = reader.ReadVector3();

        for (int i = 0; i < 4; i++)
        {
            WheelSpecs.Add(new WheelSpec(reader));
        }

        for (int i = 0; i < 20; i++)
        {
            DeformationSensorSpecs.Add(new SensorSpec(reader));
        }

        CarModelSpaceToHandlingBodySpaceTransform = reader.ReadMatrix4x4();

        SpecID = reader.ReadByte();
        NumVehicleBodies = reader.ReadByte();
        NumDeformationSensors = reader.ReadByte();
        NumGraphicsParts = reader.ReadByte();
        reader.BaseStream.Seek(0xC, SeekOrigin.Current);

        CurrentCOMOffset = reader.ReadVector3();
        MeshOffset = reader.ReadVector3();
        RigidBodyOffset = reader.ReadVector3();
        CollisionOffset = reader.ReadVector3();
        InertiaTensor = reader.ReadVector3();

        ParseSection(reader, TagPointDataOffset, NumberOfTagPoints, r => new TagPointSpec(r), TagPointSpecs);
        ParseSection(reader, DrivenPointDataOffset, NumberOfDrivenPoints, r => new IKDrivenPointSpec(r), DrivenPoints);
        ParseSection(reader, GenericTagsInfo.Offset, (int)GenericTagsInfo.Count, r => new LocatorPointSpec(r), GenericTags);
        ParseSection(reader, CameraTagsInfo.Offset, (int)CameraTagsInfo.Count, r => new LocatorPointSpec(r), CameraTags);
        ParseSection(reader, LightTagsInfo.Offset, (int)LightTagsInfo.Count, r => new LocatorPointSpec(r), LightTags);
        ParseSection(reader, GlassPaneDataOffset, NumGlassPanes, r => new GlassPaneSpec(r), GlassPanes);
        ParseSection(reader, IKPartDataOffset, NumberOfIKParts, r => new IKBodyPartSpec(r, arch), IKParts);
    }

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);
        throw new NotImplementedException("Writing StreamedDeformationSpec is not implemented.");
    }

    public StreamedDeformationSpec() : base() { }

    public StreamedDeformationSpec(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

    internal static ulong ReadPointer(ResourceBinaryReader reader, Arch arch)
    {
        return arch == Arch.x64 ? reader.ReadUInt64() : reader.ReadUInt32();
    }

    private static int ReadCount(ResourceBinaryReader reader, Arch arch)
    {
        int count = reader.ReadInt32();
        if (arch == Arch.x64)
        {
            reader.BaseStream.Seek(0x4, SeekOrigin.Current);
        }

        return count;
    }

    private static void ParseSection<T>(ResourceBinaryReader reader, ulong offset, int count, Func<ResourceBinaryReader, T> parser, List<T> destination)
    {
        if (count <= 0 || offset == 0)
        {
            return;
        }

        long originalPosition = reader.BaseStream.Position;
        reader.BaseStream.Seek((long)offset, SeekOrigin.Begin);

        for (int i = 0; i < count; i++)
        {
            destination.Add(parser(reader));
        }

        reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
    }
}

public static class ResourceBinaryReaderExtensions
{
    public static Matrix4x4 ReadMatrix4x4(this ResourceBinaryReader reader)
    {
        return new Matrix4x4(
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
        );
    }
}
