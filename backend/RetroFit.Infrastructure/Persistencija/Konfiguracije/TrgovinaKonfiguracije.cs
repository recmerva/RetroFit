using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Infrastructure.Persistencija.Konfiguracije;

internal sealed class RezervacijaKonfiguracija : IEntityTypeConfiguration<Rezervacija>
{
    public void Configure(EntityTypeBuilder<Rezervacija> b)
    {
        b.ToTable("Rezervacije", t =>
        {
            t.HasCheckConstraint("CK_Rezervacije_Status", KonfiguracijaPomoc.EnumIn<StatusRezervacije>("Status"));
            t.HasCheckConstraint("CK_Rezervacije_RazlicitiUcesnici", "[KupacId] <> [ProdavacId]");
            t.HasCheckConstraint("CK_Rezervacije_Cena", "[DogovorenaCena] IS NULL OR [DogovorenaCena] > 0");
            t.HasCheckConstraint("CK_Rezervacije_Istek", "[IsticeUtc] > [KreiranoUtc]");
            // Zatvorena rezervacija mora imati datum zatvaranja.
            t.HasCheckConstraint("CK_Rezervacije_Zavrsetak", "[Status] = 'Aktivna' OR [ZavrsenoUtc] IS NOT NULL");
        });
        b.HasKey(r => r.Id);
        b.Property(r => r.Status).KaoTekst();
        b.OpcioniTekst(r => r.RazlogOtkazivanja, 500);
        b.Property(r => r.RowVersion).IsRowVersion();

        // KLJUČNO PRAVILO: oglas može imati najviše jednu AKTIVNU rezervaciju.
        // Čak i ako dva zahteva istovremeno prođu proveru u aplikaciji, baza odbija drugi INSERT.
        b.HasIndex(r => r.OglasId).IsUnique().HasFilter("[Status] = 'Aktivna'").HasDatabaseName("UX_Rezervacije_JednaAktivnaPoOglasu");
        // Jedna prihvaćena ponuda daje najviše jednu rezervaciju.
        b.HasIndex(r => r.PonudaId).IsUnique().HasFilter("[PonudaId] IS NOT NULL").HasDatabaseName("UX_Rezervacije_PonudaId");
        b.HasIndex(r => new { r.KupacId, r.Status }).HasDatabaseName("IX_Rezervacije_Kupac");
        b.HasIndex(r => new { r.ProdavacId, r.Status }).HasDatabaseName("IX_Rezervacije_Prodavac");
        b.HasIndex(r => r.IsticeUtc).HasFilter("[Status] = 'Aktivna'").HasDatabaseName("IX_Rezervacije_ZaIstek");

        b.HasOne(r => r.Oglas).WithMany().HasForeignKey(r => r.OglasId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Kupac).WithMany().HasForeignKey(r => r.KupacId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Prodavac).WithMany().HasForeignKey(r => r.ProdavacId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Ponuda).WithMany().HasForeignKey(r => r.PonudaId).OnDelete(DeleteBehavior.Restrict);
        b.Ignore(r => r.JeAktivna);
    }
}

internal sealed class TransakcijaKonfiguracija : IEntityTypeConfiguration<Transakcija>
{
    public void Configure(EntityTypeBuilder<Transakcija> b)
    {
        b.ToTable("Transakcije", t =>
        {
            t.HasCheckConstraint("CK_Transakcije_Status", KonfiguracijaPomoc.EnumIn<StatusTransakcije>("Status"));
            t.HasCheckConstraint("CK_Transakcije_StatusEscrowa", KonfiguracijaPomoc.EnumIn<StatusEscrowa>("StatusEscrowa"));
            t.HasCheckConstraint("CK_Transakcije_NacinPreuzimanja", KonfiguracijaPomoc.EnumIn<NacinPreuzimanja>("NacinPreuzimanja"));
            t.HasCheckConstraint("CK_Transakcije_RazlicitiUcesnici", "[KupacId] <> [ProdavacId]");
            t.HasCheckConstraint("CK_Transakcije_Cena", "[Cena] IS NULL OR [Cena] > 0");
            t.HasCheckConstraint("CK_Transakcije_AdresaZaDostavu", "[NacinPreuzimanja] <> 'Dostava' OR [AdresaDostave] IS NOT NULL");
            t.HasCheckConstraint("CK_Transakcije_EscrowZahtevaCenu", "[StatusEscrowa] = 'NijeKoriscen' OR [Cena] IS NOT NULL");
            // Završena transakcija = obe strane potvrdile + datum završetka.
            t.HasCheckConstraint("CK_Transakcije_Zavrsena",
                "[Status] <> 'Zavrsena' OR ([KupacPotvrdioUtc] IS NOT NULL AND [ProdavacPotvrdioUtc] IS NOT NULL AND [ZavrsenoUtc] IS NOT NULL)");
            t.HasCheckConstraint("CK_Transakcije_Otkazana", "[Status] <> 'Otkazana' OR [OtkazanoUtc] IS NOT NULL");
        });
        b.HasKey(t => t.Id);
        b.AuditPolja();
        b.Property(t => t.Status).KaoTekst();
        b.Property(t => t.StatusEscrowa).KaoTekst();
        b.Property(t => t.NacinPreuzimanja).KaoTekst();
        b.OpcioniTekst(t => t.AdresaDostave, 300);
        b.OpcioniTekst(t => t.RazlogOtkazivanja, 500);
        b.Property(t => t.RowVersion).IsRowVersion();

        // Rezervacija -> najviše jedna transakcija (1:1).
        b.HasIndex(t => t.RezervacijaId).IsUnique().HasDatabaseName("UX_Transakcije_RezervacijaId");
        // KLJUČNO PRAVILO: oglas ne može biti prodat dva puta.
        // Dozvoljeno je više OTKAZANIH transakcija za isti oglas, ali samo jedna koja nije otkazana.
        b.HasIndex(t => t.OglasId).IsUnique().HasFilter("[Status] <> 'Otkazana'").HasDatabaseName("UX_Transakcije_OglasNeotkazana");
        b.HasIndex(t => new { t.KupacId, t.Status }).HasDatabaseName("IX_Transakcije_Kupac");
        b.HasIndex(t => new { t.ProdavacId, t.Status }).HasDatabaseName("IX_Transakcije_Prodavac");

        b.HasOne(t => t.Oglas).WithMany().HasForeignKey(t => t.OglasId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.Rezervacija).WithMany().HasForeignKey(t => t.RezervacijaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.Kupac).WithMany().HasForeignKey(t => t.KupacId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.Prodavac).WithMany().HasForeignKey(t => t.ProdavacId).OnDelete(DeleteBehavior.Restrict);
        b.Ignore(t => t.KoristiEscrow);
    }
}

