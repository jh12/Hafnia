using System.Runtime.CompilerServices;
using Amazon.SecurityToken.Model;
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

    public async Task<string> GetIdFromUrlAsync(Uri uri, CancellationToken cancellationToken)
    {
        FilterDefinition<Metadata> filter = Builders<Metadata>.Filter.Eq(u => u.Uri, uri.AbsoluteUri);
        Metadata? existing = await _metadataCollection.Find(filter).SingleOrDefaultAsync(cancellationToken);

        if (existing == null)
        {
            Metadata replacement = Metadata.CreateEmpty(uri);
            await _metadataCollection.InsertOneAsync(replacement, new InsertOneOptions(), cancellationToken);

            return replacement.Id.ToString()!;
        }

        return existing.Id.ToString()!;
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
}
