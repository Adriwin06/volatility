namespace Volatility.Resources;

public abstract class TypedResource : Resource
{
    private readonly ResourceType _resourceType;

    protected TypedResource(ResourceType resourceType)
    {
        _resourceType = resourceType;
    }

    protected TypedResource(ResourceType resourceType, string path, Endian endianness = Endian.Agnostic)
        : this(resourceType)
    {
        InitializeFromPath(path, endianness);
    }

    public sealed override ResourceType ResourceType => _resourceType;
}
