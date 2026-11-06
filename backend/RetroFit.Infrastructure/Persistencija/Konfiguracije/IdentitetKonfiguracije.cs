using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Infrastructure.Persistencija.Seed;

namespace RetroFit.Infrastructure.Persistencija.Konfiguracije;

internal sealed class KorisnikKonfiguracija : IEntityTypeConfiguration<Korisnik>
{
    public void Configure(EntityTypeBuilder<Korisnik> b)
    {
        b.ToTable("Korisnici", t =>
        {
            t.HasCheckConstraint("CK_Korisnici_ProsecnaOcena", "[ProsecnaOcena] BETWEEN 0 AND 5");
            t.HasCheckConstraint("CK_Korisnici_BrojRecenzija", "[BrojRecenzija] >= 0");
            t.HasCheckConstraint("CK_Korisnici_BrojTransakcija", "[BrojZavrsenihTransakcija] >= 0");
            // Telefon ne može biti verifikovan ako ne postoji.
            t.HasCheckConstraint("CK_Korisnici_TelefonVerifikacija", "[TelefonVerifikovanUtc] IS NULL OR [Telefon] IS NOT NULL");
        });
        b.HasKey(k => k.Id);
        b.AuditPolja();

        b.Tekst(k => k.Email, 256);
        b.Tekst(k => k.NormalizovanEmail, 256);
        b.Tekst(k => k.LozinkaHash, 200).IsUnicode(false);
        b.Tekst(k => k.Ime, 50);
        b.Tekst(k => k.Prezime, 50);
        b.OpcioniTekst(k => k.OKorisniku, 500);
        b.OpcioniTekst(k => k.Telefon, 20).IsUnicode(false);
        b.Property(k => k.ProsecnaOcena).HasPrecision(3, 2);
        b.Property(k => k.PrikaziTelefonKupcu).HasDefaultValue(true);

        // UNIQUE nad normalizovanim emailom (case-insensitive jedinstvenost).
        b.HasIndex(k => k.NormalizovanEmail).IsUnique().HasDatabaseName("UX_Korisnici_NormalizovanEmail");
        // Filtrirani UNIQUE: više korisnika može nemati telefon (NULL), ali isti broj ne mogu imati dva aktivna naloga.
        b.HasIndex(k => k.Telefon).IsUnique()
            .HasFilter("[Telefon] IS NOT NULL AND [Obrisan] = 0")
            .HasDatabaseName("UX_Korisnici_Telefon");

        b.HasOne(k => k.Grad).WithMany().HasForeignKey(k => k.GradId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(k => k.Uloge).WithOne(u => u.Korisnik).HasForeignKey(u => u.KorisnikId).Kaskadno();
        b.Navigation(k => k.Uloge).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.Ignore(k => k.PrikaznoIme);
        b.Ignore(k => k.TelefonVerifikovan);
        b.Ignore(k => k.Reputacija);
        b.Ignore(k => k.PouzdanProdavac);
    }
}

internal sealed class UlogaKonfiguracija : IEntityTypeConfiguration<Uloga>
{
    public void Configure(EntityTypeBuilder<Uloga> b)
    {
        b.ToTable("Uloge");
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).ValueGeneratedNever();
        b.Tekst(u => u.Naziv, 50);
        b.Tekst(u => u.Opis, 200);
        b.HasIndex(u => u.Naziv).IsUnique().HasDatabaseName("UX_Uloge_Naziv");
        b.HasData(PocetniPodaci.Uloge());
    }
}

