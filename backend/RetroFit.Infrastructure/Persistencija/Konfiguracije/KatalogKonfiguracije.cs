using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Infrastructure.Persistencija.Seed;

namespace RetroFit.Infrastructure.Persistencija.Konfiguracije;

internal sealed class GradKonfiguracija : IEntityTypeConfiguration<Grad>
{
    /// <summary>Naziv shadow svojstva sa SQL Server tipom geography (SRID 4326 = WGS84, GPS koordinate).</summary>
    public const string Lokacija = "Lokacija";

    public void Configure(EntityTypeBuilder<Grad> b)
    {
        b.ToTable("Gradovi", t =>
        {
            t.HasCheckConstraint("CK_Gradovi_Sirina", "[GeografskaSirina] BETWEEN -90 AND 90");
            t.HasCheckConstraint("CK_Gradovi_Duzina", "[GeografskaDuzina] BETWEEN -180 AND 180");
        });
        b.HasKey(g => g.Id);
        b.Property(g => g.Id).ValueGeneratedNever();
        b.Tekst(g => g.Naziv, 100);
        b.Tekst(g => g.PostanskiBroj, 10).IsUnicode(false);
        b.HasIndex(g => g.Naziv).IsUnique().HasDatabaseName("UX_Gradovi_Naziv");

        // Shadow property: postoji samo u bazi i EF modelu, ne i u domenskoj klasi,
        // pa Domain ostaje bez zavisnosti od NetTopologySuite biblioteke.
        // Prostorni (SPATIAL) indeks nad ovom kolonom pravi se u migraciji (EF ga ne podržava fluent API-jem).
        b.Property<Point>(Lokacija).HasColumnType("geography").IsRequired();

        b.HasData(PocetniPodaci.Gradovi());
    }
}

