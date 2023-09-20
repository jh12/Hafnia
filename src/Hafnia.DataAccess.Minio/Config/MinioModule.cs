using Autofac;
using Hafnia.DataAccess.Minio.Repositories;
using Hafnia.DataAccess.Repositories;
using Microsoft.Extensions.Options;
using Minio;

namespace Hafnia.DataAccess.Minio.Config;

public class MinioModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        RegisterRepositories(builder);
        RegisterClient(builder);
    }

    private static void RegisterRepositories(ContainerBuilder builder)
    {
        builder.RegisterType<FileRepository>().As<IFileRepository>();
    }

    private static void RegisterClient(ContainerBuilder builder)
    {
        builder.Register(ctx =>
        {
            IOptions<MinioConfiguration> options = ctx.Resolve<IOptions<MinioConfiguration>>();

            MinioConfiguration minio = options.Value;

            return new MinioClient()
                .WithEndpoint(minio.Endpoint)
                .WithCredentials(minio.AccessKey, minio.SecretKey)
                .Build();
        }).As<IMinioClient>().SingleInstance();
    }
}
