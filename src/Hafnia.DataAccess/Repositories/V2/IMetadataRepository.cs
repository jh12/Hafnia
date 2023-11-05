using Hafnia.DataAccess.Models;
using Hafnia.DTOs;

namespace Hafnia.DataAccess.Repositories.V2;

public interface IMetadataRepository
{
    Task<Metadata?> GetAsync(string id, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Metadata> SearchAsync(string[] allTags, string[] anyTags, int? limit, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Metadata> GetAllAsync(string? after, int limit, TagFilter tagFilter, CancellationToken cancellation = default);
}
