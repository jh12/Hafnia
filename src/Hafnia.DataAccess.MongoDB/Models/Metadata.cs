using MongoDB.Bson;

namespace Hafnia.DataAccess.MongoDB.Models;

public record Metadata
(
    ObjectId Id,
    string Uri,
    MetadataFlags Flags,
    string[]? Tags
)
{
    public static Metadata CreateEmpty(Uri uri)
    {
        return new Metadata(ObjectId.GenerateNewId(), uri.AbsoluteUri, MetadataFlags.CreateDefault(), Array.Empty<string>());
    }
}
