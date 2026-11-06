using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Application.Common.Validacija;
using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;

namespace RetroFit.Application.Features.Oglasi;

/// <summary>Zajednička polja za kreiranje i izmenu oglasa (da se validacija ne ponavlja).</summary>
public interface IOglasPodaci
{
    int KategorijaId { get; }
    int GradId { get; }
    string Naslov { get; }
    string Opis { get; }
    TipOglasa Tip { get; }
    decimal? Cena { get; }
    StanjeProizvoda Stanje { get; }
    bool DostavaMoguca { get; }
    bool LicnoPreuzimanjeMoguce { get; }
    string? ZeljenaZamena { get; }
    IReadOnlyDictionary<int, string?>? Atributi { get; }
}

internal static class OglasValidacija
{
    public static void Pravila<T>(AbstractValidator<T> v) where T : IOglasPodaci
    {
        v.RuleFor(x => x.KategorijaId).GreaterThan(0).WithMessage("Izaberite kategoriju.");
        v.RuleFor(x => x.GradId).GreaterThan(0).WithMessage("Izaberite grad.");
        v.RuleFor(x => x.Naslov).NotEmpty().WithMessage("Naslov je obavezan.")
            .Length(5, 120).WithMessage("Naslov mora imati od 5 do 120 karaktera.");
        v.RuleFor(x => x.Opis).NotEmpty().WithMessage("Opis je obavezan.")
            .Length(10, 4000).WithMessage("Opis mora imati od 10 do 4000 karaktera.");
        v.RuleFor(x => x.Tip).IsInEnum();
        v.RuleFor(x => x.Stanje).IsInEnum();
        v.RuleFor(x => x.Cena).NotNull().WithMessage("Za prodaju je obavezna cena.").Cena().When(x => x.Tip == TipOglasa.Prodaja);
        v.RuleFor(x => x.Cena).Must(c => c is null or 0).When(x => x.Tip != TipOglasa.Prodaja)
            .WithMessage("Oglas za razmenu ili poklon ne sme imati cenu.");
        v.RuleFor(x => x).Must(x => x.DostavaMoguca || x.LicnoPreuzimanjeMoguce)
            .WithName("NacinPreuzimanja").WithMessage("Omogućite bar dostavu ili lično preuzimanje.");
        v.RuleFor(x => x.ZeljenaZamena).MaximumLength(300);
        v.RuleFor(x => x.Atributi).Must(a => a is null || a.Count <= 30).WithMessage("Previše atributa.");
        v.RuleForEach(x => x.Atributi).Must(a => a.Value is null || a.Value.Length <= 200)
            .WithMessage("Vrednost atributa može imati najviše 200 karaktera.");
    }
}

/// <summary>Provere koje zahtevaju bazu, a zajedničke su za kreiranje i izmenu.</summary>
internal static class OglasProvere
{
    public static async Task<IReadOnlyList<DefinicijaAtributa>> ProveriKategorijuIGradAsync(IUnitOfWork uow, int kategorijaId, int gradId,
        CancellationToken ct)
    {
        var kategorija = await uow.Kategorije.PronadjiPoIdAsync(kategorijaId, ct) ?? throw new NijePronadjenoIzuzetak("Kategorija", kategorijaId);
        if (!kategorija.Aktivna)
        {
            throw new DomenskiIzuzetak("validacija", "Izabrana kategorija nije aktivna.");
        }

        if (await uow.Kategorije.ImaPodkategorijeAsync(kategorijaId, ct))
        {
            throw new DomenskiIzuzetak("validacija", "Izaberite podkategoriju (npr. 'Mobilni telefoni' umesto 'Elektronika').");
        }

        if (!await uow.Gradovi.PostojiAsync(g => g.Id == gradId, ct))
        {
            throw new NijePronadjenoIzuzetak("Grad", gradId);
        }

        return await uow.Kategorije.DefinicijeZaKategorijuAsync(kategorijaId, ct);
    }

