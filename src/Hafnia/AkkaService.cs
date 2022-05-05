using Akka.Actor;
using Akka.DI.AutoFac;
using Autofac;
using Hafnia.Config;
using Hafnia.Services;
using Hafnia.Shared.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Hafnia;

internal class AkkaService : IHostedService, IActorBridge
{
    private readonly IHostApplicationLifetime _appLifeTime;

    private ActorSystem? _system;
    private AutoFacDependencyResolver? _dependencyResolver;
    private IActorRef? _supervisorRef;
    private SystemConfig _systemConfig;

    public AkkaService(IHostApplicationLifetime appLifeTime, IConfiguration configuration)
    {
        _appLifeTime = appLifeTime;

        _systemConfig = configuration.GetSection("cluster").Get<SystemConfig>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ContainerBuilder containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<ClusterSupervisor>();

        IContainer container = containerBuilder.Build();

        ActorSystem system = ActorSystemConfig.CreateSystemFromPath(_systemConfig.SystemName, _systemConfig.HoconPath);
        _system = system.UseAutofac(container);

        _dependencyResolver = new AutoFacDependencyResolver(container, system);

        Props supervisorProps = _dependencyResolver.Create<ClusterSupervisor>();
        _supervisorRef = _system.ActorOf(supervisorProps, "supervisor");

        _system.WhenTerminated.ContinueWith(tr =>
        {
            _appLifeTime.StopApplication();
        });

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await CoordinatedShutdown.Get(_system).Run(CoordinatedShutdown.ClrExitReason.Instance);
    }

    public void Tell(object message)
    {
        _supervisorRef.Tell(message);
    }

    public async Task<T> Ask<T>(object message, CancellationToken cancellationToken = default)
    {
        return await _supervisorRef.Ask<T>(message, cancellationToken);
    }
}
