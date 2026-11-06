using RetroFit.Domain.Common;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Grad (šifarnik). Koordinate se u domenu čuvaju kao brojevi, a Infrastructure sloj
/// dodatno mapira SQL Server kolonu tipa geography (shadow property "Lokacija")
/// sa prostornim (spatial) indeksom, koja se koristi za pretragu po udaljenosti.
/// </summary>
public class Grad : Entitet
{
    private Grad() { }

    public Grad(int id, string naziv, string postanskiBroj, double geografskaSirina, double geografskaDuzina)
    {
        Provera.Uslov(geografskaSirina is >= -90 and <= 90, "validacija", "Geografska širina nije ispravna.");
        Provera.Uslov(geografskaDuzina is >= -180 and <= 180, "validacija", "Geografska dužina nije ispravna.");
        Id = id;
        Naziv = Provera.Tekst(naziv, "Naziv", 100);
        PostanskiBroj = Provera.Tekst(postanskiBroj, "Poštanski broj", 10);
        GeografskaSirina = geografskaSirina;
        GeografskaDuzina = geografskaDuzina;
    }

    public string Naziv { get; private set; } = string.Empty;
    public string PostanskiBroj { get; private set; } = string.Empty;
    public double GeografskaSirina { get; private set; }
    public double GeografskaDuzina { get; private set; }
}
