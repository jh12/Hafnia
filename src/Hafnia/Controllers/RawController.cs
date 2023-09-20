using System.Text;
using Hafnia.DataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Hafnia.Controllers;

[ApiController]
[Route("raw")]
public class RawController : ControllerBase
{
    private readonly IFileRepository _fileRepository;
    private readonly IMetadataRepository _metadataRepository;

    public RawController(IFileRepository fileRepository, IMetadataRepository metadataRepository)
    {
        _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
        _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
    }

    [HttpGet("{id}")]
    public async Task<string> GetRaw(string id, CancellationToken cancellationToken)
    {
        return await _fileRepository.GetRawTextAsync(id, cancellationToken);
    }

    [HttpGet]
    public Task<IActionResult> ListRawIds([FromQuery] string? pattern, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return Task.FromResult<IActionResult>(BadRequest($"{nameof(pattern)} cannot be empty"));

        return Task.FromResult<IActionResult>(Ok(_fileRepository.ListRawIdsAsync(pattern, cancellationToken)));
    }

    [HttpGet("{id}/exists")]
    public async Task<IActionResult> HasRaw(string id, CancellationToken cancellationToken)
    {
        if (await _fileRepository.RawExistsAsync(id, cancellationToken))
            return Ok();

        return NotFound();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutRaw(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(Request.ContentType))
            return BadRequest("ContentType cannot be empty");

        try
        {
            if (!await _metadataRepository.ExistsIdAsync(id, cancellationToken))
            {
                return NotFound();
            }

            using StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8);
            string content = await reader.ReadToEndAsync(cancellationToken);

            await _fileRepository.SaveRawTextAsync(id, content, Request.ContentType, cancellationToken);
        }
        catch (Exception)
        {
            return BadRequest(); // TODO: Better error
        }

        return Ok();
    }
}
