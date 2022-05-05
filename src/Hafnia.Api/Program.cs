using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hafnia.Api.Config;
using Octothorp.AspNetCore.Helpers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host
    .UseSerilog((ctx, logBuilder) => logBuilder.WriteTo.Console())
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(AutofacConfigure.Configure);

// Services
IServiceCollection services = builder.Services;

services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true);
services.AddCommonSwagger();

// AppBuilder
var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCommonSwagger(options =>
{

}, uiOptions =>
{
    uiOptions.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    uiOptions.RoutePrefix = string.Empty;
});

app.MapControllers();
app.MapStatus();

app.Run();
