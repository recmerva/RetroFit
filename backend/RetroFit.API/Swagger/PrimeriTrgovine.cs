using RetroFit.API.Kontroleri;
using RetroFit.Application.Features.Oglasi;
using RetroFit.Application.Features.Razgovori;
using RetroFit.Application.Features.Trgovina;
using RetroFit.Domain.Enumi;
using Swashbuckle.AspNetCore.Filters;

namespace RetroFit.API.Swagger;

internal sealed class KreirajOglasPrimer : IExamplesProvider<KreirajOglasCommand>
{
    public KreirajOglasCommand GetExamples() => new(
        KategorijaId: 21, GradId: 3, Naslov: "Opel Astra H 1.7 CDTI", Opis: "Redovno servisirana, nove gume, registrovana do juna.",
        Tip: TipOglasa.Prodaja, Cena: 350000, Stanje: StanjeProizvoda.Polovno, DostavaMoguca: false, LicnoPreuzimanjeMoguce: true,
        ZeljenaZamena: null, Atributi: new Dictionary<int, string?> { [8] = "Opel", [9] = "2008", [10] = "245000", [11] = "Dizel" });
}

internal sealed class PonudaZahtevPrimer : IExamplesProvider<IznosZahtev>
{
    public IznosZahtev GetExamples() => new(9500, "Može li 9.500 uz lično preuzimanje danas?");
}

internal sealed class KupovinaZahtevPrimer : IExamplesProvider<KupovinaZahtev>
{
    public KupovinaZahtev GetExamples() => new(NacinPreuzimanja.Dostava, "Bulevar oslobođenja 10, Novi Sad", KoristiEscrow: true);
}

internal sealed class RecenzijaZahtevPrimer : IExamplesProvider<RecenzijaZahtev>
{
    public RecenzijaZahtev GetExamples() => new(5, "Sve kao u opisu, brzo poslato. Preporuka!");
}

internal sealed class PorukaZahtevPrimer : IExamplesProvider<PorukaZahtev>
{
    public PorukaZahtev GetExamples() => new("Zdravo, da li je oglas još dostupan?", null);
}

internal sealed class PonudaOdgovorPrimer : IExamplesProvider<PonudaDto>
{
    public PonudaDto GetExamples() => new()
    {
        Id = 12, OglasId = 3, OglasNaslov = "Retro gramofon Tesla NC 440", CenaOglasa = 15000, RazgovorId = 7, KupacId = 5, KupacIme = "Jovan N.",
        ProdavacId = 4, ProdavacIme = "Ana J.", PonudioKorisnikId = 5, Iznos = 12000, Napomena = "Može li 12?", Status = StatusPonude.NaCekanju,
        KreiranoUtc = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc), IsticeUtc = new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc)
    };
}

internal sealed class RezervacijaOdgovorPrimer : IExamplesProvider<RezervacijaDto>
{
    public RezervacijaDto GetExamples() => new()
    {
        Id = 2, Oglas = new OglasUkratkoDto { Id = 3, Naslov = "Retro gramofon Tesla NC 440", Cena = 15000, Tip = TipOglasa.Prodaja, Status = StatusOglasa.Rezervisan, ProdavacId = 4 },
        KupacId = 5, KupacIme = "Jovan N.", ProdavacId = 4, ProdavacIme = "Ana J.", PonudaId = 13, DogovorenaCena = 14000, Status = StatusRezervacije.Aktivna,
        KreiranoUtc = new DateTime(2026, 11, 3, 10, 5, 0, DateTimeKind.Utc), IsticeUtc = new DateTime(2026, 11, 6, 10, 5, 0, DateTimeKind.Utc)
    };
}
