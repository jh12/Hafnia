namespace Hafnia.Config;

public class BaseConfig
{
    public required bool CorsEnable { get; init; }
    public required string CorsDomains { get; init; }
    public required bool CorsAllowAll { get; init; }
}
