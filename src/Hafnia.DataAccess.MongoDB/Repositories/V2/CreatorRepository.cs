using Hafnia.DataAccess.MongoDB.Cache;
using Hafnia.DataAccess.MongoDB.Config;
using Hafnia.DataAccess.MongoDB.Mappers.V2;
using Hafnia.DataAccess.MongoDB.Models.V2;
using Hafnia.DataAccess.Repositories.V2;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Hafnia.DataAccess.MongoDB.Repositories.V2;

internal class CreatorRepository : EntityCacheBase<Creator>, ICreatorRepository
{
    private readonly IMapper<Creator, DTOs.Creator> _dtoMapper;

    public CreatorRepository
    (
        IMapper<Creator, DTOs.Creator> dtoMapper,
        IMongoClient client,
        IOptions<MongoConfiguration> mongoConfig
    ) : base(client, mongoConfig, "v2_creator")
    {
        _dtoMapper = dtoMapper ?? throw new ArgumentNullException(nameof(dtoMapper));
    }

    async IAsyncEnumerable<DTOs.Creator> ICreatorRepository.GetAsync(CancellationToken cancellationToken = default)
    {
        foreach (DTOs.Creator creator in (await GetAsync(cancellationToken)).Select(_dtoMapper.Map))
        {
            yield return creator;
        }
    }

    public async Task<DTOs.Creator> GetOrCreateAsync(DTOs.Creator creator, CancellationToken cancellationToken = default)
    {
        IEnumerable<Creator> creators = await GetAsync(cancellationToken);
        Creator? existing = creators.SingleOrDefault(c => c.Uri == creator.Uri.AbsoluteUri);

        if (existing != null)
            return creator;

        await Semaphore.WaitAsync(CancellationToken.None);

        try
        {
            Creator newCreator = new Creator(ObjectId.GenerateNewId(), creator.Uri.AbsoluteUri, creator.Username);
            await Collection.InsertOneAsync(newCreator, new InsertOneOptions(), cancellationToken);

            await Clear();

            return _dtoMapper.Map(newCreator);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    protected override async Task<IEnumerable<Creator>> GetInnerAsync(CancellationToken cancellationToken)
    {
        return await Collection
            .Find(FilterDefinition<Creator>.Empty)
            .ToListAsync(cancellationToken);
    }
}
