using MongoDB.Bson;

namespace Hafnia.DataAccess.MongoDB.Models.V2;

public record Collection
(
    ObjectId Id,
    string Name,
    ObjectId Thumbnail,
    ObjectId[]? Children,
    ObjectId[]? IncludedTags,
    ObjectId[]? ExcludedTags,
    ObjectId[] MetadataIds,
    bool IsRoot
);
