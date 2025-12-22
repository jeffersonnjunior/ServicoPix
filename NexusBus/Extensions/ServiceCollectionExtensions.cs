using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusBus.Abstractions;
using NexusBus.Configuration;
using NexusBus.Providers.RabbitMQ;

namespace NexusBus.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNexusBus(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(NexusOptions.SectionName);
        services.Configure<NexusOptions>(section);

        RegisterRabbitMq(services);

        return services;
    }

    private static void RegisterRabbitMq(IServiceCollection services)
    {
        services.AddSingleton<RabbitMqConnection>();

        services.AddSingleton<RabbitMqProducer>();
        services.AddSingleton<RabbitMqConsumer>();

        services.AddSingleton<INexusBus, RabbitMqProvider>();
    }
}