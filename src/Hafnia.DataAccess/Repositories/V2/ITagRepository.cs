using Hafnia.DTOs;

namespace Hafnia.DataAccess.Repositories.V2;

public interface ITagRepository
{
    IAsyncEnumerable<Tag> GetTagsAsync(CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Tag>> GetTagWithChildrenAsync(IEnumerable<string> rootTags, CancellationToken cancellationToken = default);
}
