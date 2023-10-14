using System.Runtime.CompilerServices;
using Hafnia.DataAccess.MongoDB.Config;
using Hafnia.DataAccess.MongoDB.Mappers;
using Hafnia.DataAccess.MongoDB.Models;
using Hafnia.DataAccess.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Hafnia.DataAccess.MongoDB.Repositories;

public class MetadataRepository : IMetadataRepository
{
    private readonly IMongoCollection<Metadata> _metadataCollection;

    public MetadataRepository(IMongoClient client, IOptions<MongoConfiguration> mongoConfig)
    {
        MongoConfiguration mongoConfigValue = mongoConfig.Value;

        IMongoDatabase database = client.GetDatabase(mongoConfigValue.Database);

        _metadataCollection = database.GetCollection<Metadata>("metadata");
    }

    public async Task<string?> GetIdFromUrlAsync(Uri uri, CancellationToken cancellationToken)
    {
        FilterDefinition<Metadata> filter = Builders<Metadata>.Filter.Eq(u => u.Uri, uri.AbsoluteUri);
        Metadata? existing = await _metadataCollection.Find(filter).SingleOrDefaultAsync(cancellationToken);

        if (existing == null)
        {
            return null;
        }

        return existing.Id.ToString()!;
    }

    public async Task<(bool Created, DTOs.Metadata Metadata)> GetOrCreateMetadataAsync(DTOs.Metadata metadata, CancellationToken cancellationToken = default)
    {
        FilterDefinition<Metadata> filter = Builders<Metadata>.Filter.Eq(u => u.Uri, metadata.Uri.AbsoluteUri);
        Metadata? existing = await _metadataCollection.Find(filter).SingleOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            return (false, MetadataMapper.MapToDomain(existing));
        }

        Metadata newMetadata = MetadataMapper.MapToNewDatabase(metadata, MetadataFlags.CreateDefault());
        await _metadataCollection.InsertOneAsync(newMetadata, new InsertOneOptions(), cancellationToken);

