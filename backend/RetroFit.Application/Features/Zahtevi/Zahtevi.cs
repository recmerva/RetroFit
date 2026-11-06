using FluentValidation;
using MediatR;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Application.Common.Modeli;
using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Zahtevi;

public interface IZahtevPodaci
{
    string Naslov { get; }
    string Opis { get; }
    int KategorijaId { get; }
    int? GradId { get; }
    decimal? MaksimalnaCena { get; }
}

internal static class ZahtevValidacija
{
    public static void Pravila<T>(AbstractValidator<T> v) where T : IZahtevPodaci
    {
        v.RuleFor(x => x.Naslov).NotEmpty().WithMessage("Naslov je obavezan.").Length(5, 120);
        v.RuleFor(x => x.Opis).NotEmpty().WithMessage("Opis je obavezan.").Length(10, 2000);
        v.RuleFor(x => x.KategorijaId).GreaterThan(0).WithMessage("Izaberite kategoriju.");
        v.RuleFor(x => x.MaksimalnaCena).GreaterThan(0).When(x => x.MaksimalnaCena.HasValue)
            .WithMessage("Maksimalna cena mora biti veća od 0.");
    }

    public static async Task ProveriAsync(IUnitOfWork uow, IZahtevPodaci z, CancellationToken ct)
    {
        if (!await uow.Kategorije.PostojiAsync(k => k.Id == z.KategorijaId && k.Aktivna, ct))
        {
            throw new NijePronadjenoIzuzetak("Kategorija", z.KategorijaId);
        }

        if (z.GradId is not null && !await uow.Gradovi.PostojiAsync(g => g.Id == z.GradId, ct))
        {
            throw new NijePronadjenoIzuzetak("Grad", z.GradId);
        }
    }
}

public sealed record KreirajZahtevCommand(string Naslov, string Opis, int KategorijaId, int? GradId, decimal? MaksimalnaCena)
    : IRequest<ZahtevDto>, IZahtevPodaci;

public sealed class KreirajZahtevValidator : AbstractValidator<KreirajZahtevCommand>
{
    public KreirajZahtevValidator() => ZahtevValidacija.Pravila(this);
}

internal sealed class KreirajZahtevHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<KreirajZahtevCommand, ZahtevDto>
{
    public async Task<ZahtevDto> Handle(KreirajZahtevCommand z, CancellationToken ct)
    {
        await ZahtevValidacija.ProveriAsync(uow, z, ct);
        var zahtev = new ZahtevTrazim(trenutni.ObavezanId, z.Naslov, z.Opis, z.KategorijaId, z.GradId, z.MaksimalnaCena);
        await uow.Zahtevi.DodajAsync(zahtev, ct);
        await uow.SacuvajAsync(ct);
        return (await uow.Zahtevi.DtoAsync(zahtev.Id, ct))!;
    }
}

public sealed record IzmeniZahtevCommand(int Id, string Naslov, string Opis, int KategorijaId, int? GradId, decimal? MaksimalnaCena)
    : IRequest<ZahtevDto>, IZahtevPodaci;

public sealed class IzmeniZahtevValidator : AbstractValidator<IzmeniZahtevCommand>
{
    public IzmeniZahtevValidator() => ZahtevValidacija.Pravila(this);
}

internal sealed class IzmeniZahtevHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<IzmeniZahtevCommand, ZahtevDto>
{
    public async Task<ZahtevDto> Handle(IzmeniZahtevCommand z, CancellationToken ct)
    {
        await ZahtevValidacija.ProveriAsync(uow, z, ct);
        var zahtev = await uow.Zahtevi.PronadjiPoIdAsync(z.Id, ct) ?? throw new NijePronadjenoIzuzetak("Zahtev", z.Id);
        zahtev.Izmeni(trenutni.ObavezanId, z.Naslov, z.Opis, z.KategorijaId, z.GradId, z.MaksimalnaCena);
        await uow.SacuvajAsync(ct);
        return (await uow.Zahtevi.DtoAsync(zahtev.Id, ct))!;
    }
}

public sealed record ZatvoriZahtevCommand(int Id, bool Ispunjen) : IRequest<ZahtevDto>;

