using System.Net.Http.Json;
using Hafnia.DTOs;

namespace Hafnia.Frontend.Repositories;

internal class TagRepository
{
    private readonly HttpClient _httpClient;

    public TagRepository(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public Task<Tag[]> GetAllTagsAsync()
    {
        return _httpClient.GetFromJsonAsync<Tag[]>("v2/tag")!;
    }
}
