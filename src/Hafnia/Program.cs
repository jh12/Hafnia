using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hafnia.Config;
using Hafnia.DataAccess.Minio.Config;
using Hafnia.DataAccess.MongoDB.Config;
using Serilog;
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
    .Configure<BaseConfig>(builder.Configuration)
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

BaseConfig baseConfig = builder.Configuration.Get<BaseConfig>()!;

if (baseConfig.CorsEnable)
{
    AddCors(baseConfig, builder);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

if (baseConfig.CorsEnable)
{
    app.UseCors();
}

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.EnableTryItOutByDefault();
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    o.RoutePrefix = "api";
});

app.UseBlazorFrameworkFiles();
app.MapFallbackToFile("index.html");

app.UseRouting();
app.UseStaticFiles();

app.MapControllers();

app.Run();

static void SetupLogger(WebApplicationBuilder webApplicationBuilder)
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

    loggerConfiguration.ReadFrom.Configuration(webApplicationBuilder.Configuration);

    loggerConfiguration
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext();

    Log.Logger = loggerConfiguration.CreateLogger();
}

static void AddCors(BaseConfig baseConfig, WebApplicationBuilder builder)
{
    builder.Services.AddCors(o =>
    {
        bool isCorsConfigured = false;

        if (!string.IsNullOrWhiteSpace(baseConfig.CorsDomains))
        {
            string[] domains = baseConfig.CorsDomains.Split(',', StringSplitOptions.TrimEntries);

            o.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithOrigins(domains);
            });

            isCorsConfigured = true;
        }

        if (baseConfig.CorsAllowAll)
        {
            o.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });

            isCorsConfigured = true;
        }

        if (!isCorsConfigured)
        {
            throw new Exception("Cors enabled but no policy configured");
        }
    });
}
