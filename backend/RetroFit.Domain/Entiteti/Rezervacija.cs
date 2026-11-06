using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Rezervacija = privremeno zadržavanje oglasa za JEDNOG konkretnog kupca (KupacId NOT NULL).
/// Nastaje kada prodavac prihvati ponudu ili direktno rezerviše oglas za kupca iz razgovora.
///
/// Statusi: Aktivna -> Realizovana (nastala je transakcija) | Otkazana | Istekla (posle 72h).
///
/// U bazi: filtrirani UNIQUE indeks (OglasId) WHERE Status = 'Aktivna' garantuje da oglas
/// ima najviše jednu aktivnu rezervaciju, dok stare (otkazane/istekle) ostaju kao istorija.
/// </summary>
public class Rezervacija : Entitet
{
    private Rezervacija() { }

    public int OglasId { get; private set; }
    public Oglas Oglas { get; private set; } = null!;
    public int KupacId { get; private set; }
    public Korisnik Kupac { get; private set; } = null!;
    public int ProdavacId { get; private set; }
    public Korisnik Prodavac { get; private set; } = null!;
    public int? PonudaId { get; private set; }
    public Ponuda? Ponuda { get; private set; }
    /// <summary>Dogovorena cena (iz prihvaćene ponude ili cena iz oglasa). NULL za razmenu/poklon.</summary>
    public decimal? DogovorenaCena { get; private set; }
    public StatusRezervacije Status { get; private set; }
    public DateTime KreiranoUtc { get; private set; }
    public DateTime IsticeUtc { get; private set; }
    public DateTime? ZavrsenoUtc { get; private set; }
    public int? OtkazaoKorisnikId { get; private set; }
    public string? RazlogOtkazivanja { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public bool JeAktivna => Status == StatusRezervacije.Aktivna;
    public bool JeUcesnik(int korisnikId) => korisnikId == KupacId || korisnikId == ProdavacId;

    /// <summary>Kreira rezervaciju i istovremeno prebacuje oglas u status Rezervisan.</summary>
    public static Rezervacija Kreiraj(Oglas oglas, int kupacId, decimal? dogovorenaCena, Ponuda? ponuda, DateTime sada)
    {
        Provera.Uslov(!oglas.JeVlasnik(kupacId), "sopstveni_oglas", "Ne možete rezervisati sopstveni oglas.");
        Provera.Uslov(ponuda is null || ponuda.Status == StatusPonude.Prihvacena, "validacija",
            "Rezervacija iz ponude zahteva prihvaćenu ponudu.");
        Provera.Uslov(oglas.Tip != TipOglasa.Prodaja || dogovorenaCena is > 0, "validacija",
            "Za oglas tipa Prodaja dogovorena cena je obavezna.");

        oglas.Rezervisi();

        return new Rezervacija
        {
            Oglas = oglas,
            OglasId = oglas.Id,
            KupacId = kupacId,
            ProdavacId = oglas.ProdavacId,
            Ponuda = ponuda,
            PonudaId = ponuda is { Id: > 0 } ? ponuda.Id : null,
            DogovorenaCena = oglas.Tip == TipOglasa.Prodaja ? dogovorenaCena : null,
            Status = StatusRezervacije.Aktivna,
            KreiranoUtc = sada,
            IsticeUtc = sada.AddHours(PoslovnaPravila.TrajanjeRezervacijeSati)
        };
    }

    public void Otkazi(int korisnikId, string? razlog, Oglas oglas, DateTime sada)
    {
        Provera.Uslov(JeUcesnik(korisnikId), "zabranjeno", "Niste učesnik ove rezervacije.");
        Provera.Uslov(JeAktivna, "rezervacija_zatvorena", "Rezervacija nije aktivna.");
        Status = StatusRezervacije.Otkazana;
        OtkazaoKorisnikId = korisnikId;
        RazlogOtkazivanja = Provera.OpcioniTekst(razlog, "Razlog", 500);
        ZavrsenoUtc = sada;
        oglas.OtkaziRezervaciju();
    }

    /// <summary>Sistemsko otkazivanje (npr. moderator uklanja oglas).</summary>
    public void OtkaziSistemski(string razlog, DateTime sada)
    {
        if (!JeAktivna)
        {
            return;
        }

        Status = StatusRezervacije.Otkazana;
        RazlogOtkazivanja = razlog;
        ZavrsenoUtc = sada;
    }

    public void Istekni(Oglas oglas, DateTime sada)
    {
        Provera.Uslov(JeAktivna && sada >= IsticeUtc, "rezervacija_nije_istekla", "Rezervacija još nije istekla.");
        Status = StatusRezervacije.Istekla;
        ZavrsenoUtc = sada;
        oglas.OtkaziRezervaciju();
    }

    internal void Realizuj(DateTime sada)
    {
        Provera.Uslov(JeAktivna, "rezervacija_zatvorena", "Rezervacija nije aktivna.");
        Provera.Uslov(sada < IsticeUtc, "rezervacija_istekla", "Rezervacija je istekla.");
        Status = StatusRezervacije.Realizovana;
        ZavrsenoUtc = sada;
    }
}
