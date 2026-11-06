using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Knjiženje plaćanja (ledger) za mock escrow i plaćeno isticanje oglasa.
/// Svaka promena stanja novca je novi red (uplata, isplata, povraćaj), a postojeći redovi se ne menjaju,
/// pa se istorija novca uvek može rekonstruisati.
/// UNIQUE ReferencaProcesora i filtrirani UNIQUE KljucIdempotentnosti (sprečava duplu naplatu
/// kada klijent zbog greške mreže pošalje isti zahtev dva puta).
/// </summary>
public class Placanje : Entitet
{
    private Placanje() { }

    public Placanje(int korisnikId, int? transakcijaId, TipPlacanja tip, StatusPlacanja status, decimal iznos,
        string referencaProcesora, string? kljucIdempotentnosti, string opis, DateTime sada)
    {
        Provera.Uslov(iznos > 0, "validacija", "Iznos plaćanja mora biti veći od 0.");
        KorisnikId = korisnikId;
        TransakcijaId = transakcijaId;
        Tip = tip;
        Status = status;
        Iznos = iznos;
        Valuta = "RSD";
        ReferencaProcesora = referencaProcesora;
        KljucIdempotentnosti = kljucIdempotentnosti;
        Opis = Provera.Tekst(opis, "Opis", 300);
        KreiranoUtc = sada;
    }

    public int KorisnikId { get; private set; }
    public Korisnik Korisnik { get; private set; } = null!;
    public int? TransakcijaId { get; private set; }
    public Transakcija? Transakcija { get; private set; }
    public TipPlacanja Tip { get; private set; }
    public StatusPlacanja Status { get; private set; }
    public decimal Iznos { get; private set; }
    public string Valuta { get; private set; } = "RSD";
    public string ReferencaProcesora { get; private set; } = string.Empty;
    public string? KljucIdempotentnosti { get; private set; }
    public string Opis { get; private set; } = string.Empty;
    public DateTime KreiranoUtc { get; private set; }
}
