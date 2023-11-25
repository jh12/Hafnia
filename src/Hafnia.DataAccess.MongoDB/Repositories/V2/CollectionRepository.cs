using System.Runtime.CompilerServices;
using Hafnia.DataAccess.Exceptions;
using Hafnia.DataAccess.MongoDB.Cache;
using Hafnia.DataAccess.MongoDB.Config;
using Hafnia.DataAccess.MongoDB.Mappers.V2;
using Hafnia.DataAccess.MongoDB.Models.V2;
using Hafnia.DataAccess.Repositories.V2;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Metadata = Hafnia.DTOs.Metadata;

namespace Hafnia.DataAccess.MongoDB.Repositories.V2;

internal class CollectionRepository : EntityCacheBase<Collection>, ICollectionRepository
{
    private readonly IMapper<Collection, DTOs.Collection> _dtoMapper;
    private readonly IMetadataRepository _metadataRepository;

    public CollectionRepository(
        IMapper<Collection, DTOs.Collection> dtoMapper,
        IMetadataRepository metadataRepository,
        IMongoClient client,
        IOptions<MongoConfiguration> mongoConfig
    ) : base(client, mongoConfig, "v2_collection")
    {
        _dtoMapper = dtoMapper ?? throw new ArgumentNullException(nameof(dtoMapper));
        _metadataRepository = metadataRepository;
    }

    async IAsyncEnumerable<DTOs.Collection> ICollectionRepository.GetAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (DTOs.Collection collection in (await GetAsync(cancellationToken)).Select(_dtoMapper.Map))
        {
            yield return collection;
        }
    }

    protected override async Task<IEnumerable<Collection>> GetInnerAsync(CancellationToken cancellationToken)
    {
        return await Collection
            .Find(FilterDefinition<Collection>.Empty)
            .ToListAsync(cancellationToken);
    }

    public async IAsyncEnumerable<Metadata> GetContentAsync(string id, string sortField, bool ascending, int page, int pageSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var collections = await GetAsync(cancellationToken);
        Collection? collection = collections.SingleOrDefault(c => c.Id == ObjectId.Parse(id));

        if (collection == null)
        {
            throw new NotFoundException();
        }

        await foreach (Metadata metadata in _metadataRepository.GetForCollectionAsync(_dtoMapper.Map(collection), sortField, ascending, page, pageSize, cancellationToken))
        {
            yield return metadata;
        }
    }

    public async Task ClearCacheAsync()
    {
        await Clear();
    }
}
