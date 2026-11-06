using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Pokušaj verifikacije (SMS kod za telefon). Kod se čuva kao heš,
/// ima rok trajanja i ograničen broj pokušaja (zaštita od pogađanja koda).
/// </summary>
public class VerifikacijaKorisnika : Entitet
{
    private VerifikacijaKorisnika() { }

    public VerifikacijaKorisnika(int korisnikId, TipVerifikacije tip, string vrednost, string kodHash, DateTime sada)
    {
        KorisnikId = korisnikId;
        Tip = tip;
        Vrednost = vrednost;
        KodHash = kodHash;
        KreiranoUtc = sada;
        IsticeUtc = sada.AddMinutes(PoslovnaPravila.TrajanjeVerifikacionogKodaMinuta);
    }

    public int KorisnikId { get; private set; }
    public Korisnik Korisnik { get; private set; } = null!;
    public TipVerifikacije Tip { get; private set; }
    /// <summary>Vrednost koja se verifikuje (broj telefona).</summary>
    public string Vrednost { get; private set; } = string.Empty;
    public string KodHash { get; private set; } = string.Empty;
    public DateTime KreiranoUtc { get; private set; }
    public DateTime IsticeUtc { get; private set; }
    public int BrojPokusaja { get; private set; }
    public DateTime? IskorisceneUtc { get; private set; }

    public bool MozeSeKoristiti(DateTime sada) =>
        IskorisceneUtc is null && sada < IsticeUtc && BrojPokusaja < PoslovnaPravila.MaksPokusajaVerifikacije;

    /// <summary>Proverava kod. Broji se svaki pokušaj, i uspešan i neuspešan.</summary>
    public bool Proveri(string kodHash, DateTime sada)
    {
        Provera.Uslov(MozeSeKoristiti(sada), "verifikacija_istekla",
            "Kod je istekao ili je iskorišćen maksimalan broj pokušaja. Zatražite novi kod.");

        BrojPokusaja++;
        if (!string.Equals(KodHash, kodHash, StringComparison.Ordinal))
        {
            return false;
        }

        IskorisceneUtc = sada;
        return true;
    }

    public void Ponisti(DateTime sada) => IskorisceneUtc ??= sada;
}
