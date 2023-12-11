using System.Runtime.CompilerServices;
using Hafnia.DataAccess.Exceptions;
using Hafnia.DataAccess.Models;
using Hafnia.DataAccess.MongoDB.Config;
using Hafnia.DataAccess.MongoDB.Mappers.V2;
using Hafnia.DataAccess.Repositories.V2;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Metadata = Hafnia.DataAccess.MongoDB.Models.V2.Metadata;

namespace Hafnia.DataAccess.MongoDB.Repositories.V2;

internal class MetadataRepository : IMetadataRepository
{
    private readonly ITagRepository _tagRepository;
    private readonly IMapper<Metadata, DTOs.V2.MetadataV2> _metadataMapper;
    private readonly IAsyncMapper<IEnumerable<Metadata>, IEnumerable<DTOs.V2.MetadataWithSourceV2>> _metadataSourceMapper;
    private readonly IMapper<DTOs.V2.MetadataSourceV2, Metadata> _sourceMapper;
    private readonly IMapper<string, ObjectId> _tagIdMapper;
    private readonly IMongoCollection<Metadata> _metadataCollection;

    public MetadataRepository(
        IMongoClient client,
        ITagRepository tagRepository,
        IMapper<Metadata, DTOs.V2.MetadataV2> metadataMapper,
        IAsyncMapper<IEnumerable<Metadata>, IEnumerable<DTOs.V2.MetadataWithSourceV2>> metadataSourceMapper,
        IMapper<DTOs.V2.MetadataSourceV2, Metadata> sourceMapper,
        IMapper<string, ObjectId> tagIdMapper,
        IOptions<MongoConfiguration> mongoConfig)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _metadataMapper = metadataMapper ?? throw new ArgumentNullException(nameof(metadataMapper));
        _metadataSourceMapper = metadataSourceMapper ?? throw new ArgumentNullException(nameof(metadataSourceMapper));
        _sourceMapper = sourceMapper ?? throw new ArgumentNullException(nameof(sourceMapper));
        _tagIdMapper = tagIdMapper ?? throw new ArgumentNullException(nameof(tagIdMapper));

        MongoConfiguration mongoConfigValue = mongoConfig.Value;

        IMongoDatabase database = client.GetDatabase(mongoConfigValue.Database);

