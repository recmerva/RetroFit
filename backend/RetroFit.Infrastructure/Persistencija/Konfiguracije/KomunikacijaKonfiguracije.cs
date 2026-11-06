using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Infrastructure.Persistencija.Konfiguracije;

internal sealed class OmiljeniOglasKonfiguracija : IEntityTypeConfiguration<OmiljeniOglas>
{
    public void Configure(EntityTypeBuilder<OmiljeniOglas> b)
    {
        b.ToTable("OmiljeniOglasi");
        b.HasKey(o => new { o.KorisnikId, o.OglasId });
        b.HasIndex(o => o.OglasId).HasDatabaseName("IX_OmiljeniOglasi_OglasId");
        b.HasIndex(o => new { o.KorisnikId, o.DodatoUtc }).IsDescending(false, true).HasDatabaseName("IX_OmiljeniOglasi_Korisnik_Datum");
        b.HasOne(o => o.Korisnik).WithMany().HasForeignKey(o => o.KorisnikId).Kaskadno();
        b.HasOne(o => o.Oglas).WithMany().HasForeignKey(o => o.OglasId).Kaskadno();
    }
}

internal sealed class SacuvanaPretragaKonfiguracija : IEntityTypeConfiguration<SacuvanaPretraga>
{
    public void Configure(EntityTypeBuilder<SacuvanaPretraga> b)
    {
        b.ToTable("SacuvanePretrage", t =>
        {
            t.HasCheckConstraint("CK_SacuvanePretrage_Cene", "[CenaOd] IS NULL OR [CenaDo] IS NULL OR [CenaOd] <= [CenaDo]");
            t.HasCheckConstraint("CK_SacuvanePretrage_Stanje", KonfiguracijaPomoc.EnumIn<StanjeProizvoda>("Stanje", nullable: true));
            t.HasCheckConstraint("CK_SacuvanePretrage_Tip", KonfiguracijaPomoc.EnumIn<TipOglasa>("Tip", nullable: true));
        });
        b.HasKey(p => p.Id);
        b.Tekst(p => p.Naziv, 100);
        b.OpcioniTekst(p => p.Upit, 200);
        b.Property(p => p.Stanje).KaoTekst();
        b.Property(p => p.Tip).KaoTekst();
        b.HasIndex(p => p.KorisnikId).HasDatabaseName("IX_SacuvanePretrage_KorisnikId");
        b.HasIndex(p => p.PoslednjaProveraUtc).HasFilter("[Obavestavaj] = 1").HasDatabaseName("IX_SacuvanePretrage_ZaObavestavanje");
        b.HasOne(p => p.Korisnik).WithMany().HasForeignKey(p => p.KorisnikId).Kaskadno();
        b.HasOne(p => p.Kategorija).WithMany().HasForeignKey(p => p.KategorijaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Grad).WithMany().HasForeignKey(p => p.GradId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IstaknutiOglasKonfiguracija : IEntityTypeConfiguration<IstaknutiOglas>
{
    public void Configure(EntityTypeBuilder<IstaknutiOglas> b)
    {
        b.ToTable("IstaknutiOglasi", t =>
        {
            t.HasCheckConstraint("CK_IstaknutiOglasi_Period", "[KrajUtc] > [PocetakUtc]");
            t.HasCheckConstraint("CK_IstaknutiOglasi_Tip", KonfiguracijaPomoc.EnumIn<TipIsticanja>("Tip"));
            // Plaćeno podizanje mora imati plaćanje, admin izdvajanje ne sme.
            t.HasCheckConstraint("CK_IstaknutiOglasi_Placanje",
                "([Tip] = 'Podignut' AND [PlacanjeId] IS NOT NULL) OR ([Tip] = 'Izdvojen' AND [PlacanjeId] IS NULL)");
        });
        b.HasKey(i => i.Id);
        b.Property(i => i.Tip).KaoTekst();
        b.HasIndex(i => new { i.KrajUtc, i.PocetakUtc }).IncludeProperties(i => new { i.OglasId, i.Tip })
            .HasFilter("[OtkazanoUtc] IS NULL").HasDatabaseName("IX_IstaknutiOglasi_Aktivni");
        b.HasIndex(i => i.OglasId).HasDatabaseName("IX_IstaknutiOglasi_OglasId");
        // Jedno plaćanje = najviše jedno podizanje.
        b.HasIndex(i => i.PlacanjeId).IsUnique().HasFilter("[PlacanjeId] IS NOT NULL").HasDatabaseName("UX_IstaknutiOglasi_PlacanjeId");
        b.HasOne(i => i.Oglas).WithMany().HasForeignKey(i => i.OglasId).Kaskadno();
        b.HasOne(i => i.KreiraoKorisnik).WithMany().HasForeignKey(i => i.KreiraoKorisnikId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(i => i.Placanje).WithMany().HasForeignKey(i => i.PlacanjeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RazgovorKonfiguracija : IEntityTypeConfiguration<Razgovor>
{
    public void Configure(EntityTypeBuilder<Razgovor> b)
    {
        b.ToTable("Razgovori", t =>
            t.HasCheckConstraint("CK_Razgovori_RazlicitiUcesnici", "[KupacId] <> [ProdavacId]"));
        b.HasKey(r => r.Id);
        // Jedan kupac ima najviše jedan razgovor po oglasu.
        b.HasIndex(r => new { r.OglasId, r.KupacId }).IsUnique().HasDatabaseName("UX_Razgovori_Oglas_Kupac");
        b.HasIndex(r => new { r.KupacId, r.PoslednjaPorukaUtc }).IsDescending(false, true).HasDatabaseName("IX_Razgovori_Kupac");
        b.HasIndex(r => new { r.ProdavacId, r.PoslednjaPorukaUtc }).IsDescending(false, true).HasDatabaseName("IX_Razgovori_Prodavac");
        b.HasOne(r => r.Oglas).WithMany().HasForeignKey(r => r.OglasId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Kupac).WithMany().HasForeignKey(r => r.KupacId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Prodavac).WithMany().HasForeignKey(r => r.ProdavacId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(r => r.Poruke).WithOne(p => p.Razgovor).HasForeignKey(p => p.RazgovorId).Kaskadno();
        b.HasMany(r => r.Ponude).WithOne(p => p.Razgovor).HasForeignKey(p => p.RazgovorId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(r => r.Poruke).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(r => r.Ponude).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PorukaKonfiguracija : IEntityTypeConfiguration<Poruka>
{
    public void Configure(EntityTypeBuilder<Poruka> b)
    {
        b.ToTable("Poruke", t =>
            t.HasCheckConstraint("CK_Poruke_Tip", KonfiguracijaPomoc.EnumIn<TipPoruke>("Tip")));
        b.HasKey(p => p.Id);
        b.Tekst(p => p.Sadrzaj, 2000);
        b.Property(p => p.Tip).KaoTekst();
        b.HasIndex(p => new { p.RazgovorId, p.PoslatoUtc }).IsDescending(false, true).HasDatabaseName("IX_Poruke_Razgovor_Vreme");
        // Brojanje nepročitanih poruka: indeks samo nad nepročitanim (mali i brz).
        b.HasIndex(p => new { p.RazgovorId, p.PosiljalacId }).HasFilter("[ProcitanoUtc] IS NULL").HasDatabaseName("IX_Poruke_Neprocitane");
        b.HasOne(p => p.Posiljalac).WithMany().HasForeignKey(p => p.PosiljalacId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Ponuda).WithMany().HasForeignKey(p => p.PonudaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PonudaKonfiguracija : IEntityTypeConfiguration<Ponuda>
{
    public void Configure(EntityTypeBuilder<Ponuda> b)
    {
        b.ToTable("Ponude", t =>
        {
            t.HasCheckConstraint("CK_Ponude_Status", KonfiguracijaPomoc.EnumIn<StatusPonude>("Status"));
            t.HasCheckConstraint("CK_Ponude_Iznos", "[Iznos] > 0");
            t.HasCheckConstraint("CK_Ponude_Istek", "[IsticeUtc] > [KreiranoUtc]");
            t.HasCheckConstraint("CK_Ponude_NijeSamaSebiPrethodna", "[PrethodnaPonudaId] IS NULL OR [PrethodnaPonudaId] <> [Id]");
        });
        b.HasKey(p => p.Id);
        b.Property(p => p.Status).KaoTekst();
        b.OpcioniTekst(p => p.Napomena, 500);
        b.Property(p => p.RowVersion).IsRowVersion();

        // Najviše jedna ponuda na čekanju po razgovoru (filtrirani UNIQUE indeks).
        b.HasIndex(p => p.RazgovorId).IsUnique().HasFilter("[Status] = 'NaCekanju'").HasDatabaseName("UX_Ponude_JednaNaCekanju");
        b.HasIndex(p => new { p.OglasId, p.Status }).HasDatabaseName("IX_Ponude_Oglas_Status");
        b.HasIndex(p => p.IsticeUtc).HasFilter("[Status] = 'NaCekanju'").HasDatabaseName("IX_Ponude_ZaIstek");
        b.HasIndex(p => new { p.KupacId, p.KreiranoUtc }).HasDatabaseName("IX_Ponude_Kupac");

        b.HasOne(p => p.Oglas).WithMany().HasForeignKey(p => p.OglasId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Kupac).WithMany().HasForeignKey(p => p.KupacId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.PonudioKorisnik).WithMany().HasForeignKey(p => p.PonudioKorisnikId).OnDelete(DeleteBehavior.Restrict);
        // Samoreferencirajuća veza: lanac kontraponuda.
        b.HasOne(p => p.PrethodnaPonuda).WithMany().HasForeignKey(p => p.PrethodnaPonudaId).OnDelete(DeleteBehavior.Restrict);
        b.Ignore(p => p.OdgovaraKorisnikId);
    }
}

internal sealed class ObavestenjeKonfiguracija : IEntityTypeConfiguration<Obavestenje>
{
    public void Configure(EntityTypeBuilder<Obavestenje> b)
    {
        b.ToTable("Obavestenja", t =>
            t.HasCheckConstraint("CK_Obavestenja_Tip", KonfiguracijaPomoc.EnumIn<TipObavestenja>("Tip")));
        b.HasKey(o => o.Id);
        b.Property(o => o.Tip).KaoTekst(40);
        b.Tekst(o => o.Naslov, 150);
        b.Tekst(o => o.Sadrzaj, 1000);
        b.OpcioniTekst(o => o.Link, 300);
        b.HasIndex(o => new { o.KorisnikId, o.KreiranoUtc }).IsDescending(false, true).HasDatabaseName("IX_Obavestenja_Korisnik_Datum");
        b.HasIndex(o => o.KorisnikId).HasFilter("[ProcitanoUtc] IS NULL").HasDatabaseName("IX_Obavestenja_Neprocitana");
        b.HasOne(o => o.Korisnik).WithMany().HasForeignKey(o => o.KorisnikId).Kaskadno();
    }
}
