using System.Runtime.CompilerServices;
using Hafnia.DataAccess.MongoDB.Config;
using Hafnia.DataAccess.MongoDB.Mappers;
using Hafnia.DataAccess.MongoDB.Models;
using Hafnia.DataAccess.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Hafnia.DataAccess.MongoDB.Repositories;

public class WorkRepository : IWorkRepository
{
    private readonly IMongoCollection<MetadataWork> _metadataCollection;

    public WorkRepository(IMongoClient client, IOptions<MongoConfiguration> mongoConfig)
    {
        MongoConfiguration mongoConfigValue = mongoConfig.Value;

        IMongoDatabase database = client.GetDatabase(mongoConfigValue.Database);

        _metadataCollection = database.GetCollection<MetadataWork>("work_metadata");
    }

    public async IAsyncEnumerable<DTOs.MetadataWork> GetAsync(string origin, string[] ids, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var filterBuilder = Builders<MetadataWork>.Filter;

        FilterDefinition<MetadataWork> originFilter = filterBuilder.Eq(w => w.Origin, origin);
        FilterDefinition<MetadataWork> idFilter = filterBuilder.In(w => w.Id, ids);

        FilterDefinition<MetadataWork> filter = filterBuilder.And(originFilter, idFilter);

        IAsyncCursor<MetadataWork> cursor = await _metadataCollection
            .Find(filter)
            .ToCursorAsync(cancellationToken);

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (MetadataWork work in cursor.Current)
            {
                yield return WorkMapper.MapToDomain(work);
            }
        }
    }

    public async IAsyncEnumerable<DTOs.MetadataWork> GetOrCreateAsync(string origin, DTOs.MetadataWork[] list, bool excludeCompleted, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        MetadataWork[] database = list
            .Select(WorkMapper.MapToDatabase)
            .ToArray();

        Dictionary<string, MetadataWork> missingItems = database
            .Where(d => !d.Complete)
            .ToDictionary(d => d.Id);

        var filterBuilder = Builders<MetadataWork>.Filter;

        FilterDefinition<MetadataWork> originFilter = filterBuilder.Eq(w => w.Origin, origin);
        FilterDefinition<MetadataWork> idFilter = filterBuilder.In(w => w.Id, list.Select(l => l.Id));
        FilterDefinition<MetadataWork> completeFilter = FilterDefinition<MetadataWork>.Empty;

        if (excludeCompleted)
            completeFilter = filterBuilder.Ne(w => w.Complete, true);

        FilterDefinition<MetadataWork> filter = filterBuilder.And(originFilter, idFilter, completeFilter);

        List<MetadataWork> existingItems = await _metadataCollection
            .Find(filter)
            .ToListAsync(cancellationToken);

        existingItems.ForEach(i => missingItems.Remove(i.Id));

        if (missingItems.Count > 0)
            await _metadataCollection.InsertManyAsync(missingItems.Values, new InsertManyOptions(), cancellationToken);

        foreach (DTOs.MetadataWork metadataWork in list)
        {
            yield return metadataWork;
        }
    }

    public async IAsyncEnumerable<DTOs.MetadataWork> GetWorkAsync(string origin, DateTime? updatedAfter, int limit, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var filterBuilder = Builders<MetadataWork>.Filter;

        FilterDefinition<MetadataWork> originFilter = filterBuilder.Eq(w => w.Origin, origin);
        FilterDefinition<MetadataWork> updateFilter = FilterDefinition<MetadataWork>.Empty;
        FilterDefinition<MetadataWork> completeFilter = filterBuilder.Ne(w => w.Complete, true);

        if (updatedAfter.HasValue)
            updateFilter = filterBuilder.Gte(w => w.UpdatedAt, updatedAfter.Value);

        FilterDefinition<MetadataWork> filter = filterBuilder.And(originFilter, updateFilter, completeFilter);

        IAsyncCursor<MetadataWork>? cursor = await _metadataCollection
            .Find(filter)
            .Sort(Builders<MetadataWork>.Sort.Ascending(w => w.UpdatedAt))
            .Limit(limit)
            .ToCursorAsync(cancellationToken);

        foreach (MetadataWork work in cursor.ToEnumerable(cancellationToken))
        {
            yield return WorkMapper.MapToDomain(work);
        }
    }

    public async Task UpdateAsync(string origin, string id, DTOs.MetadataWork item, CancellationToken cancellationToken)
    {
        MetadataWork db = WorkMapper.MapToDatabase(item);

        var filterBuilder = Builders<MetadataWork>.Filter;

        FilterDefinition<MetadataWork> originFilter = filterBuilder.Eq(w => w.Origin, origin);
        FilterDefinition<MetadataWork> idFilter = filterBuilder.Eq(w => w.Id, id);

        FilterDefinition<MetadataWork> filter = filterBuilder.And(originFilter, idFilter);

        var updateBuilder = Builders<MetadataWork>.Update;

        UpdateDefinition<MetadataWork> update = updateBuilder.Set(w => w.UpdatedAt, DateTime.UtcNow)
            .Set(w => w.Complete, item.Complete)
            .Set(w => w.Data, db.Data);

        await _metadataCollection
            .UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }
}
