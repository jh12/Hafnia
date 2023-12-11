using Hafnia.DTOs;

namespace Hafnia.DataAccess.Repositories.V2;

public interface ICreatorRepository
{
    IAsyncEnumerable<Creator> GetAsync(CancellationToken cancellationToken = default);
    Task<Creator> GetOrCreateAsync(Creator creator, CancellationToken cancellationToken = default);
}
