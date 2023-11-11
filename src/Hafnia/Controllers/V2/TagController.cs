using Hafnia.DataAccess.Repositories.V2;
using Hafnia.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Hafnia.Controllers.V2;

[ApiController]
[Route("v2/tag")]
public class TagController : ControllerBase
{
    private readonly ITagRepository _tagRepository;

    public TagController(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
    }

    [HttpGet]
    public IAsyncEnumerable<Tag> GetAll(CancellationToken cancellationToken)
    {
        return _tagRepository.GetTagsAsync(cancellationToken);
    }
}
