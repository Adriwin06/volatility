using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.MaterialTechnique)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class MaterialTechnique : SidecarBackedResource
{
    public List<ExternalResourceImport> ExternalImports = [];
    public ResourceImport ShaderReference;
    public ResourceImport MaterialStateReference;

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        ExternalImports.Clear();
        long bodyLength = ResourceImportTableUtilities.GetBodyLengthWithoutTrailingImports(reader, 2);
        CaptureBody(reader, bodyLength);

        List<KeyValuePair<long, ResourceImport>> imports = ResourceImportTableUtilities.ReadTrailingImports(reader, 2);
        if (imports.Count == 0)
        {
            ResourceImport.ReadExternalImport(0, reader, bodyLength, out ShaderReference);
            ResourceImport.ReadExternalImport(1, reader, bodyLength, out MaterialStateReference);
            AddImport(0, ShaderReference, "Shader");
            AddImport(0, MaterialStateReference, "MaterialState");
            return;
        }

        ShaderReference = imports[0].Value;
        MaterialStateReference = imports.Count > 1 ? imports[1].Value : default;
        AddImport(imports[0].Key, ShaderReference, "Shader");
        if (imports.Count > 1)
        {
            AddImport(imports[1].Key, MaterialStateReference, "MaterialState");
        }
    }

    public override IEnumerable<KeyValuePair<long, ResourceImport>> GetExternalImports()
    {
        foreach (ExternalResourceImport entry in ExternalImports)
        {
            if (entry.ResourceReference.ExternalImport)
            {
                yield return new KeyValuePair<long, ResourceImport>(entry.Offset, entry.ResourceReference);
            }
        }
    }

    private void AddImport(long offset, ResourceImport reference, string role)
    {
        if (!reference.ExternalImport)
        {
            return;
        }

        ExternalImports.Add(new ExternalResourceImport
        {
            Offset = offset,
            ResourceReference = reference,
            Role = role,
        });
    }

    public MaterialTechnique() : base() { }
    public MaterialTechnique(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
