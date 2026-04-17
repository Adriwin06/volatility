using System.Collections.Concurrent;

namespace Volatility.Resources;

public abstract class TypedResource : Resource
{
    private static readonly ConcurrentDictionary<Type, ResourceType> RuntimeResourceTypes = new();
    private readonly ResourceType _resourceType;

    protected TypedResource()
    {
        _resourceType = GetRuntimeResourceTypeFor(GetType());
    }

    protected TypedResource(ResourceType resourceType)
    {
        _resourceType = resourceType;
    }

    protected TypedResource(string path, Endian endianness = Endian.Agnostic)
    {
        _resourceType = GetRuntimeResourceTypeFor(GetType());
        InitializeFromPath(path, endianness);
    }

    protected TypedResource(ResourceType resourceType, string path, Endian endianness = Endian.Agnostic)
        : this(resourceType)
    {
        InitializeFromPath(path, endianness);
    }

    public sealed override ResourceType ResourceType => _resourceType;

    private static ResourceType GetRuntimeResourceTypeFor(Type resourceClass)
    {
        return RuntimeResourceTypes.GetOrAdd(resourceClass, ResourceMetadata.GetResourceType);
    }
}
