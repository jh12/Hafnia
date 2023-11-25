using Hafnia.DTOs;

namespace Hafnia.DataAccess.Repositories.V2;

public interface ITagRepository
{
    IAsyncEnumerable<Tag> GetTagsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<Tag>> GetTagWithAncestorsAsync(IEnumerable<string> tagIds, CancellationToken cancellationToken = default);
}
