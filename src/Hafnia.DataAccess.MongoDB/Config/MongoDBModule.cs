using Autofac;
using Hafnia.DataAccess.MongoDB.Repositories;
using Hafnia.DataAccess.MongoDB.Repositories.V2;
using Hafnia.DataAccess.MongoDB.Services;
using Hafnia.DataAccess.Repositories;
using Hafnia.DataAccess.Repositories.V2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using V2Mapper = Hafnia.DataAccess.MongoDB.Mappers.V2;

namespace Hafnia.DataAccess.MongoDB.Config;

public class MongoDBModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        RegisterConnection(builder);
        RegisterMappers(builder);
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

    private static void RegisterMappers(ContainerBuilder builder)
    {
        builder.RegisterType<V2Mapper.CollectionMapper>().AsImplementedInterfaces();
        builder.RegisterType<V2Mapper.MetadataMapper>().AsImplementedInterfaces();
        builder.RegisterType<V2Mapper.TagMapper>().AsImplementedInterfaces();
    }

    private static void RegisterRepositories(ContainerBuilder builder)
    {
        builder.RegisterType<CollectionRepository>().As<ICollectionRepository>().SingleInstance();
        builder.RegisterType<WorkRepository>().As<IWorkRepository>().SingleInstance();
        builder.RegisterType<TagRepository>().AsImplementedInterfaces().SingleInstance();

        builder.RegisterType<MetadataRepository>().AsSelf().As<IMetadataRepository>().SingleInstance();
    }

    private static void RegisterServices(ContainerBuilder builder)
    {
        builder.RegisterType<IndexCreatorService>().As<IHostedService>().SingleInstance();
    }
}
