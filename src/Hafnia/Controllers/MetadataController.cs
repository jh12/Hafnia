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

    [HttpPost("getOrCreate")]
    public async Task<ActionResult<Metadata>> GetMetadataOrCreate(Metadata metadata, CancellationToken cancellationToken)
    {
        (bool Created, Metadata Metadata) newMetadata = await _metadataRepository.GetOrCreateMetadataAsync(metadata, cancellationToken);

        if (!newMetadata.Created)
            return Ok(newMetadata.Metadata);

        return CreatedAtRoute("MetadataGet", new { id = newMetadata.Metadata.Id }, newMetadata.Metadata);
    }
}
