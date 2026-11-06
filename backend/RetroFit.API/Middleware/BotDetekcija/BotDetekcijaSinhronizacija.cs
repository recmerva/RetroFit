using System.Text.Json;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using Microsoft.Extensions.Options;

namespace RetroFit.API.Middleware.BotDetekcija;

/// <summary>
/// Pozadinska sinhronizacija Bot Detection stanja sa bazom (na svakih 30 sekundi):
///  1) upisuje skupljenu statistiku otisaka u OtisciUredjaja (batch umesto upisa na svaki zahtev);
///  2) upisuje bezbednosne događaje u AuditZapisi i pravi automatske blokade u BlokiraniUredjaji;
///  3) učitava listu trenutno blokiranih otisaka u memoriju (middleware je proverava bez upita bazi).
/// </summary>
internal sealed class BotDetekcijaSinhronizacija(
    IServiceScopeFactory opsezi,
    PracenjePonasanja pracenje,
    IOptionsMonitor<BotDetekcijaOpcije> opcije,
    ILogger<BotDetekcijaSinhronizacija> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (opcije.CurrentValue.Rezim == RezimBotDetekcije.Iskljuceno)
        {
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        using var tajmer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            try
            {
                await SinhronizujAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sinhronizacija Bot Detection podataka nije uspela.");
            }
        }
        while (await tajmer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task SinhronizujAsync(CancellationToken ct)
    {
        using var opseg = opsezi.CreateScope();
        var uow = opseg.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var sada = opseg.ServiceProvider.GetRequiredService<IDatumVreme>().SadaUtc;

        // 1) Statistika otisaka.
        foreach (var (hash, st) in pracenje.PreuzmiStatistiku())
        {
            var otisak = await uow.OtisciUredjaja.PoHashuAsync(hash, ct);
            if (otisak is null)
            {
                otisak = new OtisakUredjaja(hash, st.OpisKlijenta, sada);
                await uow.OtisciUredjaja.DodajAsync(otisak, ct);
            }

            otisak.Zabelezi((int)Math.Min(st.NoviZahtevi, int.MaxValue), st.NajveciRizik, st.KorisnikId, sada, st.OpisKlijenta);
        }

        await SacuvajSaPonovnimPokusajemAsync(uow, ct);

        // 2) Bezbednosni događaji i automatske blokade.
        var o = opcije.CurrentValue;
        foreach (var d in pracenje.PreuzmiDogadjaje())
        {
            var otisak = await uow.OtisciUredjaja.PoHashuAsync(d.Otisak, ct);
            if (otisak is null)
            {
                otisak = new OtisakUredjaja(d.Otisak, d.OpisKlijenta, sada);
                await uow.OtisciUredjaja.DodajAsync(otisak, ct);
                await uow.SacuvajAsync(ct);
            }

            if (d.AutomatskaBlokada && !await uow.OtisciUredjaja.ImaAktivnuBlokaduAsync(otisak.Id, sada, ct))
            {
                await uow.OtisciUredjaja.DodajBlokaduAsync(new BlokadaUredjaja(otisak.Id,
                    $"Automatska blokada: ponovljena sumnjiva aktivnost (rizik {d.Rizik}).", sada, sada.AddHours(o.AutomatskaBlokadaSati), null), ct);
                logger.LogWarning("Automatski blokiran uređaj {Otisak} na {Sati}h.", d.Otisak[..12], o.AutomatskaBlokadaSati);
            }

            await uow.Audit.DodajAsync(new AuditZapis(KategorijaAudita.Bezbednost, d.Akcija, nameof(OtisakUredjaja), otisak.Id.ToString(),
                d.KorisnikId, null, JsonSerializer.Serialize(new { d.Rizik, d.Razlozi, d.Ruta }, Json), d.Ip, null, d.Vreme), ct);
            await uow.SacuvajAsync(ct);
        }

        // 3) Osvežavanje liste blokiranih otisaka.
        pracenje.PostaviBlokiraneOtiske(await uow.OtisciUredjaja.AktivnoBlokiraniHesheviAsync(sada, ct));
    }

    private async Task SacuvajSaPonovnimPokusajemAsync(IUnitOfWork uow, CancellationToken ct)
    {
        try
        {
            await uow.SacuvajAsync(ct);
        }
        catch (KonfliktIzuzetak)
        {
            // Retka trka sa registracijom koja je u istom trenutku upisala isti otisak - statistika za ovaj ciklus se preskače.
            logger.LogInformation("Konflikt pri upisu statistike otisaka; preskočeno u ovom ciklusu.");
        }
    }
}
