namespace Volatility.Resources;

[ResourceDefinition(ResourceType.RwShaderProgramBuffer)]
public class ShaderProgramBufferBase : Resource
{
    public ShaderProgramBufferBase() : base() { }

    public ShaderProgramBufferBase(string path)
        : base(path) { }
}