internal sealed class ZatvoriZahtevHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<ZatvoriZahtevCommand, ZahtevDto>
{
    public async Task<ZahtevDto> Handle(ZatvoriZahtevCommand z, CancellationToken ct)
    {
        var zahtev = await uow.Zahtevi.PronadjiPoIdAsync(z.Id, ct) ?? throw new NijePronadjenoIzuzetak("Zahtev", z.Id);
        zahtev.Zatvori(trenutni.ObavezanId, z.Ispunjen);
        await uow.SacuvajAsync(ct);
        return (await uow.Zahtevi.DtoAsync(zahtev.Id, ct))!;
    }
}

public sealed record OdgovoriNaZahtevCommand(int ZahtevId, int OglasId, string? Poruka) : IRequest<Unit>;

public sealed class OdgovoriNaZahtevValidator : AbstractValidator<OdgovoriNaZahtevCommand>
{
    public OdgovoriNaZahtevValidator() => RuleFor(x => x.Poruka).MaximumLength(500);
}

internal sealed class OdgovoriNaZahtevHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja, IDatumVreme vreme)
    : IRequestHandler<OdgovoriNaZahtevCommand, Unit>
{
    public async Task<Unit> Handle(OdgovoriNaZahtevCommand z, CancellationToken ct)
    {
        var zahtev = await uow.Zahtevi.PronadjiSaOdgovorimaAsync(z.ZahtevId, ct) ?? throw new NijePronadjenoIzuzetak("Zahtev", z.ZahtevId);
        var oglas = await uow.Oglasi.PronadjiPoIdAsync(z.OglasId, ct) ?? throw new NijePronadjenoIzuzetak("Oglas", z.OglasId);

        zahtev.DodajOdgovor(oglas, trenutni.ObavezanId, z.Poruka, vreme.SadaUtc);
        await obavestenja.ObavestiAsync(zahtev.KorisnikId, TipObavestenja.OdgovorNaZahtev, "Novi odgovor na vaš zahtev",
            $"Neko nudi \"{oglas.Naslov}\" za vaš zahtev \"{zahtev.Naslov}\".", $"/trazim/{zahtev.Id}", ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record ZahteviQuery(string? Upit, int? KategorijaId, int? GradId, bool SamoMoji, bool SamoOtvoreni, Stranicenje Stranicenje)
    : IRequest<StranicaRezultata<ZahtevDto>>;

internal sealed class ZahteviHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<ZahteviQuery, StranicaRezultata<ZahtevDto>>
{
    public async Task<StranicaRezultata<ZahtevDto>> Handle(ZahteviQuery z, CancellationToken ct)
    {
        IReadOnlyList<int>? kategorije = z.KategorijaId is null ? null : await uow.Kategorije.IdSaPodkategorijamaAsync(z.KategorijaId.Value, ct);
        return await uow.Zahtevi.PretraziAsync(z.Upit, kategorije, z.GradId, z.SamoMoji ? trenutni.ObavezanId : null,
            z.SamoOtvoreni ? StatusZahteva.Otvoren : null, z.Stranicenje, ct);
    }
}

public sealed record ZahtevDetaljiQuery(int Id) : IRequest<ZahtevDetaljiDto>;

internal sealed class ZahtevDetaljiHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<ZahtevDetaljiQuery, ZahtevDetaljiDto>
{
    public async Task<ZahtevDetaljiDto> Handle(ZahtevDetaljiQuery z, CancellationToken ct)
    {
        var dto = await uow.Zahtevi.DtoAsync(z.Id, ct) ?? throw new NijePronadjenoIzuzetak("Zahtev", z.Id);
        var odgovori = await uow.Zahtevi.OdgovoriAsync(z.Id, ct);
        var kartice = (await uow.Oglasi.KarticeAsync(odgovori.Select(o => o.OglasId).ToList(), vreme.SadaUtc, ct)).ToDictionary(k => k.Id);

        return new ZahtevDetaljiDto
        {
            Zahtev = dto,
            JeMoj = trenutni.Id == dto.KorisnikId,
            // Prikazuju se samo odgovori čiji oglasi još postoje.
            Odgovori = odgovori.Where(o => kartice.ContainsKey(o.OglasId))
                .Select(o => new OdgovorNaZahtevDto { Id = o.Id, Oglas = kartice[o.OglasId], Poruka = o.Poruka, KreiranoUtc = o.KreiranoUtc })
                .ToList()
        };
    }
}
