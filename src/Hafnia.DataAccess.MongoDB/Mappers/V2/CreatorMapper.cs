using Hafnia.DataAccess.MongoDB.Models.V2;
using MongoDB.Bson;

namespace Hafnia.DataAccess.MongoDB.Mappers.V2;

internal class CreatorMapper : IMapper<Creator, DTOs.Creator>, IMapper<DTOs.Creator, Creator>
{
    public DTOs.Creator Map(Creator toMap)
    {
        return new DTOs.Creator
        (
            Id: toMap.Id.ToString()!,
            Uri: new Uri(toMap.Uri),
            Username: toMap.Username
        );
    }

    public Creator Map(DTOs.Creator toMap)
    {
        return new Creator
        (
            Id: ObjectId.Parse(toMap.Id),
            Uri: toMap.Uri.AbsoluteUri,
            Username: toMap.Username
        );
    }
}
