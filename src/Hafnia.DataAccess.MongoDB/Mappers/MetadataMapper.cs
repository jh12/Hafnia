using MongoDB.Bson;
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
            OriginalId: metadata.OriginalId,
            Uri: new Uri(metadata.Uri),
            Title: metadata.Title,
            Tags: (metadata.Tags ?? Array.Empty<string>())
        );
    }

    public static Db.Metadata MapToDatabase(DTOs.Metadata metadata, Db.MetadataFlags flags)
    {
        return new Db.Metadata
        (
            Id: ObjectId.Parse(metadata.Id),
            OriginalId: metadata.OriginalId,
            Uri: metadata.Uri.AbsoluteUri,
            Title: metadata.Title,
            Flags: flags,
            Tags: metadata.Tags
        );
    }

    public static Db.Metadata MapToNewDatabase(DTOs.Metadata metadata, Db.MetadataFlags flags)
    {
        return new Db.Metadata
        (
            Id: ObjectId.GenerateNewId(),
            OriginalId: metadata.OriginalId,
            Uri: metadata.Uri.AbsoluteUri,
            Title: metadata.Title,
            Flags: flags,
            Tags: metadata.Tags
        );
    }
}
