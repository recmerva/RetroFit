using RetroFit.Domain.Common;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Slika oglasa. Jedan oglas ima više slika (1:N). Redosled 0 je glavna slika.
/// U bazi: UNIQUE (OglasId, Redosled) i CHECK Redosled BETWEEN 0 AND 9 (najviše 10 slika).
/// </summary>
public class SlikaOglasa : Entitet
{
    private SlikaOglasa() { }

    internal SlikaOglasa(Oglas oglas, string putanja, string tipSadrzaja, long velicinaBajtova, int redosled, DateTime sada)
    {
        Oglas = oglas;
        OglasId = oglas.Id;
        Putanja = putanja;
        TipSadrzaja = tipSadrzaja;
        VelicinaBajtova = velicinaBajtova;
        Redosled = redosled;
        KreiranoUtc = sada;
    }

    public int OglasId { get; private set; }
    public Oglas Oglas { get; private set; } = null!;
    /// <summary>Relativna javna putanja, npr. "/uploads/oglasi/15/abc.jpg". Ime fajla je nasumično (GUID).</summary>
    public string Putanja { get; private set; } = string.Empty;
    public string TipSadrzaja { get; private set; } = string.Empty;
    public long VelicinaBajtova { get; private set; }
    public int Redosled { get; internal set; }
    public DateTime KreiranoUtc { get; private set; }
}
