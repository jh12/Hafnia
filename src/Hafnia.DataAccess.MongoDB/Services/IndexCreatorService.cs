using Hafnia.DataAccess.MongoDB.Config;
using Hafnia.DataAccess.MongoDB.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using V2 = Hafnia.DataAccess.MongoDB.Models.V2;

namespace Hafnia.DataAccess.MongoDB.Services;

public class IndexCreatorService : BackgroundService
{
    private readonly IMongoCollection<Metadata> _metadataCollection;
    private readonly IMongoCollection<MetadataWork> _metadataWorkCollection;
    private readonly IMongoCollection<V2.Metadata> _metadataV2Collection;
    private readonly IMongoCollection<V2.Tag> _tagCollection;
    private readonly IMongoCollection<V2.Creator> _creatorV2Collection;

    public IndexCreatorService(IMongoClient client, IOptions<MongoConfiguration> mongoConfig)
    {
        MongoConfiguration mongoConfigValue = mongoConfig.Value;

        IMongoDatabase database = client.GetDatabase(mongoConfigValue.Database);
        _metadataCollection = database.GetCollection<Metadata>("metadata");
        _metadataWorkCollection = database.GetCollection<MetadataWork>("work_metadata");
        _metadataV2Collection = database.GetCollection<V2.Metadata>("v2_metadata");
        _creatorV2Collection = database.GetCollection<V2.Creator>("v2_creator");
        _tagCollection = database.GetCollection<V2.Tag>("v2_tag");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Metadata
        await _metadataCollection.Indexes.CreateOneAsync(new CreateIndexModel<Metadata>(Builders<Metadata>.IndexKeys.Ascending(m => m.Uri), new CreateIndexOptions { Unique = true }), cancellationToken: stoppingToken);
        await _metadataCollection.Indexes.CreateOneAsync(new CreateIndexModel<Metadata>(Builders<Metadata>.IndexKeys.Ascending(m => m.Tags)), cancellationToken: stoppingToken);
        await _metadataCollection.Indexes.CreateOneAsync(new CreateIndexModel<Metadata>(Builders<Metadata>.IndexKeys.Ascending(m => m.Flags.HasFile)), cancellationToken: stoppingToken);

        // MetadataWork
        await _metadataWorkCollection.Indexes.CreateOneAsync(new CreateIndexModel<MetadataWork>(Builders<MetadataWork>.IndexKeys.Ascending(m => m.Origin).Ascending(m => m.Id), new CreateIndexOptions { Unique = true }), cancellationToken: stoppingToken);
        await _metadataWorkCollection.Indexes.CreateOneAsync(new CreateIndexModel<MetadataWork>(Builders<MetadataWork>.IndexKeys.Ascending(m => m.Complete)), cancellationToken: stoppingToken);
        await _metadataWorkCollection.Indexes.CreateOneAsync(new CreateIndexModel<MetadataWork>(Builders<MetadataWork>.IndexKeys.Ascending(m => m.UpdatedAt)), cancellationToken: stoppingToken);

        // V2
        // Metadata
        await _metadataV2Collection.Indexes.CreateOneAsync(new CreateIndexModel<V2.Metadata>(Builders<V2.Metadata>.IndexKeys.Ascending(m => m.Source.Uri), new CreateIndexOptions { Unique = true }), cancellationToken: stoppingToken);
        await _metadataV2Collection.Indexes.CreateOneAsync(new CreateIndexModel<V2.Metadata>(Builders<V2.Metadata>.IndexKeys.Ascending(m => m.Tags)), cancellationToken: stoppingToken);
        
        // Tag
        await _tagCollection.Indexes.CreateOneAsync(new CreateIndexModel<V2.Tag>(Builders<V2.Tag>.IndexKeys.Ascending(m => m.Name), new CreateIndexOptions { Unique = true }), cancellationToken: stoppingToken);
        await _tagCollection.Indexes.CreateOneAsync(new CreateIndexModel<V2.Tag>(Builders<V2.Tag>.IndexKeys.Ascending(m => m.AncestorsAndSelf)), cancellationToken: stoppingToken);

        // Creator
        await _creatorV2Collection.Indexes.CreateOneAsync(new CreateIndexModel<V2.Creator>(Builders<V2.Creator>.IndexKeys.Ascending(m => m.Uri), new CreateIndexOptions { Unique = true }), cancellationToken: stoppingToken);
    }
}
