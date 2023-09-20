using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hafnia.DataAccess.Minio.Config;
using Hafnia.DataAccess.MongoDB.Config;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

SetupLogger(builder);

builder.Host.UseSerilog();

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(b =>
{
    b.RegisterModule<MinioModule>();
    b.RegisterModule<MongoDBModule>();
});

IServiceCollection services = builder.Services;

services.AddSerilog(Log.Logger);

services
    .Configure<MinioConfiguration>(
        builder.Configuration.GetRequiredSection(MinioConfiguration.Section))
    .Configure<MongoConfiguration>(
        builder.Configuration.GetRequiredSection(MongoConfiguration.Section));

services.AddControllers();
services.AddSwaggerGen(c =>
{
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.EnableTryItOutByDefault();
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    o.RoutePrefix = string.Empty;
});

app.UseRouting();

app.MapControllers();

app.Run();

void SetupLogger(WebApplicationBuilder webApplicationBuilder)
{
    LoggerConfiguration loggerConfiguration = new LoggerConfiguration();

    if (webApplicationBuilder.Environment.IsDevelopment())
    {
        loggerConfiguration.WriteTo.Console();
    }
    else
    {
        loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
    }

    loggerConfiguration
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext();

    Log.Logger = loggerConfiguration.CreateLogger();
}
