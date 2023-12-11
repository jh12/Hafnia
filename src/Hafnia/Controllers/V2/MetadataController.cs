using Hafnia.DataAccess.Exceptions;
using Hafnia.DataAccess.Models;
using Hafnia.DataAccess.Repositories.V2;
using Hafnia.DTOs.V2;
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
    public async Task<ActionResult<MetadataV2>> Get(string id, CancellationToken cancellationToken)
    {
        var metadata = await _metadataRepository.GetAsync(id, cancellationToken);

        if (metadata == null)
            return NotFound();

        return Ok(metadata);
    }

    [HttpPost("getOrCreate")]
    public async Task<ActionResult<MetadataV2>> GetMetadataOrCreate(MetadataSourceV2 metadataSource, CancellationToken cancellationToken)
    {
        (bool Created, MetadataV2 Metadata) newMetadata = await _metadataRepository.GetOrCreateAsync(metadataSource, cancellationToken);

        if (!newMetadata.Created)
            return Ok(newMetadata.Metadata);

        return CreatedAtRoute("MetadataGet", new { id = newMetadata.Metadata.Id }, newMetadata.Metadata);
    }

    [HttpPut("{id}/source")]
    public async Task<IActionResult> UpdateMetadata(string id, MetadataSourceV2 source, CancellationToken cancellationToken)
    {
        try
        {
            await _metadataRepository.UpdateFromSourceAsync(id, source, cancellationToken);
        }
        catch (NotFoundException)
        {
            return BadRequest();
        }

        return Ok();
    }

    /// <summary>
    /// Search metadata
    /// </summary>
    /// <param name="limit">Limit result to x entries</param>
    /// <param name="allTags">Comma separated list of tags, all must match</param>
    /// <param name="anyTags">Comma separated list of tags, any must match</param>
    [HttpGet("search")]
    [ProducesResponseType(typeof(MetadataV2[]), StatusCodes.Status200OK)]
    public Task<ActionResult<MetadataV2>> Search(int? limit = 100, string? allTags = null, string? anyTags = null, CancellationToken cancellationToken = default)
    {
        if (limit > 10_000)
            return Task.FromResult<ActionResult<MetadataV2>>(BadRequest($"{nameof(limit)} cannot be greater than 10,000"));

        var allTagArray = string.IsNullOrWhiteSpace(allTags) ? Array.Empty<string>() : allTags.Split(",", StringSplitOptions.TrimEntries);
        var anyTagArray = string.IsNullOrWhiteSpace(anyTags) ? Array.Empty<string>() : anyTags.Split(",", StringSplitOptions.TrimEntries);

        return Task.FromResult<ActionResult<MetadataV2>>(Ok(_metadataRepository.SearchAsync(allTagArray, anyTagArray, limit, cancellationToken)));
    }

    [HttpGet("all")]
    public IAsyncEnumerable<MetadataV2> GetAll(string? after, int limit, string? tagInclude = null, string? tagExclude = null, CancellationToken cancellationToken = default)
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

    [HttpGet("source/all")]
    public IAsyncEnumerable<MetadataWithSourceV2> GetAll(string? after, int limit, CancellationToken cancellationToken = default)
    {
        if (limit < 1)
            limit = 0;

        if (limit > 1000)
            limit = 1000;

        return _metadataRepository.GetSourceAllAsync(after, limit, cancellationToken);
    }
}
