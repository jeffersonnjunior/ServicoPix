using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServicoPix.Domain.Interfaces.IRepositories;
using ServicoPix.Domain.Interfaces.Services;
using ServicoPix.Infrastructure.Adapters;
using ServicoPix.Infrastructure.Persistence.Context;
using ServicoPix.Infrastructure.Persistence.Repositories;

namespace ServicoPix.Infrastructure.DependencyInjection;

public static class AddInfrastructureDependencyInjection
{
    public static void DependencyInjectionInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        });

        services.AddScoped<IContaRepository, ContaRepository>();
        services.AddScoped<ITransacaoRepository, TransacaoRepository>();
        services.AddScoped<IMensageriaService, NexusBusAdapter>();
    }
}