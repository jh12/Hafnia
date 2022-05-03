using Autofac;

namespace Hafnia.Api.Config;

internal static class AutofacConfigure
{
    public static void Configure(HostBuilderContext ctx, ContainerBuilder builder)
    {
        builder.RegisterModule<ApiModule>();
    }
}
