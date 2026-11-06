using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Isticanje oglasa u određenom periodu. Dva tipa:
///  - Izdvojen: admin ručno bira oglase za početnu stranu (bez plaćanja);
///  - Podignut (boost): vlasnik plaća (mock) i oglas je na vrhu rezultata pretrage.
/// CHECK (KrajUtc > PocetakUtc).
/// </summary>
public class IstaknutiOglas : Entitet
{
    private IstaknutiOglas() { }

    private IstaknutiOglas(int oglasId, TipIsticanja tip, DateTime pocetak, DateTime kraj, int kreiraoKorisnikId, int? placanjeId)
    {
        Provera.Uslov(kraj > pocetak, "validacija", "Kraj isticanja mora biti posle početka.");
        OglasId = oglasId;
        Tip = tip;
        PocetakUtc = pocetak;
        KrajUtc = kraj;
        KreiraoKorisnikId = kreiraoKorisnikId;
        PlacanjeId = placanjeId;
        KreiranoUtc = pocetak;
    }

    public int OglasId { get; private set; }
    public Oglas Oglas { get; private set; } = null!;
    public TipIsticanja Tip { get; private set; }
    public DateTime PocetakUtc { get; private set; }
    public DateTime KrajUtc { get; private set; }
    public int KreiraoKorisnikId { get; private set; }
    public Korisnik KreiraoKorisnik { get; private set; } = null!;
    public int? PlacanjeId { get; private set; }
    public Placanje? Placanje { get; private set; }
    public DateTime KreiranoUtc { get; private set; }
    public DateTime? OtkazanoUtc { get; private set; }

    public bool JeAktivno(DateTime sada) => OtkazanoUtc is null && PocetakUtc <= sada && sada < KrajUtc;

    public static IstaknutiOglas Izdvoji(Oglas oglas, int adminId, DateTime sada, DateTime kraj)
    {
        Provera.Uslov(oglas.Status == StatusOglasa.Aktivan && !oglas.Obrisan, "oglas_nije_aktivan",
            "Može se izdvojiti samo aktivan oglas.");
        return new IstaknutiOglas(oglas.Id, TipIsticanja.Izdvojen, sada, kraj, adminId, null);
    }

    public static IstaknutiOglas Podigni(Oglas oglas, int korisnikId, int brojDana, Placanje placanje, DateTime sada)
    {
        Provera.Uslov(oglas.JeVlasnik(korisnikId), "zabranjeno", "Možete podići samo svoj oglas.");
        Provera.Uslov(oglas.Status == StatusOglasa.Aktivan && !oglas.Obrisan, "oglas_nije_aktivan",
            "Može se podići samo aktivan oglas.");
        Provera.Uslov(brojDana is >= 1 and <= PoslovnaPravila.MaksDanaPodizanja, "validacija",
            $"Podizanje može trajati od 1 do {PoslovnaPravila.MaksDanaPodizanja} dana.");
        Provera.Uslov(placanje.Status == StatusPlacanja.Uspesno, "placanje_neuspesno", "Plaćanje nije uspelo.");

        // Veza preko navigacije: EF postavlja PlacanjeId i kada plaćanje još nije upisano (Id = 0).
        var isticanje = new IstaknutiOglas(oglas.Id, TipIsticanja.Podignut, sada, sada.AddDays(brojDana), korisnikId,
            placanje.Id > 0 ? placanje.Id : null)
        {
            Placanje = placanje
        };
        return isticanje;
    }

    public static decimal CenaPodizanja(int brojDana) => brojDana * PoslovnaPravila.CenaPodizanjaPoDanu;

    public void Otkazi(DateTime sada)
    {
        Provera.Uslov(OtkazanoUtc is null, "vec_otkazano", "Isticanje je već otkazano.");
        OtkazanoUtc = sada;
    }
}
