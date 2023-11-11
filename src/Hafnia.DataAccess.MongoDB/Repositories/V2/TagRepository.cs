using System.Runtime.CompilerServices;
using Hafnia.DataAccess.MongoDB.Cache;
using Hafnia.DataAccess.MongoDB.Config;
using Hafnia.DataAccess.MongoDB.Mappers.V2;
using Hafnia.DataAccess.Repositories.V2;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Tag = Hafnia.DataAccess.MongoDB.Models.V2.Tag;

namespace Hafnia.DataAccess.MongoDB.Repositories.V2;

internal class TagRepository : EntityCacheBase<Tag>, ITagRepository
{
    private readonly IMongoCollection<Tag> _tagCollection;
    private readonly IMapper<Tag, DTOs.Tag> _dtoMapper;

    public TagRepository(IMongoClient client, IOptions<MongoConfiguration> mongoConfig, IMapper<Tag, DTOs.Tag> dtoMapper)
    {
        MongoConfiguration mongoConfigValue = mongoConfig.Value;

        IMongoDatabase database = client.GetDatabase(mongoConfigValue.Database);
        _tagCollection = database.GetCollection<Tag>("v2_tag");

        _dtoMapper = dtoMapper ?? throw new ArgumentNullException(nameof(dtoMapper));
    }

    public async IAsyncEnumerable<DTOs.Tag> GetTagsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<Tag> tags = await _tagCollection
            .Find(FilterDefinition<Tag>.Empty)
            .ToListAsync(cancellationToken);

        foreach (Tag tag in tags)
        {
            yield return _dtoMapper.Map(tag);
        }
    }

    public async Task<IEnumerable<DTOs.Tag>> GetTagWithAncestorsAsync(IEnumerable<string> tagIds, CancellationToken cancellationToken = default)
    {
        IEnumerable<Tag> tags = await GetAsync(cancellationToken);

        HashSet<ObjectId> ids = tagIds.Select(ObjectId.Parse).ToHashSet();

        return tags
            .Where(t => t.AncestorsAndSelf.Any(a => ids.Contains(a)))
            .Select(_dtoMapper.Map)
            .ToArray();
    }

    protected override async Task<IEnumerable<Tag>> GetInnerAsync(CancellationToken cancellationToken)
    {
        return await _tagCollection
            .Find(FilterDefinition<Tag>.Empty)
            .ToListAsync(cancellationToken);
    }
}
