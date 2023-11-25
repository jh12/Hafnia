using Db = Hafnia.DataAccess.MongoDB.Models.V2;

namespace Hafnia.DataAccess.MongoDB.Mappers.V2;

internal class MetadataMapper : IMapper<Db.Metadata?, DTOs.Metadata?>
{
    public DTOs.Metadata? Map(Db.Metadata? toMap)
    {
        if (toMap == null)
            return null;

        return new DTOs.Metadata
        (
            Id: toMap.Id.ToString()!,
            OriginalId: toMap.Source.Id,
            Uri: new Uri(toMap.Source.Uri),
            Title: toMap.Title ?? toMap.Source.Title,
            Tags: toMap.Tags.Select(t => t.ToString()).ToArray()
        );
    }
}
