using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;

namespace RetroFit.Domain.Entiteti;

/// <summary>Poruka u razgovoru. Sistemske poruke beleže događaje (npr. "Ponuda prihvaćena").</summary>
public class Poruka : Entitet
{
    private Poruka() { }

    internal Poruka(Razgovor razgovor, int posiljalacId, string sadrzaj, TipPoruke tip, DateTime sada, int? ponudaId)
    {
        Razgovor = razgovor;
        RazgovorId = razgovor.Id;
        PosiljalacId = posiljalacId;
        Sadrzaj = Provera.Tekst(sadrzaj, "Sadržaj", 2000);
        Tip = tip;
        PoslatoUtc = sada;
        PonudaId = ponudaId;
    }

    public int RazgovorId { get; private set; }
    public Razgovor Razgovor { get; private set; } = null!;
    public int PosiljalacId { get; private set; }
    public Korisnik Posiljalac { get; private set; } = null!;
    public string Sadrzaj { get; private set; } = string.Empty;
    public TipPoruke Tip { get; private set; }
    public DateTime PoslatoUtc { get; private set; }
    public DateTime? ProcitanoUtc { get; private set; }
    public int? PonudaId { get; private set; }
    public Ponuda? Ponuda { get; private set; }

    public void OznaciProcitanom(DateTime sada) => ProcitanoUtc ??= sada;
}
