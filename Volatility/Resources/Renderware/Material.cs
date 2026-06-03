using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.Material)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class Material : SidecarBackedResource
{
    public int MaterialStateTableOffset;
    public ResourceID MaterialId;
    public byte MaterialStateCount;
    public byte TextureStateCount;
    public byte UnknownTextureStateCount;
    public byte UnknownFlags;
    public int TextureStateTableOffset;
    public int UnknownHeaderPointer;
    public int VertexShaderConstantsOffset;
    public int PixelShaderConstantsOffset;
    public int AnimationConstantsOffset;
    public int UnknownHeaderPointer2;
    public bool UsesMaterialTechniques;

    public ResourceImport ShaderReference;
    public List<ResourceImport> MaterialStateReferences = [];
    public List<ResourceImport> MaterialTechniqueReferences = [];
    public List<ResourceImport> TextureStateReferences = [];
    public List<ExternalResourceImport> ExternalImports = [];

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        MaterialStateReferences.Clear();
        MaterialTechniqueReferences.Clear();
        TextureStateReferences.Clear();
        ExternalImports.Clear();

        ParseHeader(reader);
        UsesMaterialTechniques = reader.Endianness == Endian.BE;

        int importCount = reader.Endianness == Endian.BE
            ? MaterialStateCount + TextureStateCount
            : 1 + MaterialStateCount + TextureStateCount;

        long bodyLength = ResourceImportTableUtilities.GetBodyLengthWithoutTrailingImports(reader, importCount);
        CaptureBody(reader, bodyLength);

        List<KeyValuePair<long, ResourceImport>> imports = ResourceImportTableUtilities.ReadTrailingImports(reader, importCount);
        if (imports.Count == importCount)
        {
            AssignImports(imports);
        }
        else
        {
            ReadImportsFromSidecarOrBody(reader, bodyLength);
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

    private void ParseHeader(ResourceBinaryReader reader)
    {
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        MaterialStateTableOffset = reader.ReadInt32();
        MaterialId = reader.ReadUInt32();
        MaterialStateCount = reader.ReadByte();
        TextureStateCount = reader.ReadByte();
        UnknownTextureStateCount = reader.ReadByte();
        UnknownFlags = reader.ReadByte();
        TextureStateTableOffset = reader.ReadInt32();

        if (reader.Endianness == Endian.BE)
        {
            VertexShaderConstantsOffset = reader.ReadInt32();
            PixelShaderConstantsOffset = reader.ReadInt32();
            AnimationConstantsOffset = reader.ReadInt32();
            return;
        }

        UnknownHeaderPointer = reader.ReadInt32();
        VertexShaderConstantsOffset = reader.ReadInt32();
        PixelShaderConstantsOffset = reader.ReadInt32();
        AnimationConstantsOffset = reader.ReadInt32();
        UnknownHeaderPointer2 = reader.ReadInt32();
    }

    private void AssignImports(List<KeyValuePair<long, ResourceImport>> imports)
    {
        int index = 0;
        if (imports.Count == 0)
        {
            return;
        }

        if (UsesMaterialTechniques)
        {
            for (int i = 0; i < MaterialStateCount && index < imports.Count; i++, index++)
            {
                MaterialTechniqueReferences.Add(imports[index].Value);
                AddImport(imports[index], "MaterialTechnique");
            }
        }
        else
        {
            ShaderReference = imports[index].Value;
            AddImport(imports[index], "Shader");
            index++;

            for (int i = 0; i < MaterialStateCount && index < imports.Count; i++, index++)
            {
                MaterialStateReferences.Add(imports[index].Value);
                AddImport(imports[index], "MaterialState");
            }
        }

        for (int i = 0; i < TextureStateCount && index < imports.Count; i++, index++)
        {
            TextureStateReferences.Add(imports[index].Value);
            AddImport(imports[index], "TextureState");
        }
    }

    private void ReadImportsFromSidecarOrBody(ResourceBinaryReader reader, long bodyLength)
    {
        if (reader.Endianness == Endian.BE)
        {
            for (int i = 0; i < MaterialStateCount; i++)
            {
                long key = MaterialStateTableOffset + (i * sizeof(uint));
                if (ResourceImport.ReadExternalImport(key, reader, bodyLength, out ResourceImport materialTechniqueReference))
                {
                    MaterialTechniqueReferences.Add(materialTechniqueReference);
                    AddImport(key, materialTechniqueReference, "MaterialTechnique");
                }
            }
        }
        else
        {
            if (ResourceImport.ReadExternalImport(0x10L, reader, bodyLength, out ShaderReference))
            {
                AddImport(0x10, ShaderReference, "Shader");
            }

            for (int i = 0; i < MaterialStateCount; i++)
            {
                long key = MaterialStateTableOffset + (i * 0x20);
                if (ResourceImport.ReadExternalImport(key, reader, bodyLength, out ResourceImport materialStateReference))
                {
                    MaterialStateReferences.Add(materialStateReference);
                    AddImport(key, materialStateReference, "MaterialState");
                }
            }
        }

        for (int i = 0; i < TextureStateCount; i++)
        {
            long key = TextureStateTableOffset + (i * 0x14) + 0x10;
            if (ResourceImport.ReadExternalImport(key, reader, bodyLength, out ResourceImport textureStateReference))
            {
                TextureStateReferences.Add(textureStateReference);
                AddImport(key, textureStateReference, "TextureState");
            }
        }
    }

    private void AddImport(KeyValuePair<long, ResourceImport> import, string role)
    {
        AddImport(import.Key, import.Value, role);
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

    public Material() : base() { }
    public Material(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
