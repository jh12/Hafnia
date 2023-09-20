namespace Hafnia.DataAccess.Repositories;

public interface IFileRepository
{
    // Media
    Task<(Stream Stream, string ContentType)> GetFullStreamAsync(string id, CancellationToken cancellationToken);
    Task<(Stream Stream, string ContentType)> GetThumbStreamAsync(string id, CancellationToken cancellationToken);

    Task SaveFullStreamAsync(string id, Stream stream, string contentType, CancellationToken cancellationToken);
    Task SaveThumbStreamAsync(string id, Stream stream, string contentType, CancellationToken cancellationToken);

    Task<bool> ImageExistsAsync(string id, CancellationToken cancellationToken);

    // Raw
    Task<string> GetRawTextAsync(string id, CancellationToken cancellationToken);
    Task SaveRawTextAsync(string id, string content, string contentType, CancellationToken cancellationToken);

    IAsyncEnumerable<string> ListRawIdsAsync(string pattern, CancellationToken cancellationToken);

    Task<bool> RawExistsAsync(string id, CancellationToken cancellationToken);
}
