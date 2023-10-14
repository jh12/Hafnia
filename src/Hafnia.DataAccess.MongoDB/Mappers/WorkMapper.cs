using MongoDB.Bson;
using Db = Hafnia.DataAccess.MongoDB.Models;

namespace Hafnia.DataAccess.MongoDB.Mappers;

internal static class WorkMapper
{
    public static DTOs.MetadataWork MapToDomain(Db.MetadataWork db)
    {
        return new DTOs.MetadataWork
        (
            Id: db.Id,
            MetadataId: db.MetadataId.ToString(),
            Origin: db.Origin,
            Complete: db.Complete,
            UpdatedAt: db.UpdatedAt,
            JsonData: db.Data.ToJson()
        );
    }

    public static Db.MetadataWork MapToDatabase(DTOs.MetadataWork domain)
    {
        return new Db.MetadataWork
        (
            Id: domain.Id,
            MetadataId: new ObjectId(domain.MetadataId),
            Origin: domain.Origin,
            Complete: domain.Complete,
            UpdatedAt: domain.UpdatedAt,
            Data: BsonDocument.Parse(domain.JsonData)
        );
    }
}