    public static async Task<Oglas> UcitajSvojAsync(IUnitOfWork uow, int oglasId, int korisnikId, CancellationToken ct)
    {
        var oglas = await uow.Oglasi.PronadjiZaIzmenuAsync(oglasId, ct) ?? throw new NijePronadjenoIzuzetak("Oglas", oglasId);
        if (!oglas.JeVlasnik(korisnikId))
        {
            throw new ZabranjenoIzuzetak("Možete menjati samo sopstvene oglase.");
        }

        return oglas;
    }

    public static async Task<OglasDetaljiDto> DetaljiAsync(IUnitOfWork uow, int oglasId, DateTime sada, CancellationToken ct) =>
        (await uow.Oglasi.DetaljiAsync(oglasId, sada, ct))! with { JeMoj = true };
}

// =====================================================================
// KREIRANJE I IZMENA
// =====================================================================

public sealed record KreirajOglasCommand(
    int KategorijaId, int GradId, string Naslov, string Opis, TipOglasa Tip, decimal? Cena, StanjeProizvoda Stanje,
    bool DostavaMoguca, bool LicnoPreuzimanjeMoguce, string? ZeljenaZamena, IReadOnlyDictionary<int, string?>? Atributi,
    bool ObjaviOdmah = true) : IRequest<OglasDetaljiDto>, IOglasPodaci;

public sealed class KreirajOglasValidator : AbstractValidator<KreirajOglasCommand>
{
    public KreirajOglasValidator() => OglasValidacija.Pravila(this);
}

internal sealed class KreirajOglasHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<KreirajOglasCommand, OglasDetaljiDto>
{
    public async Task<OglasDetaljiDto> Handle(KreirajOglasCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var definicije = await OglasProvere.ProveriKategorijuIGradAsync(uow, z.KategorijaId, z.GradId, ct);

        var oglas = Oglas.Kreiraj(trenutni.ObavezanId, z.KategorijaId, z.GradId, z.Naslov, z.Opis, z.Tip, z.Cena, z.Stanje,
            z.DostavaMoguca, z.LicnoPreuzimanjeMoguce, z.ZeljenaZamena, z.ObjaviOdmah, sada);
        oglas.PostaviAtribute(z.Atributi ?? new Dictionary<int, string?>(), definicije);

        await uow.Oglasi.DodajAsync(oglas, ct);
        await uow.SacuvajAsync(ct);
        return await OglasProvere.DetaljiAsync(uow, oglas.Id, sada, ct);
    }
}

public sealed record IzmeniOglasCommand(
    int Id, int KategorijaId, int GradId, string Naslov, string Opis, TipOglasa Tip, decimal? Cena, StanjeProizvoda Stanje,
    bool DostavaMoguca, bool LicnoPreuzimanjeMoguce, string? ZeljenaZamena, IReadOnlyDictionary<int, string?>? Atributi)
    : IRequest<OglasDetaljiDto>, IOglasPodaci;

public sealed class IzmeniOglasValidator : AbstractValidator<IzmeniOglasCommand>
{
    public IzmeniOglasValidator() => OglasValidacija.Pravila(this);
}

internal sealed class IzmeniOglasHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<IzmeniOglasCommand, OglasDetaljiDto>
{
    public async Task<OglasDetaljiDto> Handle(IzmeniOglasCommand z, CancellationToken ct)
    {
        var oglas = await OglasProvere.UcitajSvojAsync(uow, z.Id, trenutni.ObavezanId, ct);
        var definicije = await OglasProvere.ProveriKategorijuIGradAsync(uow, z.KategorijaId, z.GradId, ct);

        oglas.Izmeni(z.KategorijaId, z.GradId, z.Naslov, z.Opis, z.Tip, z.Cena, z.Stanje, z.DostavaMoguca,
            z.LicnoPreuzimanjeMoguce, z.ZeljenaZamena);
        oglas.PostaviAtribute(z.Atributi ?? new Dictionary<int, string?>(), definicije);

        await uow.SacuvajAsync(ct);
        return await OglasProvere.DetaljiAsync(uow, oglas.Id, vreme.SadaUtc, ct);
    }
}

// =====================================================================
// PROMENE STATUSA (objava, obnova, prodaja van platforme, brisanje)
// =====================================================================

