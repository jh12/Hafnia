using Hafnia.DataAccess.MongoDB.Config;
using Hafnia.DataAccess.MongoDB.Mappers;
using Hafnia.DataAccess.MongoDB.Models;
using Hafnia.DataAccess.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

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

    public async Task<bool> ExistsIdAsync(string id, CancellationToken cancellationToken)
    {
        FilterDefinition<Metadata> filter = Builders<Metadata>.Filter.Eq(u => u.Id, ObjectId.Parse(id));

        Metadata metadata = await _metadataCollection.Find(filter)
            .SingleOrDefaultAsync(cancellationToken);

        return metadata != null;
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
}
