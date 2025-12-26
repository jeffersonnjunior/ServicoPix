using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusBus.Abstractions;
using NexusBus.Configuration;
using NexusBus.Providers.Kafka;
using NexusBus.Providers.RabbitMQ;

namespace NexusBus.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNexusBus(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(NexusOptions.SectionName);
        services.Configure<NexusOptions>(section);

        var provider = section.GetValue<string>(nameof(NexusOptions.Provider)) ?? "RabbitMQ";
        if (provider.Equals("Kafka", StringComparison.OrdinalIgnoreCase))
        {
            RegisterKafka(services);
        }
        else
        {
            RegisterRabbitMq(services);
        }

        return services;
    }

    public static IServiceCollection AddNexusBusRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(NexusOptions.SectionName);
        services.Configure<NexusOptions>(section);
        RegisterRabbitMq(services);
        return services;
    }

    public static IServiceCollection AddNexusBusKafka(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(NexusOptions.SectionName);
        services.Configure<NexusOptions>(section);
        RegisterKafka(services);
        return services;
    }

    private static void RegisterRabbitMq(IServiceCollection services)
    {
        services.AddSingleton<RabbitMqConnection>();

        services.AddSingleton<RabbitMqProducer>();
        services.AddSingleton<RabbitMqConsumer>();

        services.AddSingleton<INexusBus, RabbitMqProvider>();
    }

    private static void RegisterKafka(IServiceCollection services)
    {
        services.AddSingleton<KafkaProducer>();
        services.AddSingleton<KafkaConsumer>();

        services.AddSingleton<INexusBus, KafkaProvider>();
    }
}