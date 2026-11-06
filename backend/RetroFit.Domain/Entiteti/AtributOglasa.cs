namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Vrednost atributa za konkretan oglas (EAV "value" deo).
/// Kompozitni primarni ključ (OglasId, DefinicijaAtributaId): jedan atribut po oglasu najviše jednom.
/// </summary>
public class AtributOglasa
{
    private AtributOglasa() { }

    internal AtributOglasa(Oglas oglas, int definicijaAtributaId, string vrednost)
    {
        Oglas = oglas;
        OglasId = oglas.Id;
        DefinicijaAtributaId = definicijaAtributaId;
        Vrednost = vrednost;
    }

    public int OglasId { get; private set; }
    public Oglas Oglas { get; private set; } = null!;
    public int DefinicijaAtributaId { get; private set; }
    public DefinicijaAtributa DefinicijaAtributa { get; private set; } = null!;
    public string Vrednost { get; internal set; } = string.Empty;
}
