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

        // Register BOTH providers so the application can inject and use both simultaneously.
        // INexusBus remains as the default provider selected via NexusBus:Provider.
        RegisterRabbitMq(services);
        RegisterKafka(services);

        services.AddSingleton<INexusBus>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NexusOptions>>().Value;
            if (string.Equals(opts.Provider, "Kafka", StringComparison.OrdinalIgnoreCase))
                return sp.GetRequiredService<IKafkaNexusBus>();

            return sp.GetRequiredService<IRabbitMqNexusBus>();
        });

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

        services.AddSingleton<IRabbitMqNexusBus, RabbitMqProvider>();
    }

    private static void RegisterKafka(IServiceCollection services)
    {
        services.AddSingleton<KafkaProducer>();
        services.AddSingleton<KafkaConsumer>();

        services.AddSingleton<IKafkaNexusBus, KafkaProvider>();
    }
}