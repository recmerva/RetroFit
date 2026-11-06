using RetroFit.Domain.Common;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Blokada (ban) korisnika od strane moderatora. Čuva se istorija: ko je blokirao, zašto,
/// do kada i da li je blokada ukinuta. KrajUtc = null znači trajnu blokadu.
/// </summary>
public class BlokadaKorisnika : Entitet
{
    private BlokadaKorisnika() { }

    public BlokadaKorisnika(int korisnikId, int blokiraoKorisnikId, string razlog, DateTime sada, DateTime? krajUtc)
    {
        Provera.Uslov(korisnikId != blokiraoKorisnikId, "blokada_sebe", "Ne možete blokirati sami sebe.");
        Provera.Uslov(krajUtc is null || krajUtc > sada, "validacija", "Kraj blokade mora biti u budućnosti.");
        KorisnikId = korisnikId;
        BlokiraoKorisnikId = blokiraoKorisnikId;
        Razlog = Provera.Tekst(razlog, "Razlog", 500);
        PocetakUtc = sada;
        KrajUtc = krajUtc;
    }

    public int KorisnikId { get; private set; }
    public Korisnik Korisnik { get; private set; } = null!;
    public int BlokiraoKorisnikId { get; private set; }
    public Korisnik BlokiraoKorisnik { get; private set; } = null!;
    public string Razlog { get; private set; } = string.Empty;
    public DateTime PocetakUtc { get; private set; }
    public DateTime? KrajUtc { get; private set; }
    public DateTime? UkinutoUtc { get; private set; }
    public int? UkinuoKorisnikId { get; private set; }

    public bool JeAktivna(DateTime sada) => UkinutoUtc is null && (KrajUtc is null || KrajUtc > sada);

    public void Ukini(int moderatorId, DateTime sada)
    {
        Provera.Uslov(JeAktivna(sada), "blokada_neaktivna", "Blokada nije aktivna.");
        UkinutoUtc = sada;
        UkinuoKorisnikId = moderatorId;
    }
}
