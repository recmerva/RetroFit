using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;

namespace RetroFit.Domain.Entiteti;

/// <summary>Obaveštenje korisniku (nova poruka, ponuda, istek oglasa, poklapanje sačuvane pretrage...).</summary>
public class Obavestenje : Entitet
{
    private Obavestenje() { }

    public Obavestenje(int korisnikId, TipObavestenja tip, string naslov, string sadrzaj, string? link, DateTime sada)
    {
        KorisnikId = korisnikId;
        Tip = tip;
        Naslov = Provera.Tekst(naslov, "Naslov", 150);
        Sadrzaj = Provera.Tekst(sadrzaj, "Sadržaj", 1000);
        Link = Provera.OpcioniTekst(link, "Link", 300);
        KreiranoUtc = sada;
    }

    public int KorisnikId { get; private set; }
    public Korisnik Korisnik { get; private set; } = null!;
    public TipObavestenja Tip { get; private set; }
    public string Naslov { get; private set; } = string.Empty;
    public string Sadrzaj { get; private set; } = string.Empty;
    /// <summary>Relativna putanja u frontend aplikaciji, npr. "/oglasi/15".</summary>
    public string? Link { get; private set; }
    public DateTime KreiranoUtc { get; private set; }
    public DateTime? ProcitanoUtc { get; private set; }

    public void OznaciProcitanim(DateTime sada) => ProcitanoUtc ??= sada;
}
