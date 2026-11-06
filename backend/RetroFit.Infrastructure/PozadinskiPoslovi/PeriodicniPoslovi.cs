using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RetroFit.Application.Features.Odrzavanje;

namespace RetroFit.Infrastructure.PozadinskiPoslovi;

/// <summary>
/// Pozadinski servis koji na svakih N minuta (podešavanje "PozadinskiPoslovi:IntervalMinuta") pokreće:
/// istek oglasa (60 dana), istek ponuda (48h), istek rezervacija (72h), obaveštenja za sačuvane pretrage
/// i čišćenje starih refresh tokena.
///
/// Svaki posao je obična MediatR komanda (logika je u Application sloju), pa se može i testirati
/// i pozvati ručno. Greška jednog posla ne zaustavlja ostale.
/// </summary>
internal sealed class PeriodicniPoslovi(IServiceScopeFactory opsezi, IConfiguration konfiguracija, ILogger<PeriodicniPoslovi> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, konfiguracija.GetValue("PozadinskiPoslovi:IntervalMinuta", 5)));
        logger.LogInformation("Pozadinski poslovi pokrenuti (interval {Interval}).", interval);

        // Kratko čekanje da se aplikacija potpuno podigne (migracije, seed).
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        using var tajmer = new PeriodicTimer(interval);
        do
        {
            await IzvrsiSveAsync(stoppingToken);
        }
        while (await tajmer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task IzvrsiSveAsync(CancellationToken ct)
    {
        IRequest<int>[] poslovi =
        [
            new IstekniOglaseCommand(),
            new IstekniPonudeCommand(),
            new IstekniRezervacijeCommand(),
            new ObavestiSacuvanePretrageCommand(),
            new OcistiTokeneCommand()
        ];

        foreach (var posao in poslovi)
        {
            try
            {
                // Svaki posao u svom DI opsegu = svoj DbContext (izolovane izmene).
                using var opseg = opsezi.CreateScope();
                var mediator = opseg.ServiceProvider.GetRequiredService<ISender>();
                await mediator.Send(posao, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pozadinski posao {Posao} nije uspeo.", posao.GetType().Name);
            }
        }
    }
}
