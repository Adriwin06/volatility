using Volatility.Utilities;

namespace Volatility.Resources;

public interface ISidecarPayloadResource
{
    string PayloadSuffix { get; }
    byte[] GetPayloadBytes();
    void SetPayloadPath(string path);
}

public abstract class OpaqueResource : Resource, ISidecarPayloadResource
{
    [EditorCategory("Import Data"), EditorLabel("Payload Sidecar"), EditorTooltip("Relative path to the exact binary payload for resources that Volatility preserves byte-for-byte.")]
    public string? PayloadPath { get; set; }

    private byte[]? payloadData;

    public virtual string PayloadSuffix => $".{ResourceType}Payload";

    public byte[] GetPayloadBytes()
    {
        if (payloadData != null)
        {
            return payloadData;
        }

        return ResourceSidecarUtilities.ReadSidecarBytes(this, PayloadPath);
    }

    public void SetPayloadBytes(byte[] data)
    {
        payloadData = data;
    }

    public void SetPayloadPath(string path)
    {
        PayloadPath = path;
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        long originalPosition = reader.BaseStream.Position;
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        if (reader.BaseStream.Length > int.MaxValue)
        {
            throw new InvalidDataException($"Opaque resource '{AssetName}' is too large to import as a sidecar payload.");
        }

        payloadData = reader.ReadBytes((int)reader.BaseStream.Length);
        reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);
        writer.Write(GetPayloadBytes());
    }

    protected OpaqueResource() : base() { }

    protected OpaqueResource(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

[ResourceDefinition(ResourceType.MaterialState)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public sealed class MaterialState : OpaqueResource
{
    public MaterialState() : base() { }
    public MaterialState(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

[ResourceDefinition(ResourceType.ClusteredMesh)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public sealed class ClusteredMesh : OpaqueResource
{
    public ClusteredMesh() : base() { }
    public ClusteredMesh(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
