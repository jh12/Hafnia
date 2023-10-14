using Hafnia.DTOs;

namespace Hafnia.DataAccess.Repositories;

public interface IWorkRepository
{
    IAsyncEnumerable<MetadataWork> GetAsync(string origin, string[] ids, CancellationToken cancellationToken);
    IAsyncEnumerable<MetadataWork> GetOrCreateAsync(string origin, MetadataWork[] list, bool excludeCompleted, CancellationToken cancellationToken);
    IAsyncEnumerable<MetadataWork> GetWorkAsync(string origin, DateTime? updatedAfter, int limit, CancellationToken cancellationToken);
    Task UpdateAsync(string origin, string id, MetadataWork item, CancellationToken cancellationToken);
}
