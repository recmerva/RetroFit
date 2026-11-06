using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Infrastructure.Persistencija.Konfiguracije;

internal sealed class ZahtevTrazimKonfiguracija : IEntityTypeConfiguration<ZahtevTrazim>
{
    public void Configure(EntityTypeBuilder<ZahtevTrazim> b)
    {
        b.ToTable("ZahteviTrazim", t =>
        {
            t.HasCheckConstraint("CK_ZahteviTrazim_Status", KonfiguracijaPomoc.EnumIn<StatusZahteva>("Status"));
            t.HasCheckConstraint("CK_ZahteviTrazim_Cena", "[MaksimalnaCena] IS NULL OR [MaksimalnaCena] > 0");
        });
        b.HasKey(z => z.Id);
        b.AuditPolja();
        b.Tekst(z => z.Naslov, 120);
        b.Tekst(z => z.Opis, 2000);
        b.Property(z => z.Status).KaoTekst();
        b.HasIndex(z => new { z.Status, z.KategorijaId, z.KreiranoUtc }).IsDescending(false, false, true)
            .HasDatabaseName("IX_ZahteviTrazim_Pretraga");
        b.HasIndex(z => z.KorisnikId).HasDatabaseName("IX_ZahteviTrazim_KorisnikId");
        b.HasOne(z => z.Korisnik).WithMany().HasForeignKey(z => z.KorisnikId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(z => z.Kategorija).WithMany().HasForeignKey(z => z.KategorijaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(z => z.Grad).WithMany().HasForeignKey(z => z.GradId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(z => z.Odgovori).WithOne(o => o.Zahtev).HasForeignKey(o => o.ZahtevId).Kaskadno();
        b.Navigation(z => z.Odgovori).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class OdgovorNaZahtevKonfiguracija : IEntityTypeConfiguration<OdgovorNaZahtev>
{
    public void Configure(EntityTypeBuilder<OdgovorNaZahtev> b)
    {
        b.ToTable("OdgovoriNaZahteve");
        b.HasKey(o => o.Id);
        b.OpcioniTekst(o => o.Poruka, 500);
        b.HasIndex(o => new { o.ZahtevId, o.OglasId }).IsUnique().HasDatabaseName("UX_OdgovoriNaZahteve_Zahtev_Oglas");
        b.HasIndex(o => o.OglasId).HasDatabaseName("IX_OdgovoriNaZahteve_OglasId");
        b.HasOne(o => o.Oglas).WithMany().HasForeignKey(o => o.OglasId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(o => o.OdgovorioKorisnik).WithMany().HasForeignKey(o => o.OdgovorioKorisnikId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PrijavaKonfiguracija : IEntityTypeConfiguration<Prijava>
{
    public void Configure(EntityTypeBuilder<Prijava> b)
    {
        b.ToTable("Prijave", t =>
        {
            t.HasCheckConstraint("CK_Prijave_Razlog", KonfiguracijaPomoc.EnumIn<RazlogPrijave>("Razlog"));
            t.HasCheckConstraint("CK_Prijave_Status", KonfiguracijaPomoc.EnumIn<StatusPrijave>("Status"));
            t.HasCheckConstraint("CK_Prijave_Obrada",
                "[Status] = 'Otvorena' OR ([ObradioModeratorId] IS NOT NULL AND [ObradjenoUtc] IS NOT NULL)");
        });
        b.HasKey(p => p.Id);
        b.Property(p => p.Razlog).KaoTekst();
        b.Property(p => p.Status).KaoTekst();
        b.OpcioniTekst(p => p.Opis, 1000);
        b.OpcioniTekst(p => p.NapomenaModeratora, 500);
        // Jedan korisnik može imati najviše jednu OTVORENU prijavu za isti oglas.
        b.HasIndex(p => new { p.OglasId, p.PrijavioKorisnikId }).IsUnique().HasFilter("[Status] = 'Otvorena'")
            .HasDatabaseName("UX_Prijave_JednaOtvorenaPoKorisniku");
        b.HasIndex(p => new { p.Status, p.KreiranoUtc }).HasDatabaseName("IX_Prijave_Status_Datum");
        b.HasOne(p => p.Oglas).WithMany().HasForeignKey(p => p.OglasId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.PrijavioKorisnik).WithMany().HasForeignKey(p => p.PrijavioKorisnikId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.ObradioModerator).WithMany().HasForeignKey(p => p.ObradioModeratorId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OtisakUredjajaKonfiguracija : IEntityTypeConfiguration<OtisakUredjaja>
{
    public void Configure(EntityTypeBuilder<OtisakUredjaja> b)
    {
        b.ToTable("OtisciUredjaja", t =>
        {
            t.HasCheckConstraint("CK_OtisciUredjaja_Rizik", "[NajveciRizik] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_OtisciUredjaja_Brojaci", "[BrojZahteva] >= 0 AND [BrojRegistracija] >= 0");
        });
        b.HasKey(o => o.Id);
        b.Property(o => o.OtisakHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        b.OpcioniTekst(o => o.OpisKlijenta, 200);
        b.HasIndex(o => o.OtisakHash).IsUnique().HasDatabaseName("UX_OtisciUredjaja_Hash");
        b.HasIndex(o => o.NajveciRizik).HasDatabaseName("IX_OtisciUredjaja_Rizik");
        b.HasOne<Korisnik>().WithMany().HasForeignKey(o => o.PoslednjiKorisnikId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class BlokadaUredjajaKonfiguracija : IEntityTypeConfiguration<BlokadaUredjaja>
{
    public void Configure(EntityTypeBuilder<BlokadaUredjaja> b)
    {
        b.ToTable("BlokiraniUredjaji", t =>
        {
            t.HasCheckConstraint("CK_BlokiraniUredjaji_Kraj", "[KrajUtc] IS NULL OR [KrajUtc] > [PocetakUtc]");
            t.HasCheckConstraint("CK_BlokiraniUredjaji_Automatska",
                "([Automatska] = 1 AND [BlokiraoKorisnikId] IS NULL) OR ([Automatska] = 0 AND [BlokiraoKorisnikId] IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.Tekst(x => x.Razlog, 500);
        b.HasIndex(x => new { x.OtisakUredjajaId, x.UkinutoUtc, x.KrajUtc }).HasDatabaseName("IX_BlokiraniUredjaji_Aktivne");
        b.HasOne(x => x.OtisakUredjaja).WithMany().HasForeignKey(x => x.OtisakUredjajaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.BlokiraoKorisnik).WithMany().HasForeignKey(x => x.BlokiraoKorisnikId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Korisnik>().WithMany().HasForeignKey(x => x.UkinuoKorisnikId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AuditZapisKonfiguracija : IEntityTypeConfiguration<AuditZapis>
{
    public void Configure(EntityTypeBuilder<AuditZapis> b)
    {
        b.ToTable("AuditZapisi", t =>
        {
            t.HasCheckConstraint("CK_AuditZapisi_Kategorija", KonfiguracijaPomoc.EnumIn<KategorijaAudita>("Kategorija"));
            // Stare i nove vrednosti se čuvaju kao JSON - baza proverava da je ispravan JSON.
            t.HasCheckConstraint("CK_AuditZapisi_StareJson", "[StareVrednosti] IS NULL OR ISJSON([StareVrednosti]) = 1");
            t.HasCheckConstraint("CK_AuditZapisi_NoveJson", "[NoveVrednosti] IS NULL OR ISJSON([NoveVrednosti]) = 1");
        });
        b.HasKey(a => a.Id);
        b.Property(a => a.Kategorija).KaoTekst();
        b.Tekst(a => a.Akcija, 100);
        b.OpcioniTekst(a => a.NazivEntiteta, 100);
        b.OpcioniTekst(a => a.EntitetId, 50);
        b.Property(a => a.StareVrednosti);
        b.Property(a => a.NoveVrednosti);
        b.OpcioniTekst(a => a.IpAdresa, 45).IsUnicode(false);
        b.OpcioniTekst(a => a.KorelacioniId, 64).IsUnicode(false);
        b.HasIndex(a => a.KreiranoUtc).IsDescending().HasDatabaseName("IX_AuditZapisi_Datum");
        b.HasIndex(a => new { a.Kategorija, a.KreiranoUtc }).HasDatabaseName("IX_AuditZapisi_Kategorija_Datum");
        b.HasIndex(a => new { a.NazivEntiteta, a.EntitetId }).HasDatabaseName("IX_AuditZapisi_Entitet");
        b.HasIndex(a => a.KorisnikId).HasDatabaseName("IX_AuditZapisi_KorisnikId");
        // SetNull: audit zapis ostaje i kada se korisnik (fizički) obriše.
        b.HasOne(a => a.Korisnik).WithMany().HasForeignKey(a => a.KorisnikId).OnDelete(DeleteBehavior.SetNull);
    }
}
