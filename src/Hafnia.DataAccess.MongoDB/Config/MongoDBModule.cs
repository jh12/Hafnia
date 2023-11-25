using Autofac;
using V2Repo = Hafnia.DataAccess.MongoDB.Repositories.V2;
using V2Mapper = Hafnia.DataAccess.MongoDB.Mappers.V2;
using Hafnia.DataAccess.MongoDB.Repositories;
using Hafnia.DataAccess.MongoDB.Repositories.V2;
using Hafnia.DataAccess.MongoDB.Services;
using Hafnia.DataAccess.Repositories;
using V2RepoInt = Hafnia.DataAccess.Repositories.V2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using IMetadataRepository = Hafnia.DataAccess.Repositories.IMetadataRepository;
using MetadataRepository = Hafnia.DataAccess.MongoDB.Repositories.MetadataRepository;
using Hafnia.DataAccess.Repositories.V2;

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
        builder.RegisterType<V2Mapper.MetadataMapper>().AsImplementedInterfaces();
        builder.RegisterType<V2Mapper.TagMapper>().AsImplementedInterfaces();
    }

    private static void RegisterRepositories(ContainerBuilder builder)
    {
        builder.RegisterType<MetadataRepository>().As<IMetadataRepository>().SingleInstance();
        builder.RegisterType<WorkRepository>().As<IWorkRepository>().SingleInstance();
        builder.RegisterType<TagRepository>().AsImplementedInterfaces().SingleInstance();

        builder.RegisterType<V2Repo.MetadataRepository>().AsSelf().As<V2RepoInt.IMetadataRepository>().SingleInstance();
    }

    private static void RegisterServices(ContainerBuilder builder)
    {
        builder.RegisterType<IndexCreatorService>().As<IHostedService>().SingleInstance();
        //builder.RegisterType<MigrationService>().As<IHostedService>().SingleInstance();
        //builder.RegisterType<TaggingService>().As<IHostedService>().SingleInstance();
    }
}
