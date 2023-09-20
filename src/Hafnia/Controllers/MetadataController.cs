using Hafnia.DataAccess.Repositories;
using Hafnia.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Hafnia.Controllers;

[ApiController]
[Route("metadata")]
public class MetadataController : ControllerBase
{
    private readonly IMetadataRepository _metadataRepository;

    public MetadataController(IMetadataRepository metadataRepository)
    {
        _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
    }

    [HttpGet("getId")]
    [Obsolete($"Use {nameof(GetMetadataFromUrl)} instead")]
    public async Task<string> GetIdFromUrl(Uri uri, CancellationToken cancellationToken)
    {
        return await _metadataRepository.GetIdFromUrlAsync(uri, cancellationToken);
    }

    [HttpGet("getMetadataByUrl")]
    public async Task<Metadata> GetMetadataFromUrl(Uri url, CancellationToken cancellationToken)
    {
        return await _metadataRepository.GetMetadataFromUrlAsync(url, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Metadata>> Get(string id, CancellationToken cancellationToken)
    {
        Metadata? metadata = await _metadataRepository.GetAsync(id, cancellationToken);

        if (metadata == null)
            return NotFound();

        return Ok(metadata);
    }

    [HttpPut("{id}/tags")]
    public async Task<IActionResult> PutTags(string id, [FromBody] string[] tags, CancellationToken cancellationToken)
    {
        await _metadataRepository.SetTagsAsync(id, tags, cancellationToken);

        return Ok();
    }

    [HttpGet("{id}/tags")]
    public async Task<ActionResult<string[]>> GetTags(string id, CancellationToken cancellationToken)
    {
        Metadata? metadata = await _metadataRepository.GetAsync(id, cancellationToken);

        if (metadata == null)
            return NotFound();

        return Ok(metadata.Tags);
    }

    [HttpPut("{id}/flags/hasFile")]
    public async Task<IActionResult> PutHasFile(string id, CancellationToken cancellationToken)
    {
        await _metadataRepository.SetHasFileAsync(id, true, cancellationToken);

        return Ok();
    }

    [HttpPut("{id}/flags/hasThumbnail")]
    public async Task<IActionResult> PutHasThumbnail(string id, CancellationToken cancellationToken)
    {
        await _metadataRepository.SetHasThumbnailAsync(id, true, cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Search metadata
    /// </summary>
    /// <param name="limit">Limit result to x entries</param>
    /// <param name="allTags">Comma separated list of tags, all must match</param>
    /// <param name="anyTags">Comma separated list of tags, any must match</param>
    [HttpGet("search")]
    [ProducesResponseType(typeof(Metadata[]), StatusCodes.Status200OK)]
    public Task<IActionResult> Search(int? limit = 100, string? allTags = null, string? anyTags = null, CancellationToken cancellationToken = default)
    {
        if (limit > 10_000)
            return Task.FromResult<IActionResult>(BadRequest($"{nameof(limit)} cannot be greater than 10,000"));

        string[] allTagArray = string.IsNullOrWhiteSpace(allTags) ? Array.Empty<string>() : allTags.Split(",", StringSplitOptions.TrimEntries);
        string[] anyTagArray = string.IsNullOrWhiteSpace(anyTags) ? Array.Empty<string>() : anyTags.Split(",", StringSplitOptions.TrimEntries);

        return Task.FromResult<IActionResult>(Ok(_metadataRepository.SearchAsync(allTagArray, anyTagArray, limit, cancellationToken)));
    }
}
