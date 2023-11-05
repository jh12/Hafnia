using Db = Hafnia.DataAccess.MongoDB.Models.V2;

namespace Hafnia.DataAccess.MongoDB.Mappers.V2;

internal class MetadataMapper : IAsyncMapper<Db.Metadata?, DTOs.Metadata?>
{
    public async Task<DTOs.Metadata?> MapAsync(Db.Metadata? toMap, CancellationToken cancellationToken = default)
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
