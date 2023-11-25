using Hafnia.DataAccess.Repositories;
using Hafnia.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Hafnia.Controllers;

[ApiController]
[Route("work")]
public class WorkController : ControllerBase
{
    private readonly IWorkRepository _workRepository;

    public WorkController(IWorkRepository workRepository)
    {
        _workRepository = workRepository ?? throw new ArgumentNullException(nameof(workRepository));
    }

    // TODO: Change origin to user id?
    [HttpGet("metadata/{origin}/get")]
    public Task<ActionResult<MetadataWork>> Get(string ids, string origin, CancellationToken cancellationToken)
    {
        string[] splitIds = ids.Split(",", StringSplitOptions.TrimEntries);

        return Task.FromResult<ActionResult<MetadataWork>>(Ok(_workRepository.GetAsync(origin, splitIds, cancellationToken)));
    }

    [HttpPost("metadata/{origin}/getOrCreateMany")]
    public Task<ActionResult<MetadataWork>> GetOrCreateMany(string origin, [FromBody] MetadataWork[] list, bool excludeCompleted, CancellationToken cancellationToken)
    {
        if (list.Any(l => l.Origin != origin))
            return Task.FromResult<ActionResult<MetadataWork>>(BadRequest($"{nameof(origin)} wasn't equal on all items in {nameof(list)}"));

        return Task.FromResult<ActionResult<MetadataWork>>(Ok(_workRepository.GetOrCreateAsync(origin, list, excludeCompleted, cancellationToken)));
    }

    [HttpGet("metadata/{origin}")]
    public Task<ActionResult<MetadataWork>> GetWork(string origin, DateTime? updatedAfter, int limit = 1000, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ActionResult<MetadataWork>>(Ok(_workRepository.GetWorkAsync(origin, updatedAfter, limit, cancellationToken)));
    }

    [HttpPut("metadata/{origin}")]
    public async Task<IActionResult> Update(string origin, string id, [FromBody] MetadataWork item, CancellationToken cancellationToken)
    {
        if (id != item.Id)
            return BadRequest("Id in path and in body must be equal");

        await _workRepository.UpdateAsync(origin, id, item, cancellationToken);

        return Ok();
    }
}
