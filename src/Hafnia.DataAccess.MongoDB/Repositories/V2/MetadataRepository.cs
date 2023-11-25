using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Hafnia.DataAccess.Models;
using Hafnia.DataAccess.MongoDB.Cache;
using Hafnia.DataAccess.MongoDB.Config;
using Hafnia.DataAccess.MongoDB.Mappers.V2;
using Hafnia.DataAccess.Repositories.V2;
using Hafnia.DTOs;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Metadata = Hafnia.DataAccess.MongoDB.Models.V2.Metadata;
using Tag = Hafnia.DataAccess.MongoDB.Models.V2.Tag;

namespace Hafnia.DataAccess.MongoDB.Repositories.V2;

internal class MetadataRepository : IMetadataRepository
{
    private readonly ITagRepository _tagRepository;
    private readonly IEntityCache<Tag> _tagCache;
    private readonly IMapper<Metadata, DTOs.Metadata> _metadataMapper;
    private readonly IMapper<string, ObjectId> _tagIdMapper;
    private readonly IMapper<Tag, DTOs.Tag> _tagMapper;
    private readonly IMongoCollection<Metadata> _metadataCollection;

    public MetadataRepository(
        IMongoClient client,
        ITagRepository tagRepository,
        IEntityCache<Tag> tagCache,
        IMapper<Metadata, DTOs.Metadata> metadataMapper,
        IMapper<string, ObjectId> tagIdMapper,
        IMapper<Tag, DTOs.Tag> tagMapper,
        IOptions<MongoConfiguration> mongoConfig)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _tagCache = tagCache ?? throw new ArgumentNullException(nameof(tagCache));
        _metadataMapper = metadataMapper ?? throw new ArgumentNullException(nameof(metadataMapper));
        _tagIdMapper = tagIdMapper ?? throw new ArgumentNullException(nameof(tagIdMapper));
        _tagMapper = tagMapper ?? throw new ArgumentNullException(nameof(tagMapper));

        MongoConfiguration mongoConfigValue = mongoConfig.Value;

        IMongoDatabase database = client.GetDatabase(mongoConfigValue.Database);

        _metadataCollection = database.GetCollection<Metadata>("v2_metadata");
    }

    public async Task<DTOs.Metadata?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        Metadata? metadata = await _metadataCollection.Find(m => m.Id == ObjectId.Parse(id)).SingleOrDefaultAsync(cancellationToken);

        return _metadataMapper.Map(metadata);
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
            yield return _metadataMapper.Map(metadata);
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

    public async IAsyncEnumerable<MetadataWithSuggestedTags> GetTagSuggestionsAsync(string[] ids, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IEnumerable<Tag> tags = await _tagCache.GetAsync(cancellationToken);
        List<(Tag Tag, Regex Pattern)> tagPatterns = tags.SelectMany(t => t.Patterns.Select(p => (Tag: t, Pattern: new Regex(p, RegexOptions.IgnoreCase)))).ToList();

        FilterDefinition<Metadata> idFilter = Builders<Metadata>.Filter.In(m => m.Id, ids.Select(ObjectId.Parse));

        IAsyncCursor<Metadata> cursor = await _metadataCollection
            .Find(idFilter)
            .ToCursorAsync(cancellationToken);

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (Metadata metadata in cursor.Current)
            {
                HashSet<ObjectId> found = new HashSet<ObjectId>();

                foreach (string sourceTag in metadata.Source.Tags)
                {
                    IEnumerable<Tag> foundTags =
                        tagPatterns.Where(t => t.Pattern.IsMatch(sourceTag)).Select(p => p.Tag);
                    found.UnionWith(foundTags.Select(t => t.Id));
                }

                DTOs.Metadata map = _metadataMapper.Map(metadata);

                yield return new MetadataWithSuggestedTags
                (
                    map.Id,
                    map.OriginalId,
                    map.Uri,
                    map.Title,
                    map.Tags,
                    found.Select(f => f.ToString()).ToArray()
                );
            }
        }
    }
}
