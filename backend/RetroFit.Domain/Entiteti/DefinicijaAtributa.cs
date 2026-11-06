using System.Globalization;
using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Definicija atributa specifičnog za kategoriju (npr. za "Automobili": godište, kilometraža, gorivo).
/// Zajedno sa tabelom AtributiOglasa ovo je EAV model (Entity-Attribute-Value): različite kategorije
/// imaju različite atribute, a ne dodajemo novu kolonu u tabelu Oglasi za svaki mogući atribut.
/// </summary>
public class DefinicijaAtributa : Entitet
{
    private DefinicijaAtributa() { }

    public DefinicijaAtributa(int kategorijaId, string naziv, string kljuc, TipAtributa tip, bool obavezan,
        IEnumerable<string>? opcije, string? jedinicaMere, int redosled)
    {
        KategorijaId = kategorijaId;
        Naziv = Provera.Tekst(naziv, "Naziv", 100);
        Kljuc = Kategorija.NapraviSlug(kljuc);
        Tip = tip;
        Obavezan = obavezan;
        JedinicaMere = Provera.OpcioniTekst(jedinicaMere, "Jedinica mere", 20);
        Redosled = redosled;

        var listaOpcija = opcije?.Select(o => o.Trim()).Where(o => o.Length > 0).Distinct().ToList() ?? [];
        Provera.Uslov(tip != TipAtributa.Izbor || listaOpcija.Count >= 2, "validacija",
            "Atribut tipa 'Izbor' mora imati bar dve opcije.");
        Opcije = tip == TipAtributa.Izbor ? string.Join('|', listaOpcija) : null;
    }

    public int KategorijaId { get; private set; }
    public Kategorija Kategorija { get; private set; } = null!;
    public string Naziv { get; private set; } = string.Empty;
    public string Kljuc { get; private set; } = string.Empty;
    public TipAtributa Tip { get; private set; }
    public bool Obavezan { get; private set; }
    /// <summary>Za tip Izbor: dozvoljene vrednosti razdvojene znakom '|'.</summary>
    public string? Opcije { get; private set; }
    public string? JedinicaMere { get; private set; }
    public int Redosled { get; private set; }

    public IReadOnlyList<string> ListaOpcija => Opcije?.Split('|') ?? [];

    /// <summary>Proverava da li vrednost odgovara tipu atributa.</summary>
    public bool JeValidnaVrednost(string? vrednost)
    {
        if (string.IsNullOrWhiteSpace(vrednost))
        {
            return !Obavezan;
        }

        var v = vrednost.Trim();
        return Tip switch
        {
            TipAtributa.Tekst => v.Length <= 200,
            TipAtributa.Broj => decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            TipAtributa.DaNe => v is "da" or "ne",
            TipAtributa.Izbor => ListaOpcija.Contains(v),
            _ => false
        };
    }
}
