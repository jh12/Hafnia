using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Hafnia.DataAccess.DataStructures;
using Hafnia.DataAccess.MongoDB.Config;
using Hafnia.DataAccess.MongoDB.Mappers.V2;
using Hafnia.DataAccess.Repositories.V2;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Tag = Hafnia.DataAccess.MongoDB.Models.V2.Tag;

namespace Hafnia.DataAccess.MongoDB.Repositories.V2;

internal class TagRepository : ITagRepository
{
    private readonly IMongoCollection<Tag> _tagCollection;
    private readonly IMapper<Tag, DTOs.Tag> _dtoMapper;

    private List<Tag> _tags = new();
    private Tree<Tag>? _tagTree;

    private SemaphoreSlim _treeSemaphore = new(1);
    private SemaphoreSlim _semaphore = new(1);

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

    public async Task<IEnumerable<DTOs.Tag>> GetTagWithChildrenAsync(IEnumerable<string> rootTags, CancellationToken cancellationToken = default)
    {
        HashSet<ObjectId> rootTagIds = rootTags.Select(ObjectId.Parse).ToHashSet();

        Tree<Tag> tree = await GetTree(cancellationToken);

        List<Tag> tags = tree.GetDataFromDescendants(t => rootTagIds.Contains(t.Id)).ToList();

        return tags.Select(_dtoMapper.Map).ToArray();
    }

    private async Task<Tree<Tag>> GetTree(CancellationToken cancellationToken)
    {
        await _treeSemaphore.WaitAsync(cancellationToken);

        try
        {
            if (_tagTree != null)
                return _tagTree;

            Tree<Tag> tree = new Tree<Tag>();

            Dictionary<ObjectId, TreeNode<Tag>> treeMap = (await InternalGetTagsAsync(cancellationToken)).Select(t => new TreeNode<Tag>(t)).ToDictionary(e => e.Data.Id);
            HashSet<ObjectId> tagsWithReferences = new HashSet<ObjectId>(treeMap.Keys);

            foreach (TreeNode<Tag> subTree in treeMap.Values)
            {
                foreach (ObjectId childTag in subTree.Data.Children)
                {
                    subTree.AddChild(treeMap[childTag]);
                    tagsWithReferences.Remove(childTag);
                }
            }

            foreach (ObjectId rootTags in tagsWithReferences)
            {
                tree.AddChild(treeMap[rootTags]);
            }

            return tree;
        }
        finally
        {
            _treeSemaphore.Release();
        }

    }

    // TODO: Cache
    internal async Task<IEnumerable<Tag>> InternalGetTagsAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_tags.Any())
                return _tags.ToImmutableArray();

            List<Tag> tags = await _tagCollection
                .Find(FilterDefinition<Tag>.Empty)
                .ToListAsync(cancellationToken);

            _tags = tags;

            return _tags.ToImmutableArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // TODO: For development
    internal async Task TryInternalCreateTagAsync(string name, string? parent = null, params string[]? patterns)
    {
        Tag? existing = await _tagCollection.Find(t => t.Name == name).SingleOrDefaultAsync();

        if (existing != null)
        {
            FilterDefinition<Tag> idFilter = Builders<Tag>.Filter.Eq(t => t.Id, existing.Id);
            UpdateDefinition<Tag> addPatterns = Builders<Tag>.Update.AddToSetEach(t => t.Patterns, patterns);

            await _tagCollection.UpdateOneAsync(idFilter, addPatterns);

            return;
        }

        Tag? parentTag = null;

        if (parent != null)
        {
            parentTag = await _tagCollection
                .Find(t => t.Name == parent)
                .SingleOrDefaultAsync();

            if(parentTag == null)
                throw new InvalidOperationException("Parent didn't exist"); // TODO: Better error
        }

        Tag tag = new Tag(ObjectId.GenerateNewId(), name, Array.Empty<ObjectId>(), patterns ?? Array.Empty<string>());
        await _tagCollection.InsertOneAsync(tag);

        if(parent != null)
        {
            UpdateDefinition<Tag> addChild = Builders<Tag>.Update.AddToSet(t => t.Children, tag.Id);

            await _tagCollection
                .UpdateOneAsync(f => f.Id == parentTag!.Id, addChild);
        }
    }
}
