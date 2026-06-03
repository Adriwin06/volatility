using System.Text;

using Volatility.Utilities;

namespace Volatility.Resources;

public interface IRawShaderPayload
{
    bool HasRawShaderPayload { get; }
}

public abstract class ShaderRawBase : ShaderBase, ISidecarPayloadResource, IRawShaderPayload
{
    public string? PayloadPath { get; set; }
    public string Description { get; set; } = string.Empty;
    public byte MaterialStateCount { get; set; }
    public uint VertexConstantInstanceCount { get; set; }
    public uint PixelConstantInstanceCount { get; set; }
    public List<ShaderSamplerBinding> Samplers { get; set; } = [];

    private byte[]? payloadData;

    public string PayloadSuffix => ".ShaderPayload";
    public bool HasRawShaderPayload => payloadData is { Length: > 0 } || !string.IsNullOrWhiteSpace(PayloadPath);

    public byte[] GetPayloadBytes()
    {
        if (payloadData != null)
        {
            return payloadData;
        }

        return ResourceSidecarUtilities.ReadSidecarBytes(this, PayloadPath);
    }

    public void SetPayloadPath(string path)
    {
        PayloadPath = path;
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness)
    {
        base.ParseFromStream(reader, endianness);

        long originalPosition = reader.BaseStream.Position;
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        payloadData = reader.ReadBytes((int)Math.Min(int.MaxValue, reader.BaseStream.Length));
        reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);

        Samplers.Clear();
        if (reader.Endianness == Endian.BE)
        {
            ParseBigEndianShader(reader);
        }
        else
        {
            ParseLittleEndianShader(reader);
        }
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness)
    {
        base.WriteToStream(writer, endianness);
        byte[] payload = GetPayloadBytes();
        if (payload.Length > 0)
        {
            writer.Write(payload);
        }
    }

    private void ParseLittleEndianShader(ResourceBinaryReader reader)
    {
        if (reader.BaseStream.Length < 0x70)
        {
            return;
        }

        reader.BaseStream.Seek(0x4, SeekOrigin.Begin);
        MaterialStateCount = reader.ReadByte();

        reader.BaseStream.Seek(0x8, SeekOrigin.Begin);
        int descriptionOffset = reader.ReadInt32();
        reader.BaseStream.Seek(0x24, SeekOrigin.Begin);
        int decompiledShaderOffset = reader.ReadInt32();
        int constantOffset = decompiledShaderOffset == 0 ? 0 : 4;

        Description = ReadNullTerminatedString(reader, descriptionOffset, decompiledShaderOffset > descriptionOffset ? decompiledShaderOffset : (int)reader.BaseStream.Length);

        reader.BaseStream.Seek(0x50 + constantOffset, SeekOrigin.Begin);
        VertexConstantInstanceCount = reader.ReadByte();
        reader.BaseStream.Seek(0x53 + constantOffset, SeekOrigin.Begin);
        PixelConstantInstanceCount = reader.ReadByte();

        reader.BaseStream.Seek(0x5C + constantOffset, SeekOrigin.Begin);
        int samplerOffset = reader.ReadInt32();
        int samplerCount = reader.ReadByte();
        reader.BaseStream.Seek(0x68 + constantOffset, SeekOrigin.Begin);
        int samplerNamesEndOffset = reader.ReadInt32();

        ParseSamplerTable(reader, samplerOffset, samplerCount, samplerNamesEndOffset, false);
    }

    private void ParseBigEndianShader(ResourceBinaryReader reader)
    {
        if (reader.BaseStream.Length < 0x98)
        {
            return;
        }

        reader.BaseStream.Seek(0x94, SeekOrigin.Begin);
        int descriptionOffset = reader.ReadInt32();
        Description = ReadNullTerminatedString(reader, descriptionOffset, (int)reader.BaseStream.Length);

        reader.BaseStream.Seek(0x8, SeekOrigin.Begin);
        VertexConstantInstanceCount = reader.ReadUInt32();
        reader.BaseStream.Seek(0x3C, SeekOrigin.Begin);
        PixelConstantInstanceCount = reader.ReadUInt32();

        reader.BaseStream.Seek(0x8C, SeekOrigin.Begin);
        int samplerOffset = reader.ReadInt32();
        int samplerCount = reader.ReadSByte();

        ParseSamplerTable(reader, samplerOffset, samplerCount, descriptionOffset, true);
    }

    private void ParseSamplerTable(ResourceBinaryReader reader, int samplerOffset, int samplerCount, int samplerNamesEndOffset, bool channelIsShort)
    {
        if (samplerOffset <= 0 || samplerCount <= 0 || samplerOffset >= reader.BaseStream.Length)
        {
            return;
        }

        List<int> nameOffsets = [];
        List<int> channels = [];
        for (int i = 0; i < samplerCount; i++)
        {
            long entryOffset = samplerOffset + (i * 0x8);
            if (entryOffset + 0x8 > reader.BaseStream.Length)
            {
                break;
            }

            reader.BaseStream.Seek(entryOffset, SeekOrigin.Begin);
            nameOffsets.Add(reader.ReadInt32());
            channels.Add(channelIsShort ? reader.ReadInt16() : reader.ReadByte());
        }

        for (int i = 0; i < nameOffsets.Count; i++)
        {
            int nextOffset = i + 1 < nameOffsets.Count ? nameOffsets[i + 1] : samplerNamesEndOffset;
            Samplers.Add(new ShaderSamplerBinding
            {
                Channel = channels[i],
                Name = ReadNullTerminatedString(reader, nameOffsets[i], nextOffset),
            });
        }
    }

    private static string ReadNullTerminatedString(ResourceBinaryReader reader, int startOffset, int endOffset)
    {
        if (startOffset <= 0 || startOffset >= reader.BaseStream.Length)
        {
            return string.Empty;
        }

        endOffset = Math.Clamp(endOffset, startOffset, (int)reader.BaseStream.Length);
        int length = endOffset - startOffset;
        if (length <= 0)
        {
            return string.Empty;
        }

        long originalPosition = reader.BaseStream.Position;
        reader.BaseStream.Seek(startOffset, SeekOrigin.Begin);
        byte[] data = reader.ReadBytes(length);
        reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
        int terminator = Array.IndexOf(data, (byte)0);
        if (terminator >= 0)
        {
            length = terminator;
        }

        return length == 0 ? string.Empty : Encoding.ASCII.GetString(data, 0, length);
    }

    protected ShaderRawBase() : base() { }
    protected ShaderRawBase(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

[ResourceRegistration(RegistrationPlatforms.BPR)]
public sealed class ShaderBPR : ShaderRawBase
{
    public override Endian ResourceEndian => Endian.LE;
    public override Platform ResourcePlatform => Platform.BPR;

    public ShaderBPR() : base() { }
    public ShaderBPR(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

[ResourceRegistration(RegistrationPlatforms.X360)]
public sealed class ShaderX360 : ShaderRawBase
{
    public override Endian ResourceEndian => Endian.BE;
    public override Platform ResourcePlatform => Platform.X360;

    public ShaderX360() : base() { }
    public ShaderX360(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

[ResourceRegistration(RegistrationPlatforms.PS3)]
public sealed class ShaderPS3 : ShaderRawBase
{
    public override Endian ResourceEndian => Endian.BE;
    public override Platform ResourcePlatform => Platform.PS3;

    public ShaderPS3() : base() { }
    public ShaderPS3(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public sealed class ShaderSamplerBinding
{
    public int Channel { get; set; }
    public string Name { get; set; } = string.Empty;
}
