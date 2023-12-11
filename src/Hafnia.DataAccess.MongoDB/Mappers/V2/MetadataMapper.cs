using Hafnia.DataAccess.MongoDB.Cache;
using Hafnia.DTOs;
using Hafnia.DTOs.V2;
using MongoDB.Bson;
using Db = Hafnia.DataAccess.MongoDB.Models.V2;

namespace Hafnia.DataAccess.MongoDB.Mappers.V2;

internal class MetadataMapper : IMapper<Db.Metadata?, Metadata?>, IMapper<Db.Metadata?, MetadataV2?>, IMapper<MetadataSourceV2, Db.Metadata>, IAsyncMapper<IEnumerable<Db.Metadata>, IEnumerable<MetadataWithSourceV2>>
{
    private readonly IEntityCache<Db.Creator> _creatorCache;

    public MetadataMapper(IEntityCache<Db.Creator> creatorCache)
    {
        _creatorCache = creatorCache ?? throw new ArgumentNullException(nameof(creatorCache));
    }

    public Metadata? Map(Db.Metadata? toMap)
    {
        if (toMap == null)
            return null;

        return new Metadata
        (
            Id: toMap.Id.ToString()!,
            OriginalId: toMap.Source.Id,
            Uri: new Uri(toMap.Source.Uri),
            Title: toMap.Title ?? toMap.Source.Title,
            Tags: toMap.Tags.Select(t => t.ToString()).ToArray()
        );
    }

    MetadataV2 IMapper<Db.Metadata?, MetadataV2?>.Map(Db.Metadata? toMap)
    {
        if (toMap == null)
            return null;

        return new MetadataV2
        (
            Id: toMap.Id.ToString()!,
            Title: toMap.Title!,
            Tags: toMap.Tags.Select(t => t.ToString()).ToArray()
        );
    }

    public Db.Metadata Map(MetadataSourceV2 toMap)
    {
        return new Db.Metadata
        (
            Id: ObjectId.GenerateNewId(),
            Title: toMap.Title,
            Tags: Array.Empty<ObjectId>(),
            Source: new Db.MetadataSource
                (
                    Id: toMap.Id,
                    Uri: toMap.Uri,
                    Title: toMap.Title,
                    CreatorId: null,
                    Tags: toMap.Tags
                )
        );
    }

    public async Task<IEnumerable<MetadataWithSourceV2>> MapAsync(IEnumerable<Db.Metadata> toMap, CancellationToken cancellationToken = default)
    {
        IEnumerable<Db.Creator> creators = await _creatorCache.GetAsync(cancellationToken);

        HashSet<ObjectId> creatorMap = creators.Select(c => c.Id).ToHashSet();

        return toMap.Select(tm =>
        {
            return new MetadataWithSourceV2
            (
                Id: tm.Id.ToString()!,
                Title: tm.Title!,
                Tags: tm.Tags.Select(t => t.ToString()).ToArray(),
                Source: new MetadataSourceV2
                (
                    Id: tm.Source.Id!,
                    Uri: tm.Source.Uri,
                    Title: tm.Source.Title,
                    Tags: tm.Source.Tags,
                    CreatorId: tm.Source.CreatorId.HasValue && creatorMap.Contains(tm.Source.CreatorId.Value) ? tm.Source.CreatorId.ToString() : null
                )
            );
        }).ToArray();
    }
}