        _metadataCollection = database.GetCollection<Metadata>("v2_metadata");
    }

    public async Task<DTOs.V2.MetadataV2?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        Metadata? metadata = await _metadataCollection.Find(m => m.Id == ObjectId.Parse(id)).SingleOrDefaultAsync(cancellationToken);

        return _metadataMapper.Map(metadata);
    }

    public async Task<(bool Created, DTOs.V2.MetadataV2 Metadata)> GetOrCreateAsync(DTOs.V2.MetadataSourceV2 source, CancellationToken cancellationToken = default)
    {
        FilterDefinition<Metadata> filter = Builders<Metadata>.Filter.Eq(u => u.Source.Uri, source.Uri);
        Metadata? existing = await _metadataCollection.Find(filter).SingleOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            return (false, _metadataMapper.Map(existing));
        }

        Metadata newMetadata = _sourceMapper.Map(source);
        await _metadataCollection.InsertOneAsync(newMetadata, new InsertOneOptions(), cancellationToken);

        return (true, _metadataMapper.Map(newMetadata));
    }

    public async Task UpdateFromSourceAsync(string id, DTOs.V2.MetadataSourceV2 source, CancellationToken cancellationToken = default)
    {
        FilterDefinition<Metadata> filter = Builders<Metadata>.Filter.Eq(u => u.Source.Uri, source.Uri);
        Metadata? existing = await _metadataCollection.Find(filter).SingleOrDefaultAsync(cancellationToken);

        if (existing == null || existing.Id.ToString() != id)
        {
            throw new NotFoundException();
        }

        var update = Builders<Metadata>.Update;
        List<UpdateDefinition<Metadata>> updates = new();

        var titleUpdate = update
                            .Set(m => m.Title, source.Title)
                            .Set(m => m.Source.Title, source.Title);

        updates.Add(titleUpdate);

        if (existing.Source.CreatorId == null)
        {
            // TODO: Validate creator exists?
            updates.Add(update.Set(m => m.Source.CreatorId, ObjectId.Parse(source.CreatorId)));
        }

        if (string.IsNullOrEmpty(existing.Source.Id))
        {
            if (string.IsNullOrEmpty(source.Id))
                throw new Exception("Source id must be set");

            
            updates.Add(update.Set(m => m.Source.Id, source.Id));
        }

        await _metadataCollection.UpdateOneAsync(filter, update.Combine(updates), new UpdateOptions(), cancellationToken);
    }

    public async Task<bool> ExistsIdAsync(string id, CancellationToken cancellationToken = default)
    {
        FilterDefinition<Metadata> filter = Builders<Metadata>.Filter.Eq(u => u.Id, ObjectId.Parse(id));

        Metadata metadata = await _metadataCollection.Find(filter)
            .SingleOrDefaultAsync(cancellationToken);

        return metadata != null;
    }

    public async IAsyncEnumerable<DTOs.V2.MetadataV2> SearchAsync(string[] allTags, string[] anyTags, int? limit, [EnumeratorCancellation] CancellationToken cancellationToken)
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
            yield return _metadataMapper.Map(metadata);
        }
    }

    public async IAsyncEnumerable<DTOs.V2.MetadataV2> GetAllAsync(string? after, int limit, TagFilter tagFilter, [EnumeratorCancellation] CancellationToken cancellationToken)
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
            foreach (string includeTag in tagFilter.Include)
            {
                IEnumerable<DTOs.Tag> allTags = await _tagRepository.GetTagWithAncestorsAsync(new[] { includeTag }, cancellationToken);

                ObjectId[] allTagIds = allTags.Select(t => _tagIdMapper.Map(t.Id)).ToArray();

                filter &= filterBuilder.AnyIn(m => m.Tags, allTagIds);
            }
        }

        if (tagFilter.Exclude.Any())
        {
            IEnumerable<DTOs.Tag> allTags = await _tagRepository.GetTagWithAncestorsAsync(tagFilter.Exclude, cancellationToken);

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
            yield return _metadataMapper.Map(metadata);
        }
    }

    public async IAsyncEnumerable<DTOs.V2.MetadataWithSourceV2> GetSourceAllAsync(string? after, int limit, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<Metadata> filterBuilder = Builders<Metadata>.Filter;
        FilterDefinition<Metadata> filter = filterBuilder.Empty;

        SortDefinition<Metadata> order = Builders<Metadata>.Sort.Ascending(m => m.Id);

        if (!string.IsNullOrEmpty(after))
        {
            ObjectId afterId = ObjectId.Parse(after);

            filter &= Builders<Metadata>.Filter.Gt(m => m.Id, afterId);
        }

        IAsyncCursor<Metadata> cursor = await _metadataCollection
            .Find(filter)
            .Sort(order)
            .Limit(limit)
            .ToCursorAsync(cancellationToken);


        List<Metadata> list = await cursor.ToListAsync(cancellationToken);
        foreach (DTOs.V2.MetadataWithSourceV2 metadataWithSourceV2 in await _metadataSourceMapper.MapAsync(list, cancellationToken))
        {
            yield return metadataWithSourceV2;
        }
    }

    public async IAsyncEnumerable<DTOs.V2.MetadataV2> GetForCollectionAsync(DTOs.Collection collection, string sortField, bool ascending, int page, int pageSize, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<Metadata> filterBuilder = Builders<Metadata>.Filter;
        FilterDefinition<Metadata> filter = filterBuilder.Empty;

        if (collection.IncludedTags.Length != 0)
        {
            foreach (string includeTag in collection.IncludedTags)
            {
                IEnumerable<DTOs.Tag> allTags = await _tagRepository.GetTagWithAncestorsAsync(new[] { includeTag }, cancellationToken);

                ObjectId[] allTagIds = allTags.Select(t => _tagIdMapper.Map(t.Id)).ToArray();

                filter &= filterBuilder.AnyIn(m => m.Tags, allTagIds);
            }
        }

        if (collection.ExcludedTags.Length != 0)
        {
            IEnumerable<DTOs.Tag> allTags = await _tagRepository.GetTagWithAncestorsAsync(collection.ExcludedTags, cancellationToken);

            ObjectId[] allTagIds = allTags.Select(t => _tagIdMapper.Map(t.Id)).ToArray();

            filter &= filterBuilder.Not(filterBuilder.AnyIn(m => m.Tags, allTagIds));
        }

        SortDefinition<Metadata> sorting = ascending switch
        {
            true => Builders<Metadata>.Sort.Ascending(sortField),
            false => Builders<Metadata>.Sort.Descending(sortField)
        };

        IAsyncCursor<Metadata> cursor = await _metadataCollection
            .Find(filter)
            .Sort(sorting)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToCursorAsync(cancellationToken);

        foreach (Metadata metadata in cursor.ToEnumerable(cancellationToken))
        {
            yield return _metadataMapper.Map(metadata);
        }
    }
}
