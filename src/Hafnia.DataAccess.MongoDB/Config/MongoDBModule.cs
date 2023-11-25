using Autofac;
using Hafnia.DataAccess.MongoDB.Repositories;
using Hafnia.DataAccess.MongoDB.Repositories.V2;
using Hafnia.DataAccess.Repositories;
using Hafnia.DataAccess.Repositories.V2;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using IMetadataRepository = Hafnia.DataAccess.Repositories.IMetadataRepository;
using MetadataRepository = Hafnia.DataAccess.MongoDB.Repositories.MetadataRepository;
using V2Mapper = Hafnia.DataAccess.MongoDB.Mappers.V2;
using V2Repo = Hafnia.DataAccess.MongoDB.Repositories.V2;
using V2RepoInt = Hafnia.DataAccess.Repositories.V2;

namespace Hafnia.DataAccess.MongoDB.Config;

public class MongoDBModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        RegisterConnection(builder);
        RegisterMappers(builder);
        RegisterRepositories(builder);
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
        builder.RegisterType<MetadataRepository>().As<IMetadataRepository>().SingleInstance();
        builder.RegisterType<WorkRepository>().As<IWorkRepository>().SingleInstance();
        builder.RegisterType<TagRepository>().AsImplementedInterfaces().SingleInstance();

        builder.RegisterType<V2Repo.MetadataRepository>().AsSelf().As<V2RepoInt.IMetadataRepository>().SingleInstance();
    }
}
