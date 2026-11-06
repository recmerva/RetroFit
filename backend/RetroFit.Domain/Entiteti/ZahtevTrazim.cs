using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Zahtev "Tražim..." - korisnik objavljuje šta traži, a prodavci odgovaraju svojim oglasima.
/// </summary>
public class ZahtevTrazim : AuditabilniEntitet
{
    private readonly List<OdgovorNaZahtev> _odgovori = [];

    private ZahtevTrazim() { }

    public ZahtevTrazim(int korisnikId, string naslov, string opis, int kategorijaId, int? gradId, decimal? maksimalnaCena)
    {
        KorisnikId = korisnikId;
        Status = StatusZahteva.Otvoren;
        PostaviPodatke(naslov, opis, kategorijaId, gradId, maksimalnaCena);
    }

    public int KorisnikId { get; private set; }
    public Korisnik Korisnik { get; private set; } = null!;
    public string Naslov { get; private set; } = string.Empty;
    public string Opis { get; private set; } = string.Empty;
    public int KategorijaId { get; private set; }
    public Kategorija Kategorija { get; private set; } = null!;
    public int? GradId { get; private set; }
    public Grad? Grad { get; private set; }
    public decimal? MaksimalnaCena { get; private set; }
    public StatusZahteva Status { get; private set; }

    public IReadOnlyCollection<OdgovorNaZahtev> Odgovori => _odgovori;

    public void Izmeni(int korisnikId, string naslov, string opis, int kategorijaId, int? gradId, decimal? maksimalnaCena)
    {
        ProveriVlasnika(korisnikId);
        Provera.Uslov(Status == StatusZahteva.Otvoren, "zahtev_zatvoren", "Zatvoren zahtev ne može da se menja.");
        PostaviPodatke(naslov, opis, kategorijaId, gradId, maksimalnaCena);
    }

    public void Zatvori(int korisnikId, bool ispunjen)
    {
        ProveriVlasnika(korisnikId);
        Provera.Uslov(Status == StatusZahteva.Otvoren, "zahtev_zatvoren", "Zahtev je već zatvoren.");
        Status = ispunjen ? StatusZahteva.Ispunjen : StatusZahteva.Zatvoren;
    }

    public OdgovorNaZahtev DodajOdgovor(Oglas oglas, int korisnikId, string? poruka, DateTime sada)
    {
        Provera.Uslov(Status == StatusZahteva.Otvoren, "zahtev_zatvoren", "Zahtev više nije otvoren.");
        Provera.Uslov(korisnikId != KorisnikId, "sopstveni_zahtev", "Ne možete odgovoriti na sopstveni zahtev.");
        Provera.Uslov(oglas.JeVlasnik(korisnikId), "zabranjeno", "Možete ponuditi samo sopstveni oglas.");
        Provera.Uslov(oglas.Status == StatusOglasa.Aktivan && !oglas.Obrisan, "oglas_nije_aktivan",
            "Možete ponuditi samo aktivan oglas.");
        Provera.Uslov(_odgovori.All(o => o.OglasId != oglas.Id), "vec_ponudjeno",
            "Ovaj oglas je već ponuđen za ovaj zahtev.");

        var odgovor = new OdgovorNaZahtev(this, oglas.Id, korisnikId, poruka, sada);
        _odgovori.Add(odgovor);
        return odgovor;
    }

    private void PostaviPodatke(string naslov, string opis, int kategorijaId, int? gradId, decimal? maksimalnaCena)
    {
        Provera.Uslov(maksimalnaCena is null or > 0, "validacija", "Maksimalna cena mora biti veća od 0.");
        Naslov = Provera.Tekst(naslov, "Naslov", 120);
        Opis = Provera.Tekst(opis, "Opis", 2000);
        KategorijaId = kategorijaId;
        GradId = gradId;
        MaksimalnaCena = maksimalnaCena;
    }

    private void ProveriVlasnika(int korisnikId) =>
        Provera.Uslov(korisnikId == KorisnikId, "zabranjeno", "Ovo nije vaš zahtev.");
}

/// <summary>
/// Odgovor na zahtev "Tražim..." - prodavac nudi svoj oglas.
/// UNIQUE (ZahtevId, OglasId): isti oglas se ne nudi dva puta za isti zahtev.
/// </summary>
public class OdgovorNaZahtev : Entitet
{
    private OdgovorNaZahtev() { }

    internal OdgovorNaZahtev(ZahtevTrazim zahtev, int oglasId, int korisnikId, string? poruka, DateTime sada)
    {
        Zahtev = zahtev;
        ZahtevId = zahtev.Id;
        OglasId = oglasId;
        OdgovorioKorisnikId = korisnikId;
        Poruka = Provera.OpcioniTekst(poruka, "Poruka", 500);
        KreiranoUtc = sada;
    }

    public int ZahtevId { get; private set; }
    public ZahtevTrazim Zahtev { get; private set; } = null!;
    public int OglasId { get; private set; }
    public Oglas Oglas { get; private set; } = null!;
    public int OdgovorioKorisnikId { get; private set; }
    public Korisnik OdgovorioKorisnik { get; private set; } = null!;
    public string? Poruka { get; private set; }
    public DateTime KreiranoUtc { get; private set; }
}
