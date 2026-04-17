namespace Volatility.Resources;

public class ShaderProgramBufferBase : TypedResource
{
    public ShaderProgramBufferBase() : base(ResourceType.RwShaderProgramBuffer) { }

    public ShaderProgramBufferBase(string path)
        : base(ResourceType.RwShaderProgramBuffer, path) { }
}
