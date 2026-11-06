namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Omiljeni oglas (lista želja). Objedinjuje ListingLikes + Wishlists + WishlistItems iz specifikacije:
/// svaki korisnik ima tačno jednu listu omiljenih, pa bi posebna tabela Wishlists bila suvišna.
/// Kompozitni PK (KorisnikId, OglasId) sprečava da se isti oglas doda dva puta.
/// </summary>
public class OmiljeniOglas
{
    private OmiljeniOglas() { }

    public OmiljeniOglas(int korisnikId, int oglasId, DateTime sada)
    {
        KorisnikId = korisnikId;
        OglasId = oglasId;
        DodatoUtc = sada;
    }

    public int KorisnikId { get; private set; }
    public Korisnik Korisnik { get; private set; } = null!;
    public int OglasId { get; private set; }
    public Oglas Oglas { get; private set; } = null!;
    public DateTime DodatoUtc { get; private set; }
}
