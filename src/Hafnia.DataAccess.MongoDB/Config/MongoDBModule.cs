using Autofac;
using Hafnia.DataAccess.MongoDB.Repositories;
using Hafnia.DataAccess.MongoDB.Services;
using Hafnia.DataAccess.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Hafnia.DataAccess.MongoDB.Config;

public class MongoDBModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        RegisterConnection(builder);
        RegisterRepositories(builder);
        RegisterServices(builder);
    }

    private static void RegisterConnection(ContainerBuilder builder)
    {
        builder.Register(ctx =>
        {
            IOptions<MongoConfiguration> options = ctx.Resolve<IOptions<MongoConfiguration>>();
            MongoConfiguration config = options.Value;

            return new MongoClient(config.ConnectionString);
        }).As<IMongoClient>().SingleInstance();
    }

    private static void RegisterRepositories(ContainerBuilder builder)
    {
        builder.RegisterType<MetadataRepository>().As<IMetadataRepository>().SingleInstance();
    }

    private static void RegisterServices(ContainerBuilder builder)
    {
        builder.RegisterType<IndexCreatorService>().As<IHostedService>().SingleInstance();
    }
}
