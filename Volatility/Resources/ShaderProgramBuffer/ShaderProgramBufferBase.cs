namespace Volatility.Resources;

[ResourceDefinition(ResourceType.RwShaderProgramBuffer)]
public class ShaderProgramBufferBase : TypedResource
{
    public ShaderProgramBufferBase() : base() { }

    public ShaderProgramBufferBase(string path)
        : base(path) { }
}
