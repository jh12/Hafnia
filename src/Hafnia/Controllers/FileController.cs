using Hafnia.DataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IO;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using Exceptions = Hafnia.DataAccess.Exceptions;

namespace Hafnia.Controllers;

[ApiController]
[Route("media/file")]
public class FileController : ControllerBase
{
    private readonly IFileRepository _fileRepository;
    private readonly DataAccess.Repositories.V2.IMetadataRepository _metadataRepository;
    private readonly Serilog.ILogger _logger;
    private static readonly RecyclableMemoryStreamManager MemoryManager = new();

    public FileController(IFileRepository fileRepository, DataAccess.Repositories.V2.IMetadataRepository metadataRepository, Serilog.ILogger logger)
    {
        _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
        _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMedia(string id, CancellationToken cancellationToken)
    {
        try
        {
            var (stream, contentType) = await _fileRepository.GetFullStreamAsync(id, cancellationToken);

            return File(stream, contentType);
        }
        catch (Exceptions.FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id}/thumbnail")]
    public async Task<IActionResult> GetMediaThumbnail(string id, CancellationToken cancellationToken)
    {
        try
        {
            var (stream, contentType) = await _fileRepository.GetThumbStreamAsync(id, cancellationToken);

            return File(stream, contentType);
        }
        catch (Exceptions.FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}")]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> PutMedia(string id, CancellationToken cancellationToken)
    {
        await using MemoryStream stream = MemoryManager.GetStream();
        await Request.Body.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        return await HandleMedia(id, stream, cancellationToken);
    }

    [HttpPut]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> PutMedia(IFormFileCollection formFiles, CancellationToken cancellationToken)
    {
        foreach (IFormFile formFile in formFiles)
        {
            string fileName = formFile.FileName.Split('.')[0];

            await using Stream readStream = formFile.OpenReadStream();
            await HandleMedia(fileName, readStream, cancellationToken);
        }

        return Ok();
    }

    private async Task<IActionResult> HandleMedia(string id, Stream stream, CancellationToken cancellationToken)
    {
        if (!await _metadataRepository.ExistsIdAsync(id, cancellationToken))
        {
            return NotFound();
        }

        Image image = await Image.LoadAsync(stream, cancellationToken);
        IImageFormat? format = image.Metadata.DecodedImageFormat;

        if (format == null)
            return BadRequest("Could not recognize media");

        bool isSupported = format.Name switch
        {
            "PNG" => true,
            "JPEG" => true,
            "GIF" => true,
            _ => false
        };

        if (!isSupported)
            return BadRequest("Mime type of data not accepted");

        stream.Position = 0;

        try
        {
            await _fileRepository.SaveFullStreamAsync(id, stream, format.DefaultMimeType,
                cancellationToken);

            // TODO: Delegate thumbnail generation to background service?
            using MemoryStream thumbStream = MemoryManager.GetStream();
            image.Mutate(x => x.Resize(new ResizeOptions()
            {
                Size = new Size(500, 500),
                Mode = ResizeMode.Max
            }));

            await image.SaveAsync(thumbStream, new JpegEncoder { Quality = 75 }, cancellationToken);

            thumbStream.Position = 0;
            await _fileRepository.SaveThumbStreamAsync(id, thumbStream, "image/png", cancellationToken);
        }
        catch (Exceptions.FileExistsException)
        {
            return Conflict("Media already exists");
        }
        catch (Exception e)
        {
            _logger.Error(e, $"{nameof(HandleMedia)} failed");
            return BadRequest(); // TODO: Better
        }

        return Ok();
    }
}
