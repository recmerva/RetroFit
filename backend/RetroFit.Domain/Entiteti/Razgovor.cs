using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Razgovor (thread) između kupca i prodavca, uvek vezan za konkretan oglas.
/// UNIQUE (OglasId, KupacId): jedan kupac ima najviše jedan razgovor po oglasu.
/// CHECK (KupacId &lt;&gt; ProdavacId): prodavac ne može da piše sam sebi.
/// </summary>
public class Razgovor : Entitet
{
    private readonly List<Poruka> _poruke = [];
    private readonly List<Ponuda> _ponude = [];

    private Razgovor() { }

    public Razgovor(Oglas oglas, int kupacId, DateTime sada)
    {
        Provera.Uslov(!oglas.JeVlasnik(kupacId), "sopstveni_oglas", "Ne možete započeti razgovor o svom oglasu.");
        Provera.Uslov(!oglas.Obrisan && oglas.Status is StatusOglasa.Aktivan or StatusOglasa.Rezervisan,
            "oglas_nije_aktivan", "Razgovor se može započeti samo za aktivan oglas.");
        OglasId = oglas.Id;
        Oglas = oglas;
        KupacId = kupacId;
        ProdavacId = oglas.ProdavacId;
        KreiranoUtc = sada;
        PoslednjaPorukaUtc = sada;
    }

    public int OglasId { get; private set; }
    public Oglas Oglas { get; private set; } = null!;
    public int KupacId { get; private set; }
    public Korisnik Kupac { get; private set; } = null!;
    public int ProdavacId { get; private set; }
    public Korisnik Prodavac { get; private set; } = null!;
    public DateTime KreiranoUtc { get; private set; }
    public DateTime PoslednjaPorukaUtc { get; private set; }

    public IReadOnlyCollection<Poruka> Poruke => _poruke;
    public IReadOnlyCollection<Ponuda> Ponude => _ponude;

    public bool JeUcesnik(int korisnikId) => korisnikId == KupacId || korisnikId == ProdavacId;

    public int DrugiUcesnik(int korisnikId)
    {
        ProveriUcesnika(korisnikId);
        return korisnikId == KupacId ? ProdavacId : KupacId;
    }

    public Poruka PosaljiPoruku(int posiljalacId, string sadrzaj, TipPoruke tip, DateTime sada, int? ponudaId = null)
    {
        if (tip != TipPoruke.Sistemska)
        {
            ProveriUcesnika(posiljalacId);
        }

        var poruka = new Poruka(this, posiljalacId, sadrzaj, tip, sada, ponudaId);
        _poruke.Add(poruka);
        PoslednjaPorukaUtc = sada;
        return poruka;
    }

    public void ProveriUcesnika(int korisnikId) =>
        Provera.Uslov(JeUcesnik(korisnikId), "zabranjeno", "Niste učesnik ovog razgovora.");
}
