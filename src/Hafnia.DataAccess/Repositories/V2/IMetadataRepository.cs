using Hafnia.DataAccess.Models;
using Hafnia.DTOs;
using Hafnia.DTOs.V2;

namespace Hafnia.DataAccess.Repositories.V2;

public interface IMetadataRepository
{
    Task<MetadataV2?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<(bool Created, MetadataV2 Metadata)> GetOrCreateAsync(MetadataSourceV2 source, CancellationToken cancellationToken = default);
    Task<bool> ExistsIdAsync(string id, CancellationToken cancellationToken = default);

    Task UpdateFromSourceAsync(string id, MetadataSourceV2 source, CancellationToken cancellationToken = default);

    IAsyncEnumerable<MetadataV2> SearchAsync(string[] allTags, string[] anyTags, int? limit, CancellationToken cancellationToken = default);

    IAsyncEnumerable<MetadataV2> GetAllAsync(string? after, int limit, TagFilter tagFilter, CancellationToken cancellation = default);
    IAsyncEnumerable<MetadataWithSourceV2> GetSourceAllAsync(string? after, int limit, CancellationToken cancellationToken);

    IAsyncEnumerable<MetadataV2> GetForCollectionAsync(Collection collection, string sortField, bool ascending, int page, int pageSize, CancellationToken cancellationToken);
}
