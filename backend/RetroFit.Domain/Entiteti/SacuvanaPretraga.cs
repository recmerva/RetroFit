using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Sačuvana pretraga. Kriterijumi se čuvaju kao zasebne kolone (a ne kao JSON),
/// da bi pozadinski posao mogao SQL upitom da pronađe nove oglase koji odgovaraju pretrazi.
/// </summary>
public class SacuvanaPretraga : Entitet
{
    private SacuvanaPretraga() { }

    public SacuvanaPretraga(int korisnikId, string naziv, string? upit, int? kategorijaId, int? gradId,
        decimal? cenaOd, decimal? cenaDo, StanjeProizvoda? stanje, TipOglasa? tip, bool obavestavaj, DateTime sada)
    {
        KorisnikId = korisnikId;
        KreiranoUtc = sada;
        PoslednjaProveraUtc = sada;
        Izmeni(naziv, upit, kategorijaId, gradId, cenaOd, cenaDo, stanje, tip, obavestavaj);
    }

    public int KorisnikId { get; private set; }
    public Korisnik Korisnik { get; private set; } = null!;
    public string Naziv { get; private set; } = string.Empty;
    public string? Upit { get; private set; }
    public int? KategorijaId { get; private set; }
    public Kategorija? Kategorija { get; private set; }
    public int? GradId { get; private set; }
    public Grad? Grad { get; private set; }
    public decimal? CenaOd { get; private set; }
    public decimal? CenaDo { get; private set; }
    public StanjeProizvoda? Stanje { get; private set; }
    public TipOglasa? Tip { get; private set; }
    public bool Obavestavaj { get; private set; }
    public DateTime KreiranoUtc { get; private set; }
    /// <summary>Do kog trenutka su oglasi već provereni (da se korisnik ne obaveštava dva puta za isti oglas).</summary>
    public DateTime PoslednjaProveraUtc { get; private set; }

    public void Izmeni(string naziv, string? upit, int? kategorijaId, int? gradId, decimal? cenaOd,
        decimal? cenaDo, StanjeProizvoda? stanje, TipOglasa? tip, bool obavestavaj)
    {
        Provera.Uslov(cenaOd is null || cenaOd >= 0, "validacija", "Cena od ne može biti negativna.");
        Provera.Uslov(cenaOd is null || cenaDo is null || cenaOd <= cenaDo, "validacija",
            "Cena od mora biti manja ili jednaka ceni do.");
        Naziv = Provera.Tekst(naziv, "Naziv", 100);
        Upit = Provera.OpcioniTekst(upit, "Upit", 200);
        KategorijaId = kategorijaId;
        GradId = gradId;
        CenaOd = cenaOd;
        CenaDo = cenaDo;
        Stanje = stanje;
        Tip = tip;
        Obavestavaj = obavestavaj;
    }

    public void OznaciProvereno(DateTime doTrenutka) => PoslednjaProveraUtc = doTrenutka;
}
