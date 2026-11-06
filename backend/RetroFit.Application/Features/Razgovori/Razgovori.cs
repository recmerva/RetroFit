using FluentValidation;
using MediatR;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Application.Common.Modeli;
using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Razgovori;

/// <summary>Brze (unapred pripremljene) poruke - jedan klik umesto kucanja.</summary>
public static class BrzePoruke
{
    public static readonly IReadOnlyList<BrzaPorukaDto> Sve =
    [
        new("dostupno", "Zdravo! Da li je oglas još uvek dostupan?"),
        new("cena", "Da li je cena fiksna ili je moguć dogovor?"),
        new("stanje", "Možete li mi poslati još detalja o stanju predmeta?"),
        new("dostava", "Da li šaljete brzom poštom?"),
        new("licno", "Kada bih mogao/la lično da pogledam i preuzmem?"),
        new("hvala", "Hvala na brzom odgovoru!")
    ];

    public static string? Tekst(string? kljuc) => Sve.FirstOrDefault(b => b.Kljuc == kljuc)?.Tekst;
}

internal static class RazgovorPomoc
{
    public static async Task<Razgovor> UcitajKaoUcesnikAsync(IUnitOfWork uow, int razgovorId, int korisnikId, CancellationToken ct)
    {
        var razgovor = await uow.Razgovori.PronadjiSaOglasomAsync(razgovorId, ct);
        // Za korisnika koji nije učesnik razgovor "ne postoji" (404) - ne otkrivamo tuđe razgovore.
        if (razgovor is null || !razgovor.JeUcesnik(korisnikId))
        {
            throw new NijePronadjenoIzuzetak("Razgovor", razgovorId);
        }

        return razgovor;
    }

    public static async Task<Razgovor> PronadjiIliKreirajAsync(IUnitOfWork uow, Oglas oglas, int kupacId, DateTime sada, CancellationToken ct)
    {
        var postojeci = await uow.Razgovori.PronadjiZaOglasIKupcaAsync(oglas.Id, kupacId, ct);
        if (postojeci is not null)
        {
            return postojeci;
        }

        var novi = new Razgovor(oglas, kupacId, sada);
        await uow.Razgovori.DodajAsync(novi, ct);
        return novi;
    }

    public static async Task<RazgovorDetaljiDto> DetaljiAsync(IUnitOfWork uow, int razgovorId, int korisnikId, CancellationToken ct)
    {
        var dto = await uow.Razgovori.DetaljiAsync(razgovorId, korisnikId, ct) ?? throw new NijePronadjenoIzuzetak("Razgovor", razgovorId);
        if (!dto.JaSamKupac || !await uow.Rezervacije.KupacImaPravoNaKontaktAsync(dto.Oglas.Id, korisnikId, ct))
        {
            return dto;
        }

        // Pravilo privatnosti: kupac sa prihvaćenom ponudom (rezervacijom) vidi telefon prodavca, ako ga je prodavac dozvolio.
        var prodavac = await uow.Korisnici.PronadjiPoIdAsync(dto.ProdavacId, ct);
        return prodavac is { PrikaziTelefonKupcu: true, TelefonVerifikovan: true } ? dto with { TelefonProdavca = prodavac.Telefon } : dto;
    }
}

// =====================================================================
// KOMANDE
// =====================================================================

public sealed record ZapocniRazgovorCommand(int OglasId, string? Sadrzaj, string? BrzaPorukaKljuc) : IRequest<RazgovorDetaljiDto>;

public sealed class ZapocniRazgovorValidator : AbstractValidator<ZapocniRazgovorCommand>
{
    public ZapocniRazgovorValidator()
    {
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Sadrzaj) || BrzePoruke.Tekst(x.BrzaPorukaKljuc) is not null)
            .WithName("Sadrzaj").WithMessage("Poruka ne sme biti prazna.");
        RuleFor(x => x.Sadrzaj).MaximumLength(2000);
    }
}

internal sealed class ZapocniRazgovorHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja, IDatumVreme vreme)
    : IRequestHandler<ZapocniRazgovorCommand, RazgovorDetaljiDto>
{
    public async Task<RazgovorDetaljiDto> Handle(ZapocniRazgovorCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var korisnikId = trenutni.ObavezanId;
        var oglas = await uow.Oglasi.PronadjiPoIdAsync(z.OglasId, ct) ?? throw new NijePronadjenoIzuzetak("Oglas", z.OglasId);

        var razgovor = await RazgovorPomoc.PronadjiIliKreirajAsync(uow, oglas, korisnikId, sada, ct);
        var brza = BrzePoruke.Tekst(z.BrzaPorukaKljuc);
        razgovor.PosaljiPoruku(korisnikId, brza ?? z.Sadrzaj!, brza is null ? TipPoruke.Tekst : TipPoruke.BrzaPoruka, sada);
        await uow.SacuvajAsync(ct);

        // Obaveštenje posle prvog čuvanja, jer je tek tada poznat Id novog razgovora (za link).
        await obavestenja.ObavestiAsync(oglas.ProdavacId, TipObavestenja.NovaPoruka, "Nova poruka",
            $"Imate novu poruku za oglas \"{oglas.Naslov}\".", $"/razgovori/{razgovor.Id}", ct);
        await uow.SacuvajAsync(ct);
        return await RazgovorPomoc.DetaljiAsync(uow, razgovor.Id, korisnikId, ct);
    }
}

