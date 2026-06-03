namespace Volatility.Resources;

public class ExternalResourceImport
{
    public long Offset { get; set; }
    public ResourceImport ResourceReference { get; set; }
    public string Role { get; set; } = string.Empty;
}
