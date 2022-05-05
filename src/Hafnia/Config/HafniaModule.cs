using Autofac;
using Microsoft.Extensions.Hosting;

namespace Hafnia.Config;

public class HafniaModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<AkkaService>()
            .As<IHostedService>()
            .As<IActorBridge>()
            .SingleInstance();
    }
}
