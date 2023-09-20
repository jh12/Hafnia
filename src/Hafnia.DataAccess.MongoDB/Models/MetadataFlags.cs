namespace Hafnia.DataAccess.MongoDB.Models;

public record MetadataFlags
(
    bool HasFile,
    bool HasThumbnail
)
{
    internal static MetadataFlags CreateDefault()
    {
        return new MetadataFlags(false, false);
    }
}
