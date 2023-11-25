using Hafnia.DTOs;

namespace Hafnia.DataAccess.Repositories.V2;

public interface ICollectionRepository
{
    IAsyncEnumerable<DTOs.Collection> GetAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<Metadata> GetContentAsync(string id, string sortField, bool ascending, int page, int pageSize, CancellationToken cancellationToken = default);

    Task ClearCacheAsync();
}
