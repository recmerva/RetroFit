using RetroFit.Domain.Common;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Kategorija oglasa. Hijerarhija je realizovana samoreferencirajućom vezom (RoditeljId),
/// pa jedna kategorija može imati više podkategorija (npr. Elektronika -> Telefoni).
/// </summary>
public class Kategorija : AuditabilniEntitet
{
    private readonly List<Kategorija> _podkategorije = [];
    private readonly List<DefinicijaAtributa> _definicijeAtributa = [];

    private Kategorija() { }

    public Kategorija(string naziv, string slug, int? roditeljId, string? ikonica, int redosled)
    {
        Naziv = Provera.Tekst(naziv, "Naziv", 100);
        Slug = NapraviSlug(slug);
        RoditeljId = roditeljId;
        Ikonica = Provera.OpcioniTekst(ikonica, "Ikonica", 50);
        Redosled = redosled;
        Aktivna = true;
    }

    public string Naziv { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Ikonica { get; private set; }
    public int Redosled { get; private set; }
    public bool Aktivna { get; private set; }
    public int? RoditeljId { get; private set; }
    public Kategorija? Roditelj { get; private set; }

    public IReadOnlyCollection<Kategorija> Podkategorije => _podkategorije;
    public IReadOnlyCollection<DefinicijaAtributa> DefinicijeAtributa => _definicijeAtributa;

    public void Izmeni(string naziv, string slug, int? roditeljId, string? ikonica, int redosled)
    {
        Provera.Uslov(roditeljId is null || roditeljId != Id || Id == 0, "kategorija_ciklus",
            "Kategorija ne može biti sama sebi nadređena.");
        Naziv = Provera.Tekst(naziv, "Naziv", 100);
        Slug = NapraviSlug(slug);
        RoditeljId = roditeljId;
        Ikonica = Provera.OpcioniTekst(ikonica, "Ikonica", 50);
        Redosled = redosled;
    }

    public void Aktiviraj() => Aktivna = true;
    public void Deaktiviraj() => Aktivna = false;

    public DefinicijaAtributa DodajAtribut(DefinicijaAtributa definicija)
    {
        Provera.Uslov(_definicijeAtributa.All(d => d.Kljuc != definicija.Kljuc), "atribut_postoji",
            $"Atribut sa ključem '{definicija.Kljuc}' već postoji u ovoj kategoriji.");
        _definicijeAtributa.Add(definicija);
        return definicija;
    }

    public static string NapraviSlug(string vrednost)
    {
        var tekst = Provera.Tekst(vrednost, "Slug", 120).ToLowerInvariant()
            .Replace('č', 'c').Replace('ć', 'c').Replace('š', 's').Replace('ž', 'z').Replace("đ", "dj");
        var znakovi = tekst.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = string.Join('-', new string(znakovi).Split('-', StringSplitOptions.RemoveEmptyEntries));
        Provera.Uslov(slug.Length > 0, "validacija", "Slug nije ispravan.");
        return slug;
    }
}