internal sealed class KategorijaKonfiguracija : IEntityTypeConfiguration<Kategorija>
{
    public void Configure(EntityTypeBuilder<Kategorija> b)
    {
        b.ToTable("Kategorije", t =>
            t.HasCheckConstraint("CK_Kategorije_NijeSamaSebiRoditelj", "[RoditeljId] IS NULL OR [RoditeljId] <> [Id]"));
        b.HasKey(k => k.Id);
        b.AuditPolja();
        b.Tekst(k => k.Naziv, 100);
        b.Tekst(k => k.Slug, 120).IsUnicode(false);
        b.OpcioniTekst(k => k.Ikonica, 50);
        b.Property(k => k.Aktivna).HasDefaultValue(true);

        b.HasIndex(k => k.Slug).IsUnique().HasDatabaseName("UX_Kategorije_Slug");
        b.HasIndex(k => new { k.RoditeljId, k.Redosled }).HasDatabaseName("IX_Kategorije_Roditelj_Redosled");

        // Samoreferencirajuća veza (hijerarhija). Restrict: ne može se obrisati kategorija koja ima podkategorije.
        b.HasOne(k => k.Roditelj).WithMany(k => k.Podkategorije).HasForeignKey(k => k.RoditeljId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Navigation(k => k.Podkategorije).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(k => k.DefinicijeAtributa).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasData(PocetniPodaci.Kategorije());
    }
}

internal sealed class DefinicijaAtributaKonfiguracija : IEntityTypeConfiguration<DefinicijaAtributa>
{
    public void Configure(EntityTypeBuilder<DefinicijaAtributa> b)
    {
        b.ToTable("DefinicijeAtributa", t =>
        {
            t.HasCheckConstraint("CK_DefinicijeAtributa_Tip", KonfiguracijaPomoc.EnumIn<TipAtributa>("Tip"));
            // Atribut tipa Izbor mora imati opcije; ostali tipovi ne smeju.
            t.HasCheckConstraint("CK_DefinicijeAtributa_Opcije",
                "([Tip] = 'Izbor' AND [Opcije] IS NOT NULL) OR ([Tip] <> 'Izbor' AND [Opcije] IS NULL)");
        });
        b.HasKey(d => d.Id);
        b.Tekst(d => d.Naziv, 100);
        b.Tekst(d => d.Kljuc, 60).IsUnicode(false);
        b.Property(d => d.Tip).KaoTekst();
        b.OpcioniTekst(d => d.Opcije, 1000);
        b.OpcioniTekst(d => d.JedinicaMere, 20);
        b.HasIndex(d => new { d.KategorijaId, d.Kljuc }).IsUnique().HasDatabaseName("UX_DefinicijeAtributa_Kategorija_Kljuc");
        b.HasOne(d => d.Kategorija).WithMany(k => k.DefinicijeAtributa).HasForeignKey(d => d.KategorijaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Ignore(d => d.ListaOpcija);

        b.HasData(PocetniPodaci.DefinicijeAtributa());
    }
}

internal sealed class OglasKonfiguracija : IEntityTypeConfiguration<Oglas>
{
    public void Configure(EntityTypeBuilder<Oglas> b)
    {
        b.ToTable("Oglasi", t =>
        {
            t.HasCheckConstraint("CK_Oglasi_Status", KonfiguracijaPomoc.EnumIn<StatusOglasa>("Status"));
            t.HasCheckConstraint("CK_Oglasi_Tip", KonfiguracijaPomoc.EnumIn<TipOglasa>("Tip"));
            t.HasCheckConstraint("CK_Oglasi_Stanje", KonfiguracijaPomoc.EnumIn<StanjeProizvoda>("Stanje"));
            // Pravilo cene po tipu oglasa: prodaja ima cenu > 0, razmena i poklon nemaju cenu.
            t.HasCheckConstraint("CK_Oglasi_CenaPoTipu",
                "([Tip] = 'Prodaja' AND [Cena] IS NOT NULL AND [Cena] > 0) OR ([Tip] IN ('Razmena', 'Poklon') AND [Cena] IS NULL)");
            t.HasCheckConstraint("CK_Oglasi_NacinPreuzimanja", "[DostavaMoguca] = 1 OR [LicnoPreuzimanjeMoguce] = 1");
            // Svaki oglas koji je bar jednom objavljen mora imati datum objave i isteka.
            t.HasCheckConstraint("CK_Oglasi_DatumiObjave",
                "[Status] = 'Nacrt' OR ([ObjavljenoUtc] IS NOT NULL AND [IsticeUtc] IS NOT NULL)");
            t.HasCheckConstraint("CK_Oglasi_IstekPosleObjave", "[IsticeUtc] IS NULL OR [IsticeUtc] > [ObjavljenoUtc]");
            t.HasCheckConstraint("CK_Oglasi_Brojaci", "[BrojPregleda] >= 0 AND [BrojOmiljenih] >= 0");
        });
        b.HasKey(o => o.Id).HasName("PK_Oglasi");
        b.AuditPolja();

        b.Tekst(o => o.Naslov, 120);
        b.Tekst(o => o.Opis, 4000);
        b.OpcioniTekst(o => o.ZeljenaZamena, 300);
        b.OpcioniTekst(o => o.RazlogUklanjanja, 500);
        b.Property(o => o.Status).KaoTekst();
        b.Property(o => o.Tip).KaoTekst();
        b.Property(o => o.Stanje).KaoTekst();
        b.Property(o => o.RowVersion).IsRowVersion();

        // Indeksi za najčešće upite. Filtrirani (WHERE Obrisan = 0) su manji i brži jer ne sadrže obrisane oglase.
        // 1) Javna pretraga: status + kategorija, sortirano po datumu objave (najnovije prvo).
        b.HasIndex(o => new { o.Status, o.KategorijaId, o.ObjavljenoUtc })
            .IsDescending(false, false, true)
            .IncludeProperties(o => new { o.Cena, o.GradId, o.Stanje, o.Tip })
            .HasFilter("[Obrisan] = 0")
            .HasDatabaseName("IX_Oglasi_Pretraga_Kategorija");
        // 2) Pretraga po ceni.
        b.HasIndex(o => new { o.Status, o.Cena })
            .HasFilter("[Obrisan] = 0")
            .HasDatabaseName("IX_Oglasi_Pretraga_Cena");
        // 3) Pretraga po gradu.
        b.HasIndex(o => new { o.Status, o.GradId, o.ObjavljenoUtc })
            .HasFilter("[Obrisan] = 0")
            .HasDatabaseName("IX_Oglasi_Pretraga_Grad");
        // 4) "Moji oglasi".
        b.HasIndex(o => new { o.ProdavacId, o.Status }).HasDatabaseName("IX_Oglasi_Prodavac_Status");
        // 5) Pozadinski posao za istek oglasa: samo aktivni, sortirani po roku.
        b.HasIndex(o => o.IsticeUtc)
            .HasFilter("[Status] = 'Aktivan' AND [Obrisan] = 0")
            .HasDatabaseName("IX_Oglasi_ZaIstek");
        // 6) Popularnost.
        b.HasIndex(o => new { o.Status, o.BrojOmiljenih, o.BrojPregleda })
            .HasFilter("[Obrisan] = 0")
            .HasDatabaseName("IX_Oglasi_Popularnost");

        b.HasOne(o => o.Prodavac).WithMany().HasForeignKey(o => o.ProdavacId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(o => o.Kategorija).WithMany().HasForeignKey(o => o.KategorijaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(o => o.Grad).WithMany().HasForeignKey(o => o.GradId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(o => o.Slike).WithOne(s => s.Oglas).HasForeignKey(s => s.OglasId).Kaskadno();
        b.HasMany(o => o.Atributi).WithOne(a => a.Oglas).HasForeignKey(a => a.OglasId).Kaskadno();
        b.Navigation(o => o.Slike).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(o => o.Atributi).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.Ignore(o => o.JeJavnoVidljiv);
    }
}

internal sealed class SlikaOglasaKonfiguracija : IEntityTypeConfiguration<SlikaOglasa>
{
    public void Configure(EntityTypeBuilder<SlikaOglasa> b)
    {
        b.ToTable("SlikeOglasa", t =>
        {
            // Najviše 10 slika po oglasu: redosled 0..9 + UNIQUE (OglasId, Redosled).
            t.HasCheckConstraint("CK_SlikeOglasa_Redosled", "[Redosled] BETWEEN 0 AND 9");
            t.HasCheckConstraint("CK_SlikeOglasa_Velicina", "[VelicinaBajtova] > 0 AND [VelicinaBajtova] <= 5242880");
            t.HasCheckConstraint("CK_SlikeOglasa_Tip", "[TipSadrzaja] IN ('image/jpeg', 'image/png', 'image/webp')");
        });
        b.HasKey(s => s.Id);
        b.Tekst(s => s.Putanja, 300);
        b.Tekst(s => s.TipSadrzaja, 50).IsUnicode(false);
        b.HasIndex(s => new { s.OglasId, s.Redosled }).IsUnique().HasDatabaseName("UX_SlikeOglasa_Oglas_Redosled");
    }
}

internal sealed class AtributOglasaKonfiguracija : IEntityTypeConfiguration<AtributOglasa>
{
    public void Configure(EntityTypeBuilder<AtributOglasa> b)
    {
        b.ToTable("AtributiOglasa");
        // Kompozitni PK: jedan atribut najviše jednom po oglasu.
        b.HasKey(a => new { a.OglasId, a.DefinicijaAtributaId });
        b.Tekst(a => a.Vrednost, 200);
        b.HasOne(a => a.DefinicijaAtributa).WithMany().HasForeignKey(a => a.DefinicijaAtributaId)
            .OnDelete(DeleteBehavior.Restrict);
        // Pretraga oglasa po vrednosti atributa (npr. svi automobili na dizel).
        b.HasIndex(a => new { a.DefinicijaAtributaId, a.Vrednost }).HasDatabaseName("IX_AtributiOglasa_Definicija_Vrednost");
    }
}
