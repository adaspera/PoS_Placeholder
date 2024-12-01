using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;

namespace PoS_Placeholder.Server.Logging;

public static class LoggerExtensions
{
    public static ILoggingBuilder AddLogger(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions<LoggerConfigurations, LoggerProvider>(builder.Services);

        return builder;
    }

    public static ILoggingBuilder AddLogger(this ILoggingBuilder builder, Action<LoggerConfigurations> configure)
    {
        builder.AddLogger();
        builder.Services.Configure(configure);

        return builder;
    }
}