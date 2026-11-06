using RetroFit.Domain.Common;
using RetroFit.Domain.Pravila;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Recenzija nakon završene transakcije. Kupac ocenjuje prodavca i obrnuto,
/// pa jedna transakcija ima najviše dve recenzije.
/// U bazi: UNIQUE (TransakcijaId, OcenjivacId), CHECK Ocena BETWEEN 1 AND 5,
/// CHECK OcenjivacId &lt;&gt; OcenjeniKorisnikId.
/// </summary>
public class Recenzija : Entitet
{
    private Recenzija() { }

    public int TransakcijaId { get; private set; }
    public Transakcija Transakcija { get; private set; } = null!;
    public int OcenjivacId { get; private set; }
    public Korisnik Ocenjivac { get; private set; } = null!;
    public int OcenjeniKorisnikId { get; private set; }
    public Korisnik OcenjeniKorisnik { get; private set; } = null!;
    public int Ocena { get; private set; }
    public string? Komentar { get; private set; }
    public DateTime KreiranoUtc { get; private set; }

    public static Recenzija Kreiraj(Transakcija transakcija, int ocenjivacId, int ocena, string? komentar, DateTime sada)
    {
        transakcija.ProveriMozeOceniti(ocenjivacId);
        Provera.Uslov(ocena is >= 1 and <= 5, "validacija", "Ocena mora biti između 1 i 5.");
        Provera.Uslov(transakcija.ZavrsenoUtc is not null
                      && sada <= transakcija.ZavrsenoUtc.Value.AddDays(PoslovnaPravila.RokZaRecenzijuDana),
            "rok_istekao", $"Recenzija se može ostaviti najkasnije {PoslovnaPravila.RokZaRecenzijuDana} dana od završetka transakcije.");

        return new Recenzija
        {
            Transakcija = transakcija,
            TransakcijaId = transakcija.Id,
            OcenjivacId = ocenjivacId,
            OcenjeniKorisnikId = transakcija.DrugaStrana(ocenjivacId),
            Ocena = ocena,
            Komentar = Provera.OpcioniTekst(komentar, "Komentar", 1000),
            KreiranoUtc = sada
        };
    }
}
