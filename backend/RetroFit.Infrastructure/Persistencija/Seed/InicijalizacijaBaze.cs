using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Pravila;
using RetroFit.Infrastructure.Pretraga;

namespace RetroFit.Infrastructure.Persistencija.Seed;

/// <summary>
/// Pokreće se jednom pri startu API-ja:
///  1) primenjuje EF Core migracije (Database.MigrateAsync - NE EnsureCreated), ako je uključeno;
///  2) proverava dostupnost Full-Text Search-a;
///  3) kreira početnog administratora iz konfiguracije (ako ne postoji);
///  4) u razvojnom/Docker okruženju puni demo podatke (ako je baza prazna).
/// </summary>
public static class InicijalizacijaBaze
{
    public static async Task InicijalizujAsync(IServiceProvider servisi, IConfiguration konfiguracija, CancellationToken ct = default)
    {
        using var opseg = servisi.CreateScope();
        var sp = opseg.ServiceProvider;
        var db = sp.GetRequiredService<RetroFitDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("InicijalizacijaBaze");

        if (konfiguracija.GetValue("Baza:PrimeniMigracije", false))
        {
            await PrimeniMigracijeAsync(db, logger, ct);
        }

        await sp.GetRequiredService<IFullTextDetektor>().ProveriAsync(db, ct);
        await KreirajAdministratoraAsync(sp, db, konfiguracija, logger, ct);

        if (konfiguracija.GetValue("Baza:DemoPodaci", false))
        {
            await DemoPodaciSeeder.PopuniAsync(sp, db, logger, ct);
        }
    }

    private static async Task PrimeniMigracijeAsync(RetroFitDbContext db, ILogger logger, CancellationToken ct)
    {
        // SQL Server u Docker-u se pokreće nekoliko sekundi - pokušavamo više puta.
        for (var pokusaj = 1; ; pokusaj++)
        {
            try
            {
                var naCekanju = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
                if (naCekanju.Count > 0)
                {
                    logger.LogInformation("Primenjujem {Broj} migracija: {Migracije}", naCekanju.Count, string.Join(", ", naCekanju));
                    await db.Database.MigrateAsync(ct);
                }
                else
                {
                    logger.LogInformation("Baza je ažurna, nema novih migracija.");
                }

                return;
            }
            catch (Exception ex) when (pokusaj < 10 && ex is not OperationCanceledException)
            {
                logger.LogWarning("Baza još nije dostupna (pokušaj {Pokusaj}/10): {Poruka}", pokusaj, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    private static async Task KreirajAdministratoraAsync(IServiceProvider sp, RetroFitDbContext db, IConfiguration konfiguracija,
        ILogger logger, CancellationToken ct)
    {
        var email = konfiguracija["PocetniAdmin:Email"];
        var lozinka = konfiguracija["PocetniAdmin:Lozinka"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(lozinka))
        {
            return;
        }

        if (await db.KorisnikUloge.AnyAsync(ku => ku.UlogaId == PocetniPodaci.UlogaAdminId, ct))
        {
            return;
        }

        var sada = sp.GetRequiredService<IDatumVreme>().SadaUtc;
        var uloge = await db.Uloge.ToListAsync(ct);
        var admin = new Korisnik(email, "Admin", "RetroFit", 1);
        admin.PostaviLozinkuHash(sp.GetRequiredService<IHesiranjeLozinke>().Hesiraj(lozinka));
        admin.DodeliUlogu(uloge.Single(u => u.Naziv == NaziviUloga.Korisnik), sada);
        admin.DodeliUlogu(uloge.Single(u => u.Naziv == NaziviUloga.Moderator), sada);
        admin.DodeliUlogu(uloge.Single(u => u.Naziv == NaziviUloga.Admin), sada);
        db.Korisnici.Add(admin);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Kreiran početni administrator {Email}", email);
    }
}
