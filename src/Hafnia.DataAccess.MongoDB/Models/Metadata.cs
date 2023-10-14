using MongoDB.Bson;

namespace Hafnia.DataAccess.MongoDB.Models;

public record Metadata
(
    ObjectId Id,
    string? OriginalId,
    string Uri,
    string Title,
    MetadataFlags Flags,
    string[]? Tags
)
{
    public static Metadata CreateEmpty(Uri uri)
    {
        return new Metadata(ObjectId.GenerateNewId(), null, uri.AbsoluteUri, string.Empty, MetadataFlags.CreateDefault(), Array.Empty<string>());
    }
}
