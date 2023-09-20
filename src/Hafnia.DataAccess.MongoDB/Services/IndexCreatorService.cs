using Hafnia.DataAccess.MongoDB.Config;
using Hafnia.DataAccess.MongoDB.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Hafnia.DataAccess.MongoDB.Services;

public class IndexCreatorService : BackgroundService
{
    private readonly IMongoCollection<Metadata> _metadataCollection;

    public IndexCreatorService(IMongoClient client, IOptions<MongoConfiguration> mongoConfig)
    {
        MongoConfiguration mongoConfigValue = mongoConfig.Value;

        IMongoDatabase database = client.GetDatabase(mongoConfigValue.Database);
        _metadataCollection = database.GetCollection<Metadata>("metadata");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _metadataCollection.Indexes.CreateOneAsync(new CreateIndexModel<Metadata>(Builders<Metadata>.IndexKeys.Ascending(m => m.Uri), new CreateIndexOptions { Unique = true }), cancellationToken: stoppingToken);
        await _metadataCollection.Indexes.CreateOneAsync(new CreateIndexModel<Metadata>(Builders<Metadata>.IndexKeys.Ascending(m => m.Tags)), cancellationToken: stoppingToken);
        await _metadataCollection.Indexes.CreateOneAsync(new CreateIndexModel<Metadata>(Builders<Metadata>.IndexKeys.Ascending(m => m.Flags.HasFile)), cancellationToken: stoppingToken);
    }
}
