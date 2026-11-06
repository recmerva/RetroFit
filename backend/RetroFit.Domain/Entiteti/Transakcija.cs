using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Transakcija = sama kupoprodaja, nastaje iz aktivne rezervacije (RezervacijaId je UNIQUE: 1:1).
///
/// Tok:
///   1) kupac pokreće kupovinu (bira lično preuzimanje ili dostavu, i da li koristi escrow);
///   2) ako koristi escrow: kupac uplaćuje (mock) -> novac je "deponovan" (StatusEscrowa = Deponovano);
///   3) prodavac potvrđuje predaju/slanje, kupac potvrđuje prijem;
///   4) kada obe strane potvrde -> Zavrsena, oglas -> Prodat, escrow isplaćuje novac prodavcu.
///   Otkazivanje je moguće dok obe strane nisu potvrdile; deponovan novac se vraća kupcu.
///
/// U bazi: filtrirani UNIQUE (OglasId) WHERE Status &lt;&gt; 'Otkazana' -> oglas ne može biti prodat dva puta.
/// </summary>
public class Transakcija : AuditabilniEntitet
{
    private Transakcija() { }

    public int OglasId { get; private set; }
    public Oglas Oglas { get; private set; } = null!;
    public int RezervacijaId { get; private set; }
    public Rezervacija Rezervacija { get; private set; } = null!;
    public int KupacId { get; private set; }
    public Korisnik Kupac { get; private set; } = null!;
    public int ProdavacId { get; private set; }
    public Korisnik Prodavac { get; private set; } = null!;
    public decimal? Cena { get; private set; }
    public NacinPreuzimanja NacinPreuzimanja { get; private set; }
    /// <summary>Privatan podatak: vidljiv samo učesnicima transakcije.</summary>
    public string? AdresaDostave { get; private set; }
    public StatusTransakcije Status { get; private set; }
    public StatusEscrowa StatusEscrowa { get; private set; }
    public DateTime? KupacPotvrdioUtc { get; private set; }
    public DateTime? ProdavacPotvrdioUtc { get; private set; }
    public DateTime? ZavrsenoUtc { get; private set; }
    public DateTime? OtkazanoUtc { get; private set; }
    public int? OtkazaoKorisnikId { get; private set; }
    public string? RazlogOtkazivanja { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public bool KoristiEscrow => StatusEscrowa != StatusEscrowa.NijeKoriscen;
    public bool JeUcesnik(int korisnikId) => korisnikId == KupacId || korisnikId == ProdavacId;
    public int DrugaStrana(int korisnikId) => korisnikId == KupacId ? ProdavacId : KupacId;

    public static Transakcija IzRezervacije(Rezervacija rezervacija, Oglas oglas, int kupacId,
        NacinPreuzimanja nacin, string? adresaDostave, bool koristiEscrow, DateTime sada)
    {
        Provera.Uslov(rezervacija.KupacId == kupacId, "zabranjeno", "Kupovinu može pokrenuti samo kupac iz rezervacije.");
        Provera.Uslov(rezervacija.OglasId == oglas.Id, "validacija", "Rezervacija ne pripada ovom oglasu.");
        Provera.Uslov(nacin != NacinPreuzimanja.Dostava || oglas.DostavaMoguca, "validacija",
            "Prodavac ne nudi dostavu za ovaj oglas.");
        Provera.Uslov(nacin != NacinPreuzimanja.LicnoPreuzimanje || oglas.LicnoPreuzimanjeMoguce, "validacija",
            "Prodavac ne nudi lično preuzimanje za ovaj oglas.");
        Provera.Uslov(nacin != NacinPreuzimanja.Dostava || !string.IsNullOrWhiteSpace(adresaDostave), "validacija",
            "Za dostavu je obavezna adresa.");
        Provera.Uslov(!koristiEscrow || rezervacija.DogovorenaCena is > 0, "validacija",
            "Escrow je moguć samo za oglase sa cenom.");

        rezervacija.Realizuj(sada);

        return new Transakcija
        {
            Oglas = oglas,
            OglasId = oglas.Id,
            Rezervacija = rezervacija,
            RezervacijaId = rezervacija.Id,
            KupacId = rezervacija.KupacId,
            ProdavacId = rezervacija.ProdavacId,
            Cena = rezervacija.DogovorenaCena,
            NacinPreuzimanja = nacin,
            AdresaDostave = nacin == NacinPreuzimanja.Dostava ? Provera.Tekst(adresaDostave, "Adresa dostave", 300) : null,
            Status = StatusTransakcije.UToku,
            StatusEscrowa = koristiEscrow ? StatusEscrowa.CekaUplatu : StatusEscrowa.NijeKoriscen
        };
    }

    /// <summary>Kupac je uplatio novac na escrow (mock). Novac se drži dok obe strane ne potvrde.</summary>
    public void EvidentirajUplatu(int korisnikId)
    {
        ProveriUToku();
        Provera.Uslov(korisnikId == KupacId, "zabranjeno", "Uplatu vrši kupac.");
        Provera.Uslov(StatusEscrowa == StatusEscrowa.CekaUplatu, "escrow_stanje",
            "Uplata nije očekivana za ovu transakciju.");
        StatusEscrowa = StatusEscrowa.Deponovano;
    }

    /// <summary>
    /// Potvrda jedne strane. Vraća true ako je ovom potvrdom transakcija završena.
    /// Kupac ne može potvrditi prijem pre nego što uplati na escrow (ako se escrow koristi).
    /// </summary>
    public bool Potvrdi(int korisnikId, DateTime sada)
    {
        ProveriUToku();
        Provera.Uslov(JeUcesnik(korisnikId), "zabranjeno", "Niste učesnik ove transakcije.");
        Provera.Uslov(StatusEscrowa != StatusEscrowa.CekaUplatu, "escrow_stanje",
            "Transakcija čeka uplatu kupca na escrow.");

        if (korisnikId == KupacId)
        {
            Provera.Uslov(KupacPotvrdioUtc is null, "vec_potvrdjeno", "Već ste potvrdili.");
            KupacPotvrdioUtc = sada;
        }
        else
        {
            Provera.Uslov(ProdavacPotvrdioUtc is null, "vec_potvrdjeno", "Već ste potvrdili.");
            ProdavacPotvrdioUtc = sada;
        }

        if (KupacPotvrdioUtc is null || ProdavacPotvrdioUtc is null)
        {
            return false;
        }

        Zavrsi(sada);
        return true;
    }

    public void Otkazi(int korisnikId, string? razlog, DateTime sada)
    {
        ProveriUToku();
        Provera.Uslov(JeUcesnik(korisnikId), "zabranjeno", "Niste učesnik ove transakcije.");
        Provera.Uslov(KupacPotvrdioUtc is null || ProdavacPotvrdioUtc is null, "transakcija_zavrsena",
            "Transakcija je već potvrđena od obe strane.");

        Status = StatusTransakcije.Otkazana;
        OtkazanoUtc = sada;
        OtkazaoKorisnikId = korisnikId;
        RazlogOtkazivanja = razlog?.Trim();
        if (StatusEscrowa == StatusEscrowa.Deponovano)
        {
            StatusEscrowa = StatusEscrowa.Vraceno;
        }
        else if (StatusEscrowa == StatusEscrowa.CekaUplatu)
        {
            StatusEscrowa = StatusEscrowa.NijeKoriscen;
        }

        Oglas.OtkaziRezervaciju();
    }

    /// <summary>Recenzija je dozvoljena samo učesnicima ZAVRŠENE transakcije.</summary>
    public void ProveriMozeOceniti(int korisnikId)
    {
        Provera.Uslov(JeUcesnik(korisnikId), "zabranjeno", "Niste učesnik ove transakcije.");
        Provera.Uslov(Status == StatusTransakcije.Zavrsena, "transakcija_nije_zavrsena",
            "Recenzija je moguća samo za završenu transakciju.");
    }

    private void Zavrsi(DateTime sada)
    {
        Status = StatusTransakcije.Zavrsena;
        ZavrsenoUtc = sada;
        if (StatusEscrowa == StatusEscrowa.Deponovano)
        {
            StatusEscrowa = StatusEscrowa.Isplaceno;
        }

        Oglas.OznaciKaoProdat();
    }

    private void ProveriUToku() =>
        Provera.Uslov(Status == StatusTransakcije.UToku, "transakcija_zatvorena",
            $"Transakcija je u statusu '{Status}'.");
}
