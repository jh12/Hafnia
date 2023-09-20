namespace Hafnia.DataAccess.Minio.Config;

public class MinioConfiguration
{
    public const string Section = "minio";

    public required string Endpoint { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public required string Bucket { get; init; }
}