public sealed record PosaljiPorukuCommand(int RazgovorId, string? Sadrzaj, string? BrzaPorukaKljuc) : IRequest<PorukaDto>;

public sealed class PosaljiPorukuValidator : AbstractValidator<PosaljiPorukuCommand>
{
    public PosaljiPorukuValidator()
    {
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Sadrzaj) || BrzePoruke.Tekst(x.BrzaPorukaKljuc) is not null)
            .WithName("Sadrzaj").WithMessage("Poruka ne sme biti prazna.");
        RuleFor(x => x.Sadrzaj).MaximumLength(2000).WithMessage("Poruka može imati najviše 2000 karaktera.");
    }
}

internal sealed class PosaljiPorukuHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja, IDatumVreme vreme)
    : IRequestHandler<PosaljiPorukuCommand, PorukaDto>
{
    public async Task<PorukaDto> Handle(PosaljiPorukuCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var korisnikId = trenutni.ObavezanId;
        var razgovor = await RazgovorPomoc.UcitajKaoUcesnikAsync(uow, z.RazgovorId, korisnikId, ct);
        if (razgovor.Oglas.Status == StatusOglasa.Uklonjen)
        {
            throw new DomenskiIzuzetak("oglas_uklonjen", "Oglas je uklonjen od strane moderatora; razgovor je zatvoren.");
        }

        var brza = BrzePoruke.Tekst(z.BrzaPorukaKljuc);
        var poruka = razgovor.PosaljiPoruku(korisnikId, brza ?? z.Sadrzaj!, brza is null ? TipPoruke.Tekst : TipPoruke.BrzaPoruka, sada);
        await obavestenja.ObavestiAsync(razgovor.DrugiUcesnik(korisnikId), TipObavestenja.NovaPoruka, "Nova poruka",
            $"Nova poruka za oglas \"{razgovor.Oglas.Naslov}\".", $"/razgovori/{razgovor.Id}", ct);
        await uow.SacuvajAsync(ct);

        return new PorukaDto
        {
            Id = poruka.Id,
            RazgovorId = razgovor.Id,
            PosiljalacId = korisnikId,
            Sadrzaj = poruka.Sadrzaj,
            Tip = poruka.Tip,
            PoslatoUtc = poruka.PoslatoUtc
        };
    }
}

public sealed record OznaciRazgovorProcitanimCommand(int RazgovorId) : IRequest<int>;

internal sealed class OznaciRazgovorProcitanimHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<OznaciRazgovorProcitanimCommand, int>
{
    public async Task<int> Handle(OznaciRazgovorProcitanimCommand z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        await RazgovorPomoc.UcitajKaoUcesnikAsync(uow, z.RazgovorId, korisnikId, ct);
        return await uow.Razgovori.OznaciProcitaneAsync(z.RazgovorId, korisnikId, vreme.SadaUtc, ct);
    }
}

// =====================================================================
// UPITI
// =====================================================================

public sealed record RazgovoriQuery(Stranicenje Stranicenje) : IRequest<StranicaRezultata<RazgovorPregledDto>>;

internal sealed class RazgovoriHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<RazgovoriQuery, StranicaRezultata<RazgovorPregledDto>>
{
    public Task<StranicaRezultata<RazgovorPregledDto>> Handle(RazgovoriQuery z, CancellationToken ct) =>
        uow.Razgovori.ZaKorisnikaAsync(trenutni.ObavezanId, z.Stranicenje, ct);
}

public sealed record RazgovorDetaljiQuery(int Id) : IRequest<RazgovorDetaljiDto>;

internal sealed class RazgovorDetaljiHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<RazgovorDetaljiQuery, RazgovorDetaljiDto>
{
    public async Task<RazgovorDetaljiDto> Handle(RazgovorDetaljiQuery z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        await RazgovorPomoc.UcitajKaoUcesnikAsync(uow, z.Id, korisnikId, ct);
        return await RazgovorPomoc.DetaljiAsync(uow, z.Id, korisnikId, ct);
    }
}

public sealed record PorukeQuery(int RazgovorId, Stranicenje Stranicenje) : IRequest<StranicaRezultata<PorukaDto>>;

internal sealed class PorukeHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<PorukeQuery, StranicaRezultata<PorukaDto>>
{
    public async Task<StranicaRezultata<PorukaDto>> Handle(PorukeQuery z, CancellationToken ct)
    {
        await RazgovorPomoc.UcitajKaoUcesnikAsync(uow, z.RazgovorId, trenutni.ObavezanId, ct);
        return await uow.Razgovori.PorukeAsync(z.RazgovorId, z.Stranicenje, ct);
    }
}

public sealed record BrzePorukeQuery : IRequest<IReadOnlyList<BrzaPorukaDto>>;

internal sealed class BrzePorukeHandler : IRequestHandler<BrzePorukeQuery, IReadOnlyList<BrzaPorukaDto>>
{
    public Task<IReadOnlyList<BrzaPorukaDto>> Handle(BrzePorukeQuery z, CancellationToken ct) => Task.FromResult(BrzePoruke.Sve);
}
