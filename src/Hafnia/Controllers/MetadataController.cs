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
    public async Task<ActionResult<string>> GetIdFromUrl(Uri uri, CancellationToken cancellationToken)
    {
        string? id = await _metadataRepository.GetIdFromUrlAsync(uri, cancellationToken);

        if (id != null)
            return Ok(id);

        return NotFound();
    }

    [HttpGet("getMetadataByUrl")]
    [Obsolete($"Use {nameof(GetMetadataOrCreate)} instead")]
    public async Task<Metadata> GetMetadataFromUrl(Uri url, CancellationToken cancellationToken)
    {
        return await _metadataRepository.GetMetadataFromUrlAsync(url, cancellationToken);
    }

    [HttpPost("getOrCreate")]
    public async Task<ActionResult<Metadata>> GetMetadataOrCreate(Metadata metadata, CancellationToken cancellationToken)
    {
        (bool Created, Metadata Metadata) newMetadata = await _metadataRepository.GetOrCreateMetadataAsync(metadata, cancellationToken);

        if (!newMetadata.Created)
            return Ok(newMetadata.Metadata);

        return CreatedAtAction(nameof(Get), new { id = newMetadata.Metadata.Id }, newMetadata.Metadata);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Metadata>> Get(string id, CancellationToken cancellationToken)
    {
        Metadata? metadata = await _metadataRepository.GetAsync(id, cancellationToken);

        if (metadata == null)
            return NotFound();

        return Ok(metadata);
    }

    [HttpPost("{id}/update")]
    public async Task<IActionResult> Update(string id, Metadata metadata, CancellationToken cancellationToken)
    {
        await _metadataRepository.UpdateAsync(id, metadata, cancellationToken);

        return Ok();
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

    [HttpGet("tags")]
    public async Task<string[]> GetAllTags(CancellationToken cancellationToken = default)
    {
        return await _metadataRepository.GetAllTags(cancellationToken);
    }

    [HttpGet("tags/search")]
    public async Task<string[]> SearchTags(string? allTags = null, string? anyTags = null, CancellationToken cancellationToken = default)
    {
        string[] allTagArray = string.IsNullOrWhiteSpace(allTags) ? Array.Empty<string>() : allTags.Split(",", StringSplitOptions.TrimEntries);
        string[] anyTagArray = string.IsNullOrWhiteSpace(anyTags) ? Array.Empty<string>() : anyTags.Split(",", StringSplitOptions.TrimEntries);

        return await _metadataRepository.SearchTags(allTagArray, anyTagArray, cancellationToken);
    }

    // TODO: Somehow limit
    [HttpGet("all")]
    public IAsyncEnumerable<Metadata> GetAll(CancellationToken cancellationToken)
    {
        return _metadataRepository.GetAllAsync(cancellationToken);
    }
}
