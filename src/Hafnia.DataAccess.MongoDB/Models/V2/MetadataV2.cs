using MongoDB.Bson;

namespace Hafnia.DataAccess.MongoDB.Models.V2;

internal record Metadata
(
    ObjectId Id,
    string? Title,
    ObjectId[] Tags,
    MetadataSource Source
);

internal record MetadataSource
(
    string? Id,
    string Uri,
    string Title,
    ObjectId? CreatorId,
    string[] Tags
);

internal record SourceCreator
(
    ObjectId Id,
    string Source,
    string? SourceId,
    string Name,
    string? ProfileUrl
);
