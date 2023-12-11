using System.Net.Http.Json;
using Hafnia.DTOs;

namespace Hafnia.Frontend.Repositories;

internal class TagRepository
{
    private readonly HttpClient _httpClient;

    private TimeSpan _refreshInterval = TimeSpan.FromMinutes(5);
    private DateTime? _lastRefreshTime;
    private Tag[] _tags = Array.Empty<Tag>();

    private SemaphoreSlim _semaphore = new(1);

    public TagRepository(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<Tag[]> GetAllTagsAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (_lastRefreshTime.HasValue && DateTime.UtcNow < _lastRefreshTime.Value.Add(_refreshInterval))
                return _tags;

            _tags = (await _httpClient.GetFromJsonAsync<Tag[]>("v2/tag"))!;
            _lastRefreshTime = DateTime.UtcNow;

            return _tags;
        }
        finally
        {
            _semaphore.Release();
        }

        
    }
}
