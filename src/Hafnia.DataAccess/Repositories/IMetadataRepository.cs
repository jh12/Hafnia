using Hafnia.DTOs;

namespace Hafnia.DataAccess.Repositories;

public interface IMetadataRepository
{
    Task<(bool Created, Metadata Metadata)> GetOrCreateMetadataAsync(Metadata metadata, CancellationToken cancellationToken = default);

    Task<bool> ExistsIdAsync(string id, CancellationToken cancellationToken = default);

    Task SetHasFileAsync(string id, bool hasFile, CancellationToken cancellationToken = default);
    Task SetHasThumbnailAsync(string id, bool hasThumbnail, CancellationToken cancellationToken = default);
}
