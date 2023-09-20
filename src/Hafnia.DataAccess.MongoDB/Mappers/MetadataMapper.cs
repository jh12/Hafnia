using Db = Hafnia.DataAccess.MongoDB.Models;

namespace Hafnia.DataAccess.MongoDB.Mappers;

internal static class MetadataMapper
{
    public static DTOs.Metadata? MapNullableToDomain(Db.Metadata? metadata)
    {
        return metadata == null ? null : MapToDomain(metadata);
    }

    public static DTOs.Metadata MapToDomain(Db.Metadata metadata)
    {
        return new DTOs.Metadata
        (
            Id: metadata.Id.ToString()!,
            Uri: new Uri(metadata.Uri),
            Tags: (metadata.Tags ?? Array.Empty<string>())
        );
    }
}