public enum AkcijaOglasa
{
    Objavi,
    Obnovi,
    OznaciProdatim
}

public sealed record PromeniStatusOglasaCommand(int Id, AkcijaOglasa Akcija) : IRequest<OglasDetaljiDto>;

internal sealed class PromeniStatusOglasaHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme, IObavestenjaServis obavestenja)
    : IRequestHandler<PromeniStatusOglasaCommand, OglasDetaljiDto>
{
    public async Task<OglasDetaljiDto> Handle(PromeniStatusOglasaCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var oglas = await OglasProvere.UcitajSvojAsync(uow, z.Id, trenutni.ObavezanId, ct);

        switch (z.Akcija)
        {
            case AkcijaOglasa.Objavi:
                oglas.Objavi(sada);
                break;
            case AkcijaOglasa.Obnovi:
                oglas.Obnovi(sada);
                break;
            case AkcijaOglasa.OznaciProdatim:
                // Prodaja VAN platforme. Rezervisan oglas se završava kroz transakciju, ne ovde.
                if (oglas.Status != StatusOglasa.Aktivan)
                {
                    throw new DomenskiIzuzetak("nedozvoljen_prelaz",
                        "Samo aktivan oglas može da se označi kao prodat. Rezervisan oglas se završava kroz kupovinu.");
                }

                oglas.OznaciKaoProdat();
                foreach (var ponuda in await uow.Ponude.NaCekanjuZaOglasAsync(oglas.Id, ct))
                {
                    ponuda.OdbijAutomatski(sada);
                    await obavestenja.ObavestiAsync(ponuda.PonudioKorisnikId, TipObavestenja.PonudaOdbijena, "Oglas je prodat",
                        $"Oglas \"{oglas.Naslov}\" je prodat, pa je vaša ponuda zatvorena.", $"/oglasi/{oglas.Id}", ct);
                }

                break;
        }

        await uow.SacuvajAsync(ct);
        return await OglasProvere.DetaljiAsync(uow, oglas.Id, sada, ct);
    }
}

public sealed record ObrisiOglasCommand(int Id) : IRequest<Unit>;

internal sealed class ObrisiOglasHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme) : IRequestHandler<ObrisiOglasCommand, Unit>
{
    public async Task<Unit> Handle(ObrisiOglasCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var oglas = await OglasProvere.UcitajSvojAsync(uow, z.Id, trenutni.ObavezanId, ct);
        oglas.ObrisiOdStraneVlasnika(sada);
        foreach (var ponuda in await uow.Ponude.NaCekanjuZaOglasAsync(oglas.Id, ct))
        {
            ponuda.OdbijAutomatski(sada);
        }

        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

/// <summary>Pregled oglasa (brojač). Vlasnikovi pregledi se ne broje.</summary>
public sealed record ZabeleziPregledCommand(int OglasId, int ProdavacId) : IRequest<Unit>;

internal sealed class ZabeleziPregledHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<ZabeleziPregledCommand, Unit>
{
    public async Task<Unit> Handle(ZabeleziPregledCommand z, CancellationToken ct)
    {
        if (trenutni.Id != z.ProdavacId)
        {
            await uow.Oglasi.UvecajBrojPregledaAsync(z.OglasId, ct);
        }

        return Unit.Value;
    }
}

// =====================================================================
// SLIKE
// =====================================================================

public sealed record OtpremljenFajl(Stream Sadrzaj, string Naziv, string TipSadrzaja, long Velicina);

public sealed record DodajSlikeCommand(int OglasId, IReadOnlyList<OtpremljenFajl> Fajlovi) : IRequest<IReadOnlyList<SlikaDto>>;

public sealed class DodajSlikeValidator : AbstractValidator<DodajSlikeCommand>
{
    private static readonly string[] Dozvoljeni = ["image/jpeg", "image/png", "image/webp"];

    public DodajSlikeValidator()
    {
        RuleFor(x => x.Fajlovi).NotEmpty().WithMessage("Izaberite bar jednu sliku.")
            .Must(f => f.Count <= PoslovnaPravila.MaksSlikaPoOglasu).WithMessage($"Najviše {PoslovnaPravila.MaksSlikaPoOglasu} slika.");
        RuleForEach(x => x.Fajlovi).ChildRules(f =>
        {
            f.RuleFor(x => x.Velicina).InclusiveBetween(1, PoslovnaPravila.MaksVelicinaSlikeBajtova)
                .WithMessage("Svaka slika može imati najviše 5 MB.");
            f.RuleFor(x => x.TipSadrzaja).Must(t => Dozvoljeni.Contains(t))
                .WithMessage("Dozvoljene su samo JPEG, PNG i WebP slike.");
        });
    }
}

internal sealed class DodajSlikeHandler(
    IUnitOfWork uow, ITrenutniKorisnik trenutni, ISkladisteFajlova skladiste, IDatumVreme vreme, ILogger<DodajSlikeHandler> logger)
    : IRequestHandler<DodajSlikeCommand, IReadOnlyList<SlikaDto>>
{
    public async Task<IReadOnlyList<SlikaDto>> Handle(DodajSlikeCommand z, CancellationToken ct)
    {
        var oglas = await OglasProvere.UcitajSvojAsync(uow, z.OglasId, trenutni.ObavezanId, ct);
        if (oglas.Slike.Count + z.Fajlovi.Count > PoslovnaPravila.MaksSlikaPoOglasu)
        {
            throw new DomenskiIzuzetak("previse_slika",
                $"Oglas već ima {oglas.Slike.Count} slika; ukupno je dozvoljeno {PoslovnaPravila.MaksSlikaPoOglasu}.");
        }

        var sacuvani = new List<string>();
        try
        {
            foreach (var fajl in z.Fajlovi)
            {
                var sacuvan = await skladiste.SacuvajSlikuAsync(fajl.Sadrzaj, fajl.Naziv, fajl.TipSadrzaja, $"oglasi/{oglas.Id}", ct);
                sacuvani.Add(sacuvan.JavnaPutanja);
                oglas.DodajSliku(sacuvan.JavnaPutanja, sacuvan.TipSadrzaja, sacuvan.VelicinaBajtova, vreme.SadaUtc);
            }

            await uow.SacuvajAsync(ct);
        }
        catch
        {
            // Ako upis u bazu ne uspe, brišemo već snimljene fajlove (da ne ostanu "siročići" na disku).
            foreach (var putanja in sacuvani)
            {
                await skladiste.ObrisiAsync(putanja, CancellationToken.None);
            }

            logger.LogWarning("Otpremanje slika za oglas {OglasId} nije uspelo; obrisano {Broj} fajlova.", oglas.Id, sacuvani.Count);
            throw;
        }

        return oglas.Slike.OrderBy(s => s.Redosled)
            .Select(s => new SlikaDto { Id = s.Id, Putanja = s.Putanja, Redosled = s.Redosled }).ToList();
    }
}

public sealed record UkloniSlikuCommand(int OglasId, int SlikaId) : IRequest<Unit>;

internal sealed class UkloniSlikuHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, ISkladisteFajlova skladiste) : IRequestHandler<UkloniSlikuCommand, Unit>
{
    public async Task<Unit> Handle(UkloniSlikuCommand z, CancellationToken ct)
    {
        var oglas = await OglasProvere.UcitajSvojAsync(uow, z.OglasId, trenutni.ObavezanId, ct);
        var slika = oglas.UkloniSliku(z.SlikaId);
        await uow.SacuvajAsync(ct);
        // Fajl se briše tek POSLE uspešnog upisa u bazu.
        await skladiste.ObrisiAsync(slika.Putanja, ct);
        return Unit.Value;
    }
}

public sealed record PromeniRedosledSlikaCommand(int OglasId, IReadOnlyList<int> SlikeIdURedosledu) : IRequest<Unit>;

public sealed class PromeniRedosledSlikaValidator : AbstractValidator<PromeniRedosledSlikaCommand>
{
    public PromeniRedosledSlikaValidator() =>
        RuleFor(x => x.SlikeIdURedosledu).NotEmpty()
            .Must(l => l.Distinct().Count() == l.Count).WithMessage("Svaka slika sme da se pojavi samo jednom.");
}

internal sealed class PromeniRedosledSlikaHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<PromeniRedosledSlikaCommand, Unit>
{
    public async Task<Unit> Handle(PromeniRedosledSlikaCommand z, CancellationToken ct)
    {
        var oglas = await OglasProvere.UcitajSvojAsync(uow, z.OglasId, trenutni.ObavezanId, ct);
        var mapa = oglas.PripremiNoviRedosled(z.SlikeIdURedosledu);
        await uow.Oglasi.PromeniRedosledSlikaAsync(oglas.Id, mapa, ct);
        return Unit.Value;
    }
}

// =====================================================================
// PLAĆENO PODIZANJE (BOOST)
// =====================================================================

public sealed record PodizanjeRezultatDto(int IsticanjeId, DateTime KrajUtc, decimal PlaceniIznos, string ReferencaPlacanja);

public sealed record PodigniOglasCommand(int OglasId, int BrojDana, string? KljucIdempotentnosti) : IRequest<PodizanjeRezultatDto>;

public sealed class PodigniOglasValidator : AbstractValidator<PodigniOglasCommand>
{
    public PodigniOglasValidator()
    {
        RuleFor(x => x.BrojDana).InclusiveBetween(1, PoslovnaPravila.MaksDanaPodizanja)
            .WithMessage($"Podizanje može trajati od 1 do {PoslovnaPravila.MaksDanaPodizanja} dana.");
        RuleFor(x => x.KljucIdempotentnosti).MaximumLength(64);
    }
}

internal sealed class PodigniOglasHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IPlatniProcesor procesor, IDatumVreme vreme)
    : IRequestHandler<PodigniOglasCommand, PodizanjeRezultatDto>
{
    public async Task<PodizanjeRezultatDto> Handle(PodigniOglasCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var korisnikId = trenutni.ObavezanId;
        var oglas = await OglasProvere.UcitajSvojAsync(uow, z.OglasId, korisnikId, ct);

        if (z.KljucIdempotentnosti is not null && await uow.Placanja.PostojiAsync(p => p.KljucIdempotentnosti == z.KljucIdempotentnosti, ct))
        {
            throw new KonfliktIzuzetak("Ovo plaćanje je već obrađeno.");
        }

        if (await uow.IstaknutiOglasi.ImaAktivnoAsync(oglas.Id, TipIsticanja.Podignut, sada, ct))
        {
            throw new DomenskiIzuzetak("vec_podignut", "Oglas je već podignut. Sačekajte da trenutno podizanje istekne.");
        }

        var iznos = IstaknutiOglas.CenaPodizanja(z.BrojDana);
        var opis = $"Podizanje oglasa #{oglas.Id} na {z.BrojDana} dana";
        var rezultat = await procesor.NaplatiAsync(korisnikId, iznos, opis, z.KljucIdempotentnosti, ct);

        var placanje = new Placanje(korisnikId, null, TipPlacanja.Isticanje,
            rezultat.Uspesno ? StatusPlacanja.Uspesno : StatusPlacanja.Neuspesno, iznos, rezultat.ReferencaProcesora,
            z.KljucIdempotentnosti, opis, sada);
        await uow.Placanja.DodajAsync(placanje, ct);

        if (!rezultat.Uspesno)
        {
            // I neuspešno plaćanje se beleži (istorija), pa tek onda prijavljujemo grešku.
            await uow.SacuvajAsync(ct);
            throw new DomenskiIzuzetak("placanje_odbijeno", rezultat.Poruka ?? "Plaćanje je odbijeno.");
        }

        var isticanje = IstaknutiOglas.Podigni(oglas, korisnikId, z.BrojDana, placanje, sada);
        await uow.IstaknutiOglasi.DodajAsync(isticanje, ct);
        await uow.SacuvajAsync(ct);
        return new PodizanjeRezultatDto(isticanje.Id, isticanje.KrajUtc, iznos, placanje.ReferencaProcesora);
    }
}
