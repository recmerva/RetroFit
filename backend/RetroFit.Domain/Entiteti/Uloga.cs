using RetroFit.Domain.Common;

namespace RetroFit.Domain.Entiteti;

/// <summary>Uloga u sistemu: Korisnik, Moderator ili Admin.</summary>
public class Uloga : Entitet
{
    private Uloga() { }

    public Uloga(int id, string naziv, string opis)
    {
        Id = id;
        Naziv = Provera.Tekst(naziv, "Naziv", 50);
        Opis = Provera.Tekst(opis, "Opis", 200);
    }

    public string Naziv { get; private set; } = string.Empty;
    public string Opis { get; private set; } = string.Empty;
}

/// <summary>
/// Vezna tabela M:N između korisnika i uloga. Primarni ključ je kompozitni (KorisnikId, UlogaId),
/// pa ista uloga ne može dva puta biti dodeljena istom korisniku.
/// </summary>
public class KorisnikUloga
{
    private KorisnikUloga() { }

    internal KorisnikUloga(Korisnik korisnik, Uloga uloga, DateTime sada)
    {
        Korisnik = korisnik;
        KorisnikId = korisnik.Id;
        Uloga = uloga;
        UlogaId = uloga.Id;
        DodeljenoUtc = sada;
    }

    public int KorisnikId { get; private set; }
    public Korisnik Korisnik { get; private set; } = null!;
    public int UlogaId { get; private set; }
    public Uloga Uloga { get; private set; } = null!;
    public DateTime DodeljenoUtc { get; private set; }
}
