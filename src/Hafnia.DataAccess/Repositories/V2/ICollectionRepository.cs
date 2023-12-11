using Hafnia.DTOs;
using Hafnia.DTOs.V2;

namespace Hafnia.DataAccess.Repositories.V2;

public interface ICollectionRepository
{
    IAsyncEnumerable<Collection> GetAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<Collection> GetChildrenAsync(string id, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MetadataV2> GetContentAsync(string id, string sortField, bool ascending, int page, int pageSize, CancellationToken cancellationToken = default);

    Task ClearCacheAsync();
}
