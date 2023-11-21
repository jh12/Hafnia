﻿using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Hafnia.DataAccess.Minio.Config;
using Hafnia.DataAccess.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Minio;
using Minio.DataModel;
using Minio.Exceptions;

namespace Hafnia.DataAccess.Minio.Repositories;

public class FileRepository : IFileRepository
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucket;
    private static readonly RecyclableMemoryStreamManager StreamManager = new();

    public FileRepository(IMinioClient minioClient, IOptions<MinioConfiguration> minioOptions)
    {
        _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));

        _bucket = minioOptions.Value.Bucket;
    }

    public async Task<(Stream Stream, string ContentType)> GetFullStreamAsync(string id, CancellationToken cancellationToken)
    {
        return await GetStreamAsync($"images/full/{id}", cancellationToken);
    }

    public async Task<(Stream Stream, string ContentType)> GetThumbStreamAsync(string id, CancellationToken cancellationToken)
    {
        return await GetStreamAsync($"images/thumb/{id}", cancellationToken);
    }

    private async Task<(Stream Stream, string ContentType)> GetStreamAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            MemoryStream memoryStream = StreamManager.GetStream();
            GetObjectArgs getArgs = new GetObjectArgs()
                .WithBucket(_bucket)
                .WithObject(path)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                });

            ObjectStat objectStat = await _minioClient.GetObjectAsync(getArgs, cancellationToken);

            memoryStream.Position = 0;

            return (memoryStream, objectStat.ContentType);
        }
        catch (MinioException e) when (e.ServerMessage == "Not found.")
        {
            throw new Exceptions.FileNotFoundException();
        }
    }

    public async Task SaveFullStreamAsync(string id, Stream stream, string contentType, CancellationToken cancellationToken)
    {
        await SaveStreamAsync($"images/full/{id}", stream, contentType, cancellationToken);
    }

    public async Task SaveThumbStreamAsync(string id, Stream stream, string contentType, CancellationToken cancellationToken)
    {
        await SaveStreamAsync($"images/thumb/{id}", stream, contentType, cancellationToken);
    }

    private async Task SaveStreamAsync(string path, Stream stream, string contentType, CancellationToken cancellationToken)
    {
        StatObjectArgs statArgs = new StatObjectArgs()
            .WithBucket(_bucket)
            .WithObject(path);

        try
        {
            await _minioClient.StatObjectAsync(statArgs, cancellationToken);
            throw new Exceptions.FileExistsException();
        }
        catch (MinioException e) when (e.ServerMessage == "Not found.")
        {
        }
        catch (MinioException)
        {
            throw new Exceptions.FileExistsException();
        }

        PutObjectArgs putArgs = new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(path)
            .WithStreamData(stream)
            .WithContentType(contentType)
            .WithObjectSize(stream.Length);

        await _minioClient.PutObjectAsync(putArgs, cancellationToken);
    }

    public async Task<string> GetRawTextAsync(string id, CancellationToken cancellationToken)
    {
        string path = $"raw/{id}";

        return await GetTextAsync(path, cancellationToken);
    }

    public async Task<string> GetAdditionalRawTextAsync(string id, string additional, CancellationToken cancellationToken)
    {
        string path = $"raw/{id}/{additional}";

        return await GetTextAsync(path, cancellationToken);
    }

    private async Task<string> GetTextAsync(string path, CancellationToken cancellationToken)
    {
        await using MemoryStream memoryStream = StreamManager.GetStream();
        GetObjectArgs getArgs = new GetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(path)
            .WithCallbackStream(stream => { stream.CopyTo(memoryStream); });

        try
        {
            await _minioClient.GetObjectAsync(getArgs, cancellationToken);

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
        catch (ObjectNotFoundException)
        {
            throw new Exceptions.FileNotFoundException();
        }
    }

    public IAsyncEnumerable<string> GetIdsAsync(int? size, CancellationToken cancellationToken = default)
    {
        string prefix = "images/full/";
        int prefixLength = prefix.Length;

        ListObjectsArgs listArgs = new ListObjectsArgs()
            .WithBucket(_bucket)
            .WithPrefix(prefix);

        IObservable<string> observable = _minioClient.ListObjectsAsync(listArgs, cancellationToken)
            .Select(o => o.Key[prefixLength..]);

        if (size.HasValue)
            observable = observable.Take(size.Value);

        return observable
            .ToAsyncEnumerable();
    }

    public IAsyncEnumerable<string> ListRawIdsAsync(string pattern, CancellationToken cancellationToken)
    {
        string prefix = $"raw/";
        int prefixLength = prefix.Length;

        Regex regex = new Regex(pattern);

        ListObjectsArgs listArgs = new ListObjectsArgs()
            .WithBucket(_bucket)
            .WithPrefix(prefix);

        return _minioClient.ListObjectsAsync(listArgs, cancellationToken)
            .Select(o => o.Key[prefixLength..])
            .Where(k => regex.IsMatch(k))
            .ToAsyncEnumerable();
    }

    public async Task SaveRawTextAsync(string id, string content, string contentType, CancellationToken cancellationToken)
    {
        string path = $"raw/{id}";

        await SaveTextAsync(path, content, contentType, cancellationToken);
    }

    public async Task SaveAdditionalRawTextAsync(string id, string additional, string content, string contentType, CancellationToken cancellationToken)
    {
        string path = $"raw/{id}/{additional}";

        await SaveTextAsync(path, content, contentType, cancellationToken);
    }

    private async Task SaveTextAsync(string path, string content, string contentType, CancellationToken cancellationToken)
    {
        StatObjectArgs statArgs = new StatObjectArgs()
            .WithBucket(_bucket)
            .WithObject(path);

        try
        {
            await _minioClient.StatObjectAsync(statArgs, cancellationToken);
            throw new Exceptions.FileExistsException();
        }
        catch (MinioException e) when (e.ServerMessage == "Not found.")
        {
        }
        catch (MinioException)
        {
            throw new Exceptions.FileExistsException();
        }

        using MemoryStream stream = StreamManager.GetStream(Encoding.UTF8.GetBytes(content));
        PutObjectArgs putArgs = new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(path)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putArgs, cancellationToken);
    }

    public async Task<bool> RawExistsAsync(string id, CancellationToken cancellationToken)
    {
        return await ObjectExistsAsync($"raw/{id}", cancellationToken);
    }

    public async Task<bool> RawAdditionalExistsAsync(string id, string additional, CancellationToken cancellationToken)
    {
        return await ObjectExistsAsync($"raw/{id}/{additional}", cancellationToken);
    }

    private async Task<bool> ObjectExistsAsync(string path, CancellationToken cancellationToken)
    {
        StatObjectArgs statArgs = new StatObjectArgs()
            .WithBucket(_bucket)
            .WithObject(path);

        try
        {
            await _minioClient.StatObjectAsync(statArgs, cancellationToken);

            return true;
        }
        catch (MinioException)
        {
            return false;
        }
    }
}
