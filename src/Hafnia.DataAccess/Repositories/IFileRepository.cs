namespace Hafnia.DataAccess.Repositories;

public interface IFileRepository
{
    // Media
    Task<(Stream Stream, string ContentType)> GetFullStreamAsync(string id, CancellationToken cancellationToken);
    Task<(Stream Stream, string ContentType)> GetThumbStreamAsync(string id, CancellationToken cancellationToken);

    Task SaveFullStreamAsync(string id, Stream stream, string contentType, CancellationToken cancellationToken);
    Task SaveThumbStreamAsync(string id, Stream stream, string contentType, CancellationToken cancellationToken);

    // Raw
    Task<string> GetRawTextAsync(string id, CancellationToken cancellationToken);
    Task<string> GetAdditionalRawTextAsync(string id, string additional, CancellationToken cancellationToken);
    Task SaveRawTextAsync(string id, string content, string contentType, CancellationToken cancellationToken);
    Task SaveAdditionalRawTextAsync(string id, string additional, string content, string contentType, CancellationToken cancellationToken);

    IAsyncEnumerable<string> ListRawIdsAsync(string pattern, CancellationToken cancellationToken);

    Task<bool> RawExistsAsync(string id, CancellationToken cancellationToken);
    Task<bool> RawAdditionalExistsAsync(string id, string additional, CancellationToken cancellationToken);
}
