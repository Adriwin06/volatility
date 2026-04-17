namespace Volatility.Resources;

public static class ResourceFactory
{
    private static readonly Dictionary<(ResourceType, Platform), Func<string, Resource>> resourceCreators = CreateResourceCreators();

    private static Dictionary<(ResourceType, Platform), Func<string, Resource>> CreateResourceCreators()
    {
        ResourceCreatorRegistry registry = new();

        registry.AddWithPullAll(ResourceType.Texture, Platform.BPR, static path => new TextureBPR(path));
        registry.AddWithPullAll(ResourceType.Texture, Platform.TUB, static path => new TexturePC(path));
        registry.AddWithPullAll(ResourceType.Texture, Platform.X360, static path => new TextureX360(path));
        registry.AddWithPullAll(ResourceType.Texture, Platform.PS3, static path => new TexturePS3(path));

        registry.AddEndianMapped(ResourceType.Splicer, static (path, endian) => new Splicer(path, endian));
        registry.Add(ResourceType.Renderable, Platform.BPR, static path => new RenderableBPR(path));
        registry.Add(ResourceType.Renderable, Platform.TUB, static path => new RenderablePC(path));
        registry.Add(ResourceType.Renderable, Platform.X360, static path => new RenderableX360(path));
        registry.Add(ResourceType.Renderable, Platform.PS3, static path => new RenderablePS3(path));
        registry.AddEndianMapped(ResourceType.InstanceList, static (path, endian) => new InstanceList(path, endian));
        registry.AddEndianMapped(ResourceType.Model, static (path, endian) => new Model(path, endian));
        registry.AddEndianMapped(ResourceType.EnvironmentKeyframe, static (path, endian) => new EnvironmentKeyframe(path, endian));
        registry.AddEndianMapped(ResourceType.EnvironmentTimeLine, static (path, endian) => new EnvironmentTimeline(path, endian));
        registry.AddEndianMapped(ResourceType.SnapshotData, static (path, endian) => new SnapshotData(path, endian));
        registry.AddEndianMapped(ResourceType.AttribSysVault, static (path, endian) => new AttribSysVault(path, endian));
        registry.AddEndianMapped(ResourceType.StreamedDeformationSpec, static (path, endian) => new StreamedDeformationSpec(path, endian));
        registry.AddEndianMapped(ResourceType.AptData, static (path, endian) => new AptData(path, endian));
        registry.AddEndianMapped(ResourceType.GuiPopup, static (path, endian) => new GuiPopup(path, endian));

        registry.Add(ResourceType.Shader, Platform.Agnostic, static path => new ShaderBase(path));
        registry.Add(ResourceType.Shader, Platform.TUB, static path => new ShaderPC(path));

        return registry.Build();
    }

    public static Resource CreateResource(ResourceType resourceType, Platform platform, string filePath, bool x64 = false)
    {
        Console.WriteLine($"Constructing {platform} {resourceType} resource property data...");

        var key = (resourceType, platform);
        if (resourceCreators.TryGetValue(key, out var creator))
        {
            Resource output = creator(filePath);
            if (x64)
                output.SetResourceArch(Arch.x64);
            return output;
        }

        throw new InvalidPlatformException($"The '{resourceType}' type is not supported for the '{platform}' platform.");
    }

    private sealed class ResourceCreatorRegistry
    {
        private readonly Dictionary<(ResourceType, Platform), Func<string, Resource>> _creators = new();

        public void AddCreator(ResourceType resourceType, Platform platform, Func<string, Resource> creator)
        {
            _creators.Add((resourceType, platform), creator);
        }

        public void Add<TResource>(
            ResourceType resourceType,
            Platform platform,
            Func<string, TResource> creator,
            Action<TResource>? afterCreate = null)
            where TResource : Resource
        {
            AddCreator(resourceType, platform, path =>
            {
                TResource resource = creator(path);
                afterCreate?.Invoke(resource);
                return resource;
            });
        }

        public void AddWithPullAll<TResource>(
            ResourceType resourceType,
            Platform platform,
            Func<string, TResource> creator)
            where TResource : Resource
        {
            Add(resourceType, platform, creator, static resource => resource.PullAll());
        }

        public void AddEndianMapped<TResource>(
            ResourceType resourceType,
            Func<string, Endian, TResource> creator)
            where TResource : Resource
        {
            Add(resourceType, Platform.BPR, path => creator(path, Endian.LE));
            Add(resourceType, Platform.TUB, path => creator(path, Endian.LE));
            Add(resourceType, Platform.X360, path => creator(path, Endian.BE));
            Add(resourceType, Platform.PS3, path => creator(path, Endian.BE));
        }

        public Dictionary<(ResourceType, Platform), Func<string, Resource>> Build()
        {
            return _creators;
        }
    }
}
