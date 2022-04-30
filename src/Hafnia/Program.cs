using Akka.Actor;
using Akka.Routing;
using Hafnia.Actors;
using Hafnia.Shared.Config;
using Serilog;
using Serilog.Core;

CancellationTokenSource cts = new CancellationTokenSource();

Logger logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Information()
    .CreateLogger();

Log.Logger = logger;

using (ActorSystem system = ActorSystemConfig.CreateSystemFromPath("SYSTEM_NAME", "Main.hocon"))
{
    logger.Information(system.Name);

    IActorRef socialRouterRef = system.ActorOf(Props.Create<SocialActor>().WithRouter(FromConfig.Instance), "socials");

    logger.Information("Press CTRL+C or send sigterm to exit");

    Console.CancelKeyPress += (sender, args) =>
    {
        logger.Information("Termination requested");
        cts.Cancel();
        args.Cancel = true;
    };

    cts.Token.WaitHandle.WaitOne();
}
