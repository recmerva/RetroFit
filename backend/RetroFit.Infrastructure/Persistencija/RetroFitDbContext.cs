using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RetroFit.Domain.Entiteti;

namespace RetroFit.Infrastructure.Persistencija;

/// <summary>
/// EF Core kontekst baze. Sva pravila mapiranja (ključevi, indeksi, CHECK ograničenja, ponašanje
/// pri brisanju) nalaze se u klasama iz foldera Konfiguracije (IEntityTypeConfiguration), a ne ovde.
///
/// Koristi se ISKLJUČIVO iz Infrastructure sloja (repozitorijumi i Unit of Work);
/// kontroleri i handleri nikada ne pristupaju DbContext-u direktno.
/// </summary>
public class RetroFitDbContext(DbContextOptions<RetroFitDbContext> options) : DbContext(options)
{
    public DbSet<Korisnik> Korisnici => Set<Korisnik>();
    public DbSet<Uloga> Uloge => Set<Uloga>();
    public DbSet<KorisnikUloga> KorisnikUloge => Set<KorisnikUloga>();
    public DbSet<RefreshToken> RefreshTokeni => Set<RefreshToken>();
    public DbSet<VerifikacijaKorisnika> VerifikacijeKorisnika => Set<VerifikacijaKorisnika>();
    public DbSet<BlokadaKorisnika> BlokiraniKorisnici => Set<BlokadaKorisnika>();

    public DbSet<Grad> Gradovi => Set<Grad>();
    public DbSet<Kategorija> Kategorije => Set<Kategorija>();
    public DbSet<DefinicijaAtributa> DefinicijeAtributa => Set<DefinicijaAtributa>();
    public DbSet<Oglas> Oglasi => Set<Oglas>();
    public DbSet<SlikaOglasa> SlikeOglasa => Set<SlikaOglasa>();
    public DbSet<AtributOglasa> AtributiOglasa => Set<AtributOglasa>();

    public DbSet<OmiljeniOglas> OmiljeniOglasi => Set<OmiljeniOglas>();
    public DbSet<SacuvanaPretraga> SacuvanePretrage => Set<SacuvanaPretraga>();
    public DbSet<IstaknutiOglas> IstaknutiOglasi => Set<IstaknutiOglas>();

    public DbSet<Razgovor> Razgovori => Set<Razgovor>();
    public DbSet<Poruka> Poruke => Set<Poruka>();
    public DbSet<Ponuda> Ponude => Set<Ponuda>();
    public DbSet<Obavestenje> Obavestenja => Set<Obavestenje>();

    public DbSet<Rezervacija> Rezervacije => Set<Rezervacija>();
    public DbSet<Transakcija> Transakcije => Set<Transakcija>();
    public DbSet<Placanje> Placanja => Set<Placanje>();
    public DbSet<Recenzija> Recenzije => Set<Recenzija>();

    public DbSet<ZahtevTrazim> ZahteviTrazim => Set<ZahtevTrazim>();
    public DbSet<OdgovorNaZahtev> OdgovoriNaZahteve => Set<OdgovorNaZahtev>();

    public DbSet<Prijava> Prijave => Set<Prijava>();
    public DbSet<OtisakUredjaja> OtisciUredjaja => Set<OtisakUredjaja>();
    public DbSet<BlokadaUredjaja> BlokiraniUredjaji => Set<BlokadaUredjaja>();
    public DbSet<AuditZapis> AuditZapisi => Set<AuditZapis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RetroFitDbContext).Assembly);

        // Podrazumevano: brisanje roditelja NE briše decu (Restrict / NO ACTION).
        // Kaskadno brisanje je eksplicitno uključeno samo za "delove" entiteta (slike, atributi...).
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            var izvor = ((IConventionForeignKey)fk).GetDeleteBehaviorConfigurationSource();
            if (fk.DeleteBehavior == DeleteBehavior.Cascade && !fk.IsOwnership && izvor != ConfigurationSource.Explicit)
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Novac: decimal(18,2) za sve decimal kolone (osim gde je drugačije navedeno).
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<decimal?>().HavePrecision(18, 2);
        // Svi datumi su u UTC-u; datetime2(3) = preciznost u milisekundama.
        configurationBuilder.Properties<DateTime>().HaveColumnType("datetime2(3)").HaveConversion<UtcKonverter>();
        configurationBuilder.Properties<DateTime?>().HaveColumnType("datetime2(3)").HaveConversion<UtcKonverter>();
    }

    /// <summary>
    /// datetime2 ne čuva vremensku zonu, pa EF pročitane vrednosti vraća kao DateTimeKind.Unspecified.
    /// Sva vremena u bazi su UTC, zato ih pri čitanju označavamo kao UTC: JSON tada dobija sufiks "Z"
    /// i pregledač ih ispravno prikazuje u lokalnom vremenu (bez pomeranja za sat-dva).
    /// </summary>
    internal sealed class UtcKonverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