internal sealed class PlacanjeKonfiguracija : IEntityTypeConfiguration<Placanje>
{
    public void Configure(EntityTypeBuilder<Placanje> b)
    {
        b.ToTable("Placanja", t =>
        {
            t.HasCheckConstraint("CK_Placanja_Tip", KonfiguracijaPomoc.EnumIn<TipPlacanja>("Tip"));
            t.HasCheckConstraint("CK_Placanja_Status", KonfiguracijaPomoc.EnumIn<StatusPlacanja>("Status"));
            t.HasCheckConstraint("CK_Placanja_Iznos", "[Iznos] > 0");
            // Escrow knjiženja moraju biti vezana za transakciju.
            t.HasCheckConstraint("CK_Placanja_EscrowTransakcija", "[Tip] = 'Isticanje' OR [TransakcijaId] IS NOT NULL");
        });
        b.HasKey(p => p.Id);
        b.Property(p => p.Tip).KaoTekst();
        b.Property(p => p.Status).KaoTekst();
        b.Property(p => p.Valuta).HasMaxLength(3).IsFixedLength().IsUnicode(false).HasDefaultValue("RSD");
        b.Tekst(p => p.ReferencaProcesora, 64).IsUnicode(false);
        b.OpcioniTekst(p => p.KljucIdempotentnosti, 64).IsUnicode(false);
        b.Tekst(p => p.Opis, 300);

        b.HasIndex(p => p.ReferencaProcesora).IsUnique().HasDatabaseName("UX_Placanja_Referenca");
        // Idempotentnost: isti ključ = isto plaćanje (zaštita od dupliranja naplate).
        b.HasIndex(p => p.KljucIdempotentnosti).IsUnique().HasFilter("[KljucIdempotentnosti] IS NOT NULL")
            .HasDatabaseName("UX_Placanja_KljucIdempotentnosti");
        b.HasIndex(p => p.TransakcijaId).HasDatabaseName("IX_Placanja_TransakcijaId");
        b.HasIndex(p => new { p.KorisnikId, p.KreiranoUtc }).HasDatabaseName("IX_Placanja_Korisnik");

        b.HasOne(p => p.Korisnik).WithMany().HasForeignKey(p => p.KorisnikId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Transakcija).WithMany().HasForeignKey(p => p.TransakcijaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RecenzijaKonfiguracija : IEntityTypeConfiguration<Recenzija>
{
    public void Configure(EntityTypeBuilder<Recenzija> b)
    {
        b.ToTable("Recenzije", t =>
        {
            t.HasCheckConstraint("CK_Recenzije_Ocena", "[Ocena] BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_Recenzije_NeSebe", "[OcenjivacId] <> [OcenjeniKorisnikId]");
        });
        b.HasKey(r => r.Id);
        b.Property(r => r.Ocena).HasColumnType("tinyint").HasConversion<byte>();
        b.OpcioniTekst(r => r.Komentar, 1000);

        // KLJUČNO PRAVILO: korisnik ne može dva puta oceniti istu transakciju.
        // Pošto transakcija ima samo dva učesnika, ovo ujedno znači najviše 2 recenzije po transakciji.
        b.HasIndex(r => new { r.TransakcijaId, r.OcenjivacId }).IsUnique().HasDatabaseName("UX_Recenzije_Transakcija_Ocenjivac");
        b.HasIndex(r => new { r.OcenjeniKorisnikId, r.KreiranoUtc }).IsDescending(false, true).HasDatabaseName("IX_Recenzije_Ocenjeni");

        b.HasOne(r => r.Transakcija).WithMany().HasForeignKey(r => r.TransakcijaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Ocenjivac).WithMany().HasForeignKey(r => r.OcenjivacId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.OcenjeniKorisnik).WithMany().HasForeignKey(r => r.OcenjeniKorisnikId).OnDelete(DeleteBehavior.Restrict);
    }
}
