using System.Runtime.CompilerServices;
using Hafnia.DataAccess.Models;
using Hafnia.DataAccess.MongoDB.Config;
using Hafnia.DataAccess.MongoDB.Mappers.V2;
using Hafnia.DataAccess.Repositories.V2;
using Hafnia.DataAccess.MongoDB.Models.V2;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Tag = Hafnia.DTOs.Tag;

namespace Hafnia.DataAccess.MongoDB.Repositories.V2;

internal class MetadataRepository : IMetadataRepository
{
    private readonly ITagRepository _tagRepository;
    private readonly IAsyncMapper<Metadata, DTOs.Metadata> _metadataMapper;
    private readonly IMapper<string, ObjectId> _tagIdMapper;
    private readonly IMongoCollection<Metadata> _metadataCollection;

    public MetadataRepository(
        IMongoClient client,
        ITagRepository tagRepository,
        IAsyncMapper<Metadata, DTOs.Metadata> metadataMapper,
        IMapper<string, ObjectId> tagIdMapper,
        IOptions<MongoConfiguration> mongoConfig)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _metadataMapper = metadataMapper ?? throw new ArgumentNullException(nameof(metadataMapper));
        _tagIdMapper = tagIdMapper ?? throw new ArgumentNullException(nameof(tagIdMapper));

        MongoConfiguration mongoConfigValue = mongoConfig.Value;

        IMongoDatabase database = client.GetDatabase(mongoConfigValue.Database);

        _metadataCollection = database.GetCollection<Metadata>("v2_metadata");
    }

    public async Task<DTOs.Metadata?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        Metadata? metadata = await _metadataCollection.Find(m => m.Id == ObjectId.Parse(id)).SingleOrDefaultAsync(cancellationToken);

        return await _metadataMapper.MapAsync(metadata, cancellationToken);
    }

    public async IAsyncEnumerable<DTOs.Metadata> SearchAsync(string[] allTags, string[] anyTags, int? limit, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<Metadata> filterBuilder = Builders<Metadata>.Filter;

        FilterDefinition<Metadata> allTagFilter = filterBuilder.Empty;
        FilterDefinition<Metadata> anyTagFilter = filterBuilder.Empty;

        if (allTags.Length > 0)
        {
            ObjectId[] allTagIds = allTags.Select(_tagIdMapper.Map).ToArray();

            allTagFilter = filterBuilder.All(m => m.Tags, allTagIds);
        }

        if (anyTags.Length > 0)
        {
            ObjectId[] anyTagIds = anyTags.Select(_tagIdMapper.Map).ToArray();

            anyTagFilter = filterBuilder.AnyIn(m => m.Tags, anyTagIds);
        }

        FilterDefinition<Metadata> filter = filterBuilder.And(allTagFilter, anyTagFilter);

        IAsyncCursor<Metadata> cursor = await _metadataCollection
            .Find(filter)
            .Limit(limit)
            .ToCursorAsync(cancellationToken);

        foreach (Metadata metadata in cursor.ToEnumerable(cancellationToken))
        {
            yield return await _metadataMapper.MapAsync(metadata, cancellationToken);
        }
    }

    public async IAsyncEnumerable<DTOs.Metadata> GetAllAsync(string? after, int limit, TagFilter tagFilter, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<Metadata> filterBuilder = Builders<Metadata>.Filter;
        FilterDefinition<Metadata> filter = filterBuilder.Empty;

        SortDefinition<Metadata> order = Builders<Metadata>.Sort.Ascending(m => m.Id);

        if (!string.IsNullOrEmpty(after))
        {
            ObjectId afterId = ObjectId.Parse(after);
            
            filter &= Builders<Metadata>.Filter.Gt(m => m.Id, afterId);
        }

        if (tagFilter.Include.Any())
        {
            IEnumerable<Tag> allTags = await _tagRepository.GetTagWithChildrenAsync(tagFilter.Include, cancellationToken);

            ObjectId[] allTagIds = allTags.Select(t => _tagIdMapper.Map(t.Id)).ToArray();

            filter &= filterBuilder.AnyIn(m => m.Tags, allTagIds);
        }

        if (tagFilter.Exclude.Any())
        {
            IEnumerable<Tag> allTags = await _tagRepository.GetTagWithChildrenAsync(tagFilter.Exclude, cancellationToken);
            
            ObjectId[] allTagIds = allTags.Select(t => _tagIdMapper.Map(t.Id)).ToArray();

            filter &= filterBuilder.Not(filterBuilder.All(m => m.Tags, allTagIds));
        }

        IAsyncCursor<Metadata> cursor = await _metadataCollection
            .Find(filter)
            .Sort(order)
            .Limit(limit)
            .ToCursorAsync(cancellationToken);

        foreach (Metadata metadata in cursor.ToEnumerable(cancellationToken))
        {
            yield return await _metadataMapper.MapAsync(metadata, cancellationToken);
        }
    }
}
