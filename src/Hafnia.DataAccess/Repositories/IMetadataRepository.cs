using Hafnia.DTOs;

namespace Hafnia.DataAccess.Repositories;

public interface IMetadataRepository
{
    [Obsolete($"Use {nameof(GetMetadataFromUrlAsync)} instead")]
    Task<string> GetIdFromUrlAsync(Uri uri, CancellationToken cancellationToken = default);
    Task<Metadata> GetMetadataFromUrlAsync(Uri uri, CancellationToken cancellationToken = default);

    Task<bool> ExistsIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Metadata?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task SetTagsAsync(string id, string[] tags, CancellationToken cancellationToken = default);
    Task SetHasFileAsync(string id, bool hasFile, CancellationToken cancellationToken = default);
    Task SetHasThumbnailAsync(string id, bool hasThumbnail, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Metadata> SearchAsync(string[] allTags, string[] anyTags, int? limit, CancellationToken cancellationToken = default);

    // TODO: For testing, convert to get sample?
    IAsyncEnumerable<string> GetRandomIdsAsync(int size, CancellationToken cancellationToken = default);
}