internal sealed class KorisnikUlogaKonfiguracija : IEntityTypeConfiguration<KorisnikUloga>
{
    public void Configure(EntityTypeBuilder<KorisnikUloga> b)
    {
        b.ToTable("KorisnikUloge");
        // Kompozitni primarni ključ: ista uloga ne može dva puta biti dodeljena istom korisniku.
        b.HasKey(ku => new { ku.KorisnikId, ku.UlogaId });
        b.HasOne(ku => ku.Uloga).WithMany().HasForeignKey(ku => ku.UlogaId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(ku => ku.UlogaId).HasDatabaseName("IX_KorisnikUloge_UlogaId");
    }
}

internal sealed class RefreshTokenKonfiguracija : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokeni", t =>
            t.HasCheckConstraint("CK_RefreshTokeni_Istek", "[IsticeUtc] > [KreiranoUtc]"));
        b.HasKey(r => r.Id);
        // SHA-256 heš u heksadecimalnom zapisu = tačno 64 znaka.
        b.Property(r => r.TokenHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        b.OpcioniTekst(r => r.ZamenjenHashom, 64).IsFixedLength().IsUnicode(false);
        b.OpcioniTekst(r => r.RazlogOpoziva, 100);
        b.OpcioniTekst(r => r.KreiranSaIp, 45).IsUnicode(false);

        b.HasIndex(r => r.TokenHash).IsUnique().HasDatabaseName("UX_RefreshTokeni_TokenHash");
        b.HasIndex(r => r.PorodicaId).HasDatabaseName("IX_RefreshTokeni_PorodicaId");
        b.HasIndex(r => new { r.KorisnikId, r.OpozvanoUtc }).HasDatabaseName("IX_RefreshTokeni_Korisnik_Opozvano");

        b.HasOne(r => r.Korisnik).WithMany().HasForeignKey(r => r.KorisnikId).Kaskadno();
        b.Ignore(r => r.JeOpozvan);
    }
}

internal sealed class VerifikacijaKonfiguracija : IEntityTypeConfiguration<VerifikacijaKorisnika>
{
    public void Configure(EntityTypeBuilder<VerifikacijaKorisnika> b)
    {
        b.ToTable("VerifikacijeKorisnika", t =>
        {
            t.HasCheckConstraint("CK_VerifikacijeKorisnika_Tip", KonfiguracijaPomoc.EnumIn<TipVerifikacije>("Tip"));
            t.HasCheckConstraint("CK_VerifikacijeKorisnika_Pokusaji", "[BrojPokusaja] >= 0");
        });
        b.HasKey(v => v.Id);
        b.Property(v => v.Tip).KaoTekst();
        b.Tekst(v => v.Vrednost, 100);
        b.Property(v => v.KodHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        b.HasIndex(v => new { v.KorisnikId, v.Tip, v.KreiranoUtc }).HasDatabaseName("IX_Verifikacije_Korisnik_Tip");
        b.HasOne(v => v.Korisnik).WithMany().HasForeignKey(v => v.KorisnikId).Kaskadno();
    }
}

internal sealed class BlokadaKorisnikaKonfiguracija : IEntityTypeConfiguration<BlokadaKorisnika>
{
    public void Configure(EntityTypeBuilder<BlokadaKorisnika> b)
    {
        b.ToTable("BlokiraniKorisnici", t =>
        {
            t.HasCheckConstraint("CK_BlokiraniKorisnici_Kraj", "[KrajUtc] IS NULL OR [KrajUtc] > [PocetakUtc]");
            t.HasCheckConstraint("CK_BlokiraniKorisnici_NeSebe", "[KorisnikId] <> [BlokiraoKorisnikId]");
        });
        b.HasKey(x => x.Id);
        b.Tekst(x => x.Razlog, 500);
        b.HasIndex(x => new { x.KorisnikId, x.UkinutoUtc, x.KrajUtc }).HasDatabaseName("IX_BlokiraniKorisnici_Aktivne");
        b.HasOne(x => x.Korisnik).WithMany().HasForeignKey(x => x.KorisnikId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.BlokiraoKorisnik).WithMany().HasForeignKey(x => x.BlokiraoKorisnikId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Korisnik>().WithMany().HasForeignKey(x => x.UkinuoKorisnikId).OnDelete(DeleteBehavior.Restrict);
    }
}
