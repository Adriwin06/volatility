using Volatility.Utilities;

namespace Volatility.Resources;

public abstract class SidecarBackedResource : Resource, ISidecarPayloadResource
{
    [EditorCategory("Import Data"), EditorLabel("Payload Sidecar"), EditorTooltip("Relative path to the preserved primary resource body.")]
    public string? PayloadPath { get; set; }

    private byte[]? payloadData;

    public virtual string PayloadSuffix => $".{ResourceType}Body";

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

    protected void SetPayloadBytes(byte[] data)
    {
        payloadData = data;
    }

    protected byte[] ReadWholeStream(ResourceBinaryReader reader)
    {
        long originalPosition = reader.BaseStream.Position;
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        if (reader.BaseStream.Length > int.MaxValue)
        {
            throw new InvalidDataException($"Resource '{AssetName}' is too large to import as a sidecar payload.");
        }

        byte[] data = reader.ReadBytes((int)reader.BaseStream.Length);
        reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
        return data;
    }

    protected void CaptureBody(ResourceBinaryReader reader, long bodyLength)
    {
        byte[] data = ReadWholeStream(reader);
        bodyLength = Math.Clamp(bodyLength, 0, data.Length);
        byte[] body = new byte[bodyLength];
        Array.Copy(data, body, body.Length);
        SetPayloadBytes(body);
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);
        writer.Write(GetPayloadBytes());
    }

    protected SidecarBackedResource() : base() { }

    protected SidecarBackedResource(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
