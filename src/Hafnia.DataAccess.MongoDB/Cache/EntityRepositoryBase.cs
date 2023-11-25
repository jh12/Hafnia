using System.Collections.Immutable;
using Hafnia.DataAccess.MongoDB.Config;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Hafnia.DataAccess.MongoDB.Cache;

internal abstract class EntityCacheBase<T> : IEntityCache<T>
{
    private readonly SemaphoreSlim _semaphore = new(1);

    private readonly List<T> _entities = new();
    public IMongoCollection<T> Collection { get; set; }

    protected EntityCacheBase(IMongoClient client, IOptions<MongoConfiguration> mongoConfig, string collectionName)
    {
        MongoConfiguration mongoConfigValue = mongoConfig.Value;

        IMongoDatabase database = client.GetDatabase(mongoConfigValue.Database);
        Collection = database.GetCollection<T>(collectionName);
    }

    public async Task<IEnumerable<T>> GetAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (!_entities.Any())
                _entities.AddRange(await GetInnerAsync(cancellationToken));

            return _entities.ToImmutableArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    protected abstract Task<IEnumerable<T>> GetInnerAsync(CancellationToken cancellationToken);

    public async Task Clear()
    {
        await _semaphore.WaitAsync();

        try
        {
            _entities.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
