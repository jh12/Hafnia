using Hafnia.DataAccess.Exceptions;
using Hafnia.DataAccess.Repositories.V2;
using Hafnia.DTOs;
using Hafnia.DTOs.V2;
using Microsoft.AspNetCore.Mvc;

namespace Hafnia.Controllers.V2;

[ApiController]
[Route("v2/collection")]
public class CollectionController : ControllerBase
{
    private readonly ICollectionRepository _collectionRepository;

    public CollectionController(ICollectionRepository collectionRepository)
    {
        _collectionRepository = collectionRepository;
    }

    [HttpGet]
    public Task<ActionResult<Collection>> Get(CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<Collection>>(Ok(_collectionRepository.GetAsync(cancellationToken)));
    }

    [HttpGet("{id}/children")]
    public Task<ActionResult<Collection>> GetChildren(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<Collection>>(Ok(_collectionRepository.GetChildrenAsync(id, cancellationToken)));
    }

    [HttpGet("{id}/content")]
    public Task<ActionResult<Collection>> GetContent(string id, string sortingField = "id", bool ascending = true, int page = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            IAsyncEnumerable<MetadataV2> content = _collectionRepository.GetContentAsync(id, sortingField, ascending, page, pageSize, cancellationToken);
            return Task.FromResult<ActionResult<Collection>>(Ok(content));
        }
        catch (NotFoundException)
        {
            return Task.FromResult<ActionResult<Collection>>(NotFound());
        }
    }

    [HttpPost("cache/clear")]
    public async Task<ActionResult> ClearCache()
    {
        await _collectionRepository.ClearCacheAsync();

        return Ok();
    }
}
