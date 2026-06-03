namespace Volatility.Resources;

[ResourceDefinition(ResourceType.RwShaderProgramBuffer)]
[ResourceRegistration(RegistrationPlatforms.TUB | RegistrationPlatforms.X360 | RegistrationPlatforms.PS3, EndianMapped = true)]
public sealed class ShaderProgramBufferRaw : OpaqueResource
{
    public ShaderProgramBufferRaw() : base() { }
    public ShaderProgramBufferRaw(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
