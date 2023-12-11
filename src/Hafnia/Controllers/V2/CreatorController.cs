using Hafnia.DataAccess.Repositories.V2;
using Hafnia.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Hafnia.Controllers.V2;

[ApiController]
[Route("v2/creator")]
public class CreatorController : ControllerBase
{
    private readonly ICreatorRepository _creatorRepository;

    public CreatorController(ICreatorRepository creatorRepository)
    {
        _creatorRepository = creatorRepository ?? throw new ArgumentNullException(nameof(creatorRepository));
    }

    [HttpGet]
    public Task<ActionResult<Creator>> Get(CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<Creator>>(Ok(_creatorRepository.GetAsync(cancellationToken)));
    }
}
