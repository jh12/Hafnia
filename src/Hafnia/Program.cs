using Akka.Actor;
using Serilog;
using Serilog.Core;

CancellationTokenSource cts = new CancellationTokenSource();

Logger logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Information()
    .CreateLogger();

const string hoconConfig = "akka { loglevel=DEBUG, loggers=[\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"] }";

using (ActorSystem system = ActorSystem.Create("hafnia-system", hoconConfig))
{
    logger.Information("Press CTRL+C or send sigterm to exit");

    Console.CancelKeyPress += (sender, args) =>
    {
        logger.Information("Termination requested");
        cts.Cancel();
        args.Cancel = true;
    };

    cts.Token.WaitHandle.WaitOne();
}