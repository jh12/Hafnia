using MongoDB.Bson;

namespace Hafnia.DataAccess.MongoDB.Models;

public record MetadataWork
(
    string Id,
    ObjectId MetadataId,
    string Origin,
    bool Complete,
    DateTime UpdatedAt,
    BsonDocument Data
);
