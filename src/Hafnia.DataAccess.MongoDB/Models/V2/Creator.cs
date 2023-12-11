using MongoDB.Bson;

namespace Hafnia.DataAccess.MongoDB.Models.V2;

public record Creator
(
    ObjectId Id,
    string Uri,
    string Username
);
