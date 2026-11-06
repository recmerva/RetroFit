using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Infrastructure.Identitet;
using RetroFit.Infrastructure.Persistencija;
using RetroFit.Infrastructure.Persistencija.Interceptori;
using RetroFit.Infrastructure.Pretraga;
using RetroFit.Infrastructure.Servisi;

namespace RetroFit.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registracija svih servisa Infrastructure sloja u DI kontejner.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration konfiguracija)
    {
        var konekcija = konfiguracija.GetConnectionString("RetroFit")
            ?? throw new InvalidOperationException("Nedostaje konekcioni string 'ConnectionStrings:RetroFit'.");

        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<RetroFitDbContext>((sp, opcije) =>
        {
            opcije.UseSqlServer(konekcija, sql =>
            {
                sql.UseNetTopologySuite();
                // Ponovni pokušaj kod prolaznih grešaka (npr. SQL Server u Dockeru se još pokreće).
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                sql.CommandTimeout(30);
            });
            opcije.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IFullTextDetektor>(sp => new FullTextDetektor(
            sp.GetRequiredService<ILogger<FullTextDetektor>>(),
            konfiguracija["Pretraga:FullText"] ?? "auto"));

        services.Configure<JwtOpcije>(konfiguracija.GetSection(JwtOpcije.Sekcija));
        services.Configure<SkladisteOpcije>(konfiguracija.GetSection(SkladisteOpcije.Sekcija));

        services.AddSingleton<IDatumVreme, DatumVreme>();
        services.AddSingleton<IHesiranjeLozinke, HesiranjeLozinke>();
        services.AddScoped<ITokenServis, TokenServis>();
        services.AddScoped<IAuditServis, AuditServis>();
        services.AddSingleton<ISkladisteFajlova, LokalnoSkladisteFajlova>();

        // Mock spoljni servisi (singleton, jer čuvaju stanje u memoriji: poslate poruke, idempotentne naplate).
        services.AddSingleton<MockSmsServis>();
        services.AddSingleton<ISmsServis>(sp => sp.GetRequiredService<MockSmsServis>());
        services.AddSingleton<IPlatniProcesor, MockPlatniProcesor>();

        if (konfiguracija.GetValue("PozadinskiPoslovi:Ukljuceni", true))
        {
            services.AddHostedService<PozadinskiPoslovi.PeriodicniPoslovi>();
        }

        return services;
    }
}
