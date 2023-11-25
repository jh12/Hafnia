using Hafnia.DataAccess.Models;
using Hafnia.DataAccess.Repositories.V2;
using Hafnia.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Hafnia.Controllers.V2;

[ApiController]
[Route("v2/metadata")]
public class MetadataController : ControllerBase
{
    private readonly IMetadataRepository _metadataRepository;

    public MetadataController(IMetadataRepository metadataRepository)
    {
        _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
    }

    [HttpGet("{id}", Name = "MetadataGet")]
    public async Task<ActionResult<Metadata>> Get(string id, CancellationToken cancellationToken)
    {
        var metadata = await _metadataRepository.GetAsync(id, cancellationToken);

        if (metadata == null)
            return NotFound();

        return Ok(metadata);
    }

    [HttpGet("tags/suggest")]
    public Task<ActionResult<MetadataWithSuggestedTags>> SuggestTags(string ids, CancellationToken cancellationToken)
    {
        string[] metadataArray = string.IsNullOrWhiteSpace(ids) ? Array.Empty<string>() : ids.Split(",", StringSplitOptions.TrimEntries);


        return Task.FromResult<ActionResult<MetadataWithSuggestedTags>>(Ok(_metadataRepository.GetTagSuggestionsAsync(metadataArray, cancellationToken)));
    }

    /// <summary>
    /// Search metadata
    /// </summary>
    /// <param name="limit">Limit result to x entries</param>
    /// <param name="allTags">Comma separated list of tags, all must match</param>
    /// <param name="anyTags">Comma separated list of tags, any must match</param>
    [HttpGet("search")]
    [ProducesResponseType(typeof(Metadata[]), StatusCodes.Status200OK)]
    public Task<ActionResult<Metadata>> Search(int? limit = 100, string? allTags = null, string? anyTags = null, CancellationToken cancellationToken = default)
    {
        if (limit > 10_000)
            return Task.FromResult<ActionResult<Metadata>>(BadRequest($"{nameof(limit)} cannot be greater than 10,000"));

        var allTagArray = string.IsNullOrWhiteSpace(allTags) ? Array.Empty<string>() : allTags.Split(",", StringSplitOptions.TrimEntries);
        var anyTagArray = string.IsNullOrWhiteSpace(anyTags) ? Array.Empty<string>() : anyTags.Split(",", StringSplitOptions.TrimEntries);

        return Task.FromResult<ActionResult<Metadata>>(Ok(_metadataRepository.SearchAsync(allTagArray, anyTagArray, limit, cancellationToken)));
    }

    [HttpGet("all")]
    public IAsyncEnumerable<Metadata> GetAll(string? after, int limit, string? tagInclude = null, string? tagExclude = null, CancellationToken cancellationToken = default)
    {
        if (limit < 1)
            limit = 0;

        if (limit > 1000)
            limit = 1000;

        var tagIncludeArray = string.IsNullOrWhiteSpace(tagInclude) ? Array.Empty<string>() : tagInclude.Split(",", StringSplitOptions.TrimEntries);
        var tagExcludeArray = string.IsNullOrWhiteSpace(tagExclude) ? Array.Empty<string>() : tagExclude.Split(",", StringSplitOptions.TrimEntries);

        TagFilter tagFilter = new TagFilter(tagIncludeArray, tagExcludeArray);

        return _metadataRepository.GetAllAsync(after, limit, tagFilter, cancellationToken);
    }
}