        return (true, MetadataMapper.MapToDomain(newMetadata));
    }

    public async IAsyncEnumerable<string> GetRandomIdsAsync(int size, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncCursor<string> cursorAsync = await _metadataCollection
            .AsQueryable()
            .Sample(size)
            .Select(c => c.Id.ToString())
            .ToCursorAsync(cancellationToken);

        foreach (string s in cursorAsync.ToEnumerable(cancellationToken))
        {
            yield return s;
        }
    }

    public async Task<bool> ExistsIdAsync(string id, CancellationToken cancellationToken)
    {
        FilterDefinition<Metadata> filter = Builders<Metadata>.Filter.Eq(u => u.Id, ObjectId.Parse(id));

        Metadata metadata = await _metadataCollection.Find(filter)
            .SingleOrDefaultAsync(cancellationToken);

        return metadata != null;
    }

    public async Task<DTOs.Metadata> GetMetadataFromUrlAsync(Uri uri, CancellationToken cancellationToken)
    {
        FilterDefinition<Metadata> filter = Builders<Metadata>.Filter.Eq(u => u.Uri, uri.AbsoluteUri);
        Metadata? existing = await _metadataCollection.Find(filter).SingleOrDefaultAsync(cancellationToken);

        if (existing == null)
        {
            Metadata replacement = Metadata.CreateEmpty(uri);
            await _metadataCollection.InsertOneAsync(replacement, new InsertOneOptions(), cancellationToken);

            return MetadataMapper.MapToDomain(replacement);
        }

        return MetadataMapper.MapToDomain(existing);
    }

    public async Task<DTOs.Metadata?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        Metadata? metadata = await _metadataCollection.Find(m => m.Id == ObjectId.Parse(id)).SingleOrDefaultAsync(cancellationToken);

        return MetadataMapper.MapNullableToDomain(metadata);
    }

    public async Task UpdateAsync(string id, DTOs.Metadata metadata, CancellationToken cancellationToken = default)
    {
        FilterDefinition<Metadata> idFilter = Builders<Metadata>.Filter.Eq(m => m.Id, ObjectId.Parse(id));

        UpdateDefinition<Metadata> updateOriginalId = Builders<Metadata>.Update
            .Set(m => m.OriginalId, metadata.OriginalId)
            .Set(m => m.Title, metadata.Title)
            .Set(m => m.Tags, metadata.Tags);

        await _metadataCollection.UpdateOneAsync(idFilter, updateOriginalId, cancellationToken: cancellationToken);
    }

    public async Task SetTagsAsync(string id, string[] tags, CancellationToken cancellationToken = default)
    {
        tags = tags.Select(t => t.ToLower()).ToArray();

        FilterDefinition<Metadata> filter = Builders<Metadata>.Filter.Eq(m => m.Id, ObjectId.Parse(id));
        UpdateDefinition<Metadata> update = Builders<Metadata>.Update.Set(m => m.Tags, tags);

        await _metadataCollection.UpdateOneAsync(filter, update, new UpdateOptions(), cancellationToken);
    }

    public async Task SetHasFileAsync(string id, bool hasFile, CancellationToken cancellationToken = default)
    {
        FilterDefinition<Metadata> filter = Builders<Metadata>.Filter.Eq(m => m.Id, ObjectId.Parse(id));
        UpdateDefinition<Metadata> update = Builders<Metadata>.Update.Set(m => m.Flags.HasFile, hasFile);

        await _metadataCollection.UpdateOneAsync(filter, update, new UpdateOptions(), cancellationToken);
    }

    public async Task SetHasThumbnailAsync(string id, bool hasThumbnail, CancellationToken cancellationToken = default)
    {
        FilterDefinition<Metadata> filter = Builders<Metadata>.Filter.Eq(m => m.Id, ObjectId.Parse(id));
        UpdateDefinition<Metadata> update = Builders<Metadata>.Update.Set(m => m.Flags.HasThumbnail, hasThumbnail);

        await _metadataCollection.UpdateOneAsync(filter, update, new UpdateOptions(), cancellationToken);
    }

    public async IAsyncEnumerable<DTOs.Metadata> SearchAsync(string[] allTags, string[] anyTags, int? limit, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        FilterDefinitionBuilder<Metadata> filterBuilder = Builders<Metadata>.Filter;

        FilterDefinition<Metadata> allTagFilter = filterBuilder.Empty;
        FilterDefinition<Metadata> anyTagFilter = filterBuilder.Empty;

        if (allTags.Length > 0)
        {
            allTags = allTags.Select(t => t.ToLower()).ToArray();

            allTagFilter = filterBuilder.All(m => m.Tags, allTags);
        }

        if (anyTags.Length > 0)
        {
            anyTags = anyTags.Select(t => t.ToLower()).ToArray();

            anyTagFilter = filterBuilder.AnyIn(m => m.Tags, anyTags);
        }

        FilterDefinition<Metadata> filter = filterBuilder.And(allTagFilter, anyTagFilter);

        IAsyncCursor<Metadata> cursor = await _metadataCollection
            .Find(filter)
            .Limit(limit)
            .ToCursorAsync(cancellationToken);

        foreach (Metadata metadata in cursor.ToEnumerable(cancellationToken))
        {
            yield return MetadataMapper.MapToDomain(metadata);
        }
    }

    public async Task<string[]> GetAllTags(CancellationToken cancellationToken = default)
    {
        FilterDefinition<Metadata> tagExistsFilter = Builders<Metadata>.Filter.Exists(f => f.Tags);

        IAsyncCursor<string> cursor = await _metadataCollection
            .DistinctAsync<string>("Tags", tagExistsFilter, cancellationToken: cancellationToken);

        return cursor.ToEnumerable(cancellationToken).ToArray();
    }

    public async Task<string[]> SearchTags(string[] allTags, string[] anyTags, CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<Metadata> filterBuilder = Builders<Metadata>.Filter;

        FilterDefinition<Metadata> tagExists = Builders<Metadata>.Filter.Exists(f => f.Tags);

        FilterDefinition<Metadata> allTagFilter = filterBuilder.Empty;
        FilterDefinition<Metadata> anyTagFilter = filterBuilder.Empty;

        if (allTags.Length > 0)
        {
            allTags = allTags.Select(t => t.ToLower()).ToArray();

            allTagFilter = filterBuilder.All(m => m.Tags, allTags);
        }

        if (anyTags.Length > 0)
        {
            anyTags = anyTags.Select(t => t.ToLower()).ToArray();

            anyTagFilter = filterBuilder.AnyIn(m => m.Tags, anyTags);
        }

        FilterDefinition<Metadata> filter = filterBuilder.And(tagExists, allTagFilter, anyTagFilter);

        IAsyncCursor<string> cursor = await _metadataCollection
            .DistinctAsync<string>("Tags", filter, cancellationToken: cancellationToken);

        return cursor.ToEnumerable(cancellationToken).ToArray();
    }

    public async IAsyncEnumerable<DTOs.Metadata> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncCursor<Metadata>? cursor = await _metadataCollection
            .Find(FilterDefinition<Metadata>.Empty)
            .ToCursorAsync(cancellationToken);

        foreach (Metadata s in cursor.ToEnumerable(cancellationToken))
        {
            yield return MetadataMapper.MapToDomain(s);
        }
    }
}
