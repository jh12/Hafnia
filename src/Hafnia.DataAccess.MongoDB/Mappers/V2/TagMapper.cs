using Hafnia.DataAccess.MongoDB.Models.V2;
using MongoDB.Bson;

namespace Hafnia.DataAccess.MongoDB.Mappers.V2;

internal class TagMapper : IMapper<Tag, DTOs.Tag>, IMapper<string, ObjectId>
{
    public DTOs.Tag Map(Tag toMap)
    {
        return new DTOs.Tag(
            Id: toMap.Id.ToString()!,
            Name: toMap.Name,
            Parent: toMap.Parent?.ToString(),
            Ancestors: toMap.Ancestors?.Select(a => a.ToString()).ToArray() ?? Array.Empty<string>()
        );
    }

    public ObjectId Map(string toMap)
    {
        return ObjectId.Parse(toMap);
    }
}
