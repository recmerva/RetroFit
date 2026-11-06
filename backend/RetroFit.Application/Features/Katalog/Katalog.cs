using AutoMapper;
using FluentValidation;
using MediatR;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Katalog;

// =====================================================================
// UPITI
// =====================================================================

public sealed record GradoviQuery : IRequest<IReadOnlyList<GradDto>>;

internal sealed class GradoviHandler(IUnitOfWork uow) : IRequestHandler<GradoviQuery, IReadOnlyList<GradDto>>
{
    public Task<IReadOnlyList<GradDto>> Handle(GradoviQuery zahtev, CancellationToken ct) => uow.Gradovi.SviAsync(ct);
}

/// <summary>Stablo kategorija. Iz baze se čita RAVNA lista (jedan upit), a stablo se gradi u memoriji.</summary>
public sealed record KategorijeQuery(bool UkljuciNeaktivne = false) : IRequest<IReadOnlyList<KategorijaDto>>;

internal sealed class KategorijeHandler(IUnitOfWork uow) : IRequestHandler<KategorijeQuery, IReadOnlyList<KategorijaDto>>
{
    public async Task<IReadOnlyList<KategorijaDto>> Handle(KategorijeQuery zahtev, CancellationToken ct)
    {
        var ravno = await uow.Kategorije.SveRavnoAsync(!zahtev.UkljuciNeaktivne, ct);
        return NapraviStablo(ravno);
    }

    internal static IReadOnlyList<KategorijaDto> NapraviStablo(IReadOnlyList<KategorijaDto> ravno)
    {
        var poRoditelju = ravno.ToLookup(k => k.RoditeljId);

        List<KategorijaDto> Grana(int? roditeljId) => poRoditelju[roditeljId]
            .Select(k =>
            {
                var deca = Grana(k.Id);
                // Broj oglasa nadređene kategorije uključuje i oglase iz podkategorija.
                return k with { Podkategorije = deca, BrojAktivnihOglasa = k.BrojAktivnihOglasa + deca.Sum(d => d.BrojAktivnihOglasa) };
            })
            .OrderBy(k => k.Redosled).ThenBy(k => k.Naziv)
            .ToList();

        return Grana(null);
    }
}

public sealed record DefinicijeAtributaQuery(int KategorijaId) : IRequest<IReadOnlyList<DefinicijaAtributaDto>>;

internal sealed class DefinicijeAtributaHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<DefinicijeAtributaQuery, IReadOnlyList<DefinicijaAtributaDto>>
{
    public async Task<IReadOnlyList<DefinicijaAtributaDto>> Handle(DefinicijeAtributaQuery zahtev, CancellationToken ct)
    {
        if (!await uow.Kategorije.PostojiAsync(k => k.Id == zahtev.KategorijaId, ct))
        {
            throw new NijePronadjenoIzuzetak("Kategorija", zahtev.KategorijaId);
        }

        var definicije = await uow.Kategorije.DefinicijeZaKategorijuAsync(zahtev.KategorijaId, ct);
        return mapper.Map<IReadOnlyList<DefinicijaAtributaDto>>(definicije);
    }
}

// =====================================================================
// ADMIN: UPRAVLJANJE KATEGORIJAMA
// =====================================================================

public sealed record SacuvajKategorijuCommand(int? Id, string Naziv, string? Slug, int? RoditeljId, string? Ikonica, int Redosled)
    : IRequest<KategorijaDto>;

public sealed class SacuvajKategorijuValidator : AbstractValidator<SacuvajKategorijuCommand>
{
    public SacuvajKategorijuValidator()
    {
        RuleFor(x => x.Naziv).NotEmpty().WithMessage("Naziv je obavezan.").MaximumLength(100);
        RuleFor(x => x.Slug).MaximumLength(120);
        RuleFor(x => x.Ikonica).MaximumLength(50);
        RuleFor(x => x.Redosled).InclusiveBetween(0, 1000);
        RuleFor(x => x).Must(x => x.Id is null || x.RoditeljId != x.Id).WithMessage("Kategorija ne može biti sama sebi nadređena.");
    }
}

internal sealed class SacuvajKategorijuHandler(IUnitOfWork uow, IAuditServis audit) : IRequestHandler<SacuvajKategorijuCommand, KategorijaDto>
{
    public async Task<KategorijaDto> Handle(SacuvajKategorijuCommand zahtev, CancellationToken ct)
    {
        if (zahtev.RoditeljId is not null)
        {
            var roditelj = await uow.Kategorije.PronadjiPoIdAsync(zahtev.RoditeljId.Value, ct)
                           ?? throw new NijePronadjenoIzuzetak("Kategorija", zahtev.RoditeljId);
            // Hijerarhija je ograničena na dva nivoa (glavna -> podkategorija), radi preglednosti UI-ja.
            if (roditelj.RoditeljId is not null)
            {
                throw new DomenskiIzuzetak("validacija", "Podkategorija ne može imati svoje podkategorije (najviše dva nivoa).");
            }
        }

        var slug = string.IsNullOrWhiteSpace(zahtev.Slug) ? zahtev.Naziv : zahtev.Slug;
        Kategorija kategorija;
        if (zahtev.Id is null)
        {
            kategorija = new Kategorija(zahtev.Naziv, slug, zahtev.RoditeljId, zahtev.Ikonica, zahtev.Redosled);
            await uow.Kategorije.DodajAsync(kategorija, ct);
        }
        else
        {
            kategorija = await uow.Kategorije.PronadjiPoIdAsync(zahtev.Id.Value, ct) ?? throw new NijePronadjenoIzuzetak("Kategorija", zahtev.Id);
            if (zahtev.RoditeljId is not null && await uow.Kategorije.ImaPodkategorijeAsync(kategorija.Id, ct))
            {
                throw new DomenskiIzuzetak("validacija", "Kategorija koja ima podkategorije ne može postati podkategorija.");
            }

            kategorija.Izmeni(zahtev.Naziv, slug, zahtev.RoditeljId, zahtev.Ikonica, zahtev.Redosled);
        }

        await uow.SacuvajAsync(ct);
        await audit.ZabeleziAsync(KategorijaAudita.Administracija, zahtev.Id is null ? "KreiranaKategorija" : "IzmenjenaKategorija",
            nameof(Kategorija), kategorija.Id.ToString(), new { kategorija.Naziv, kategorija.Slug }, ct);
        await uow.SacuvajAsync(ct);

        return new KategorijaDto
        {
            Id = kategorija.Id,
            Naziv = kategorija.Naziv,
            Slug = kategorija.Slug,
            Ikonica = kategorija.Ikonica,
            Redosled = kategorija.Redosled,
            Aktivna = kategorija.Aktivna,
            RoditeljId = kategorija.RoditeljId
        };
    }
}

public sealed record PromeniAktivnostKategorijeCommand(int Id, bool Aktivna) : IRequest<Unit>;

internal sealed class PromeniAktivnostKategorijeHandler(IUnitOfWork uow, IAuditServis audit) : IRequestHandler<PromeniAktivnostKategorijeCommand, Unit>
{
    public async Task<Unit> Handle(PromeniAktivnostKategorijeCommand zahtev, CancellationToken ct)
    {
        var kategorija = await uow.Kategorije.PronadjiPoIdAsync(zahtev.Id, ct) ?? throw new NijePronadjenoIzuzetak("Kategorija", zahtev.Id);
        if (zahtev.Aktivna)
        {
            kategorija.Aktiviraj();
        }
        else
        {
            kategorija.Deaktiviraj();
        }

        await audit.ZabeleziAsync(KategorijaAudita.Administracija, zahtev.Aktivna ? "AktiviranaKategorija" : "DeaktiviranaKategorija",
            nameof(Kategorija), kategorija.Id.ToString(), null, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

/// <summary>Fizičko brisanje je dozvoljeno samo za kategoriju bez oglasa, podkategorija, atributa i zahteva.</summary>
public sealed record ObrisiKategorijuCommand(int Id) : IRequest<Unit>;

internal sealed class ObrisiKategorijuHandler(IUnitOfWork uow, IAuditServis audit) : IRequestHandler<ObrisiKategorijuCommand, Unit>
{
    public async Task<Unit> Handle(ObrisiKategorijuCommand zahtev, CancellationToken ct)
    {
        var kategorija = await uow.Kategorije.PronadjiSaAtributimaAsync(zahtev.Id, ct) ?? throw new NijePronadjenoIzuzetak("Kategorija", zahtev.Id);

        if (await uow.Kategorije.ImaPodkategorijeAsync(zahtev.Id, ct)
            || await uow.Oglasi.PostojiAsync(o => o.KategorijaId == zahtev.Id, ct)
            || await uow.Zahtevi.PostojiAsync(z => z.KategorijaId == zahtev.Id, ct)
            || kategorija.DefinicijeAtributa.Count > 0)
        {
            throw new DomenskiIzuzetak("kategorija_u_upotrebi",
                "Kategorija se koristi (oglasi, podkategorije, atributi ili zahtevi). Umesto brisanja je deaktivirajte.");
        }

        uow.Kategorije.Ukloni(kategorija);
        await audit.ZabeleziAsync(KategorijaAudita.Administracija, "ObrisanaKategorija", nameof(Kategorija), zahtev.Id.ToString(),
            new { kategorija.Naziv }, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record DodajDefinicijuAtributaCommand(int KategorijaId, string Naziv, string? Kljuc, TipAtributa Tip, bool Obavezan,
    IReadOnlyList<string>? Opcije, string? JedinicaMere, int Redosled) : IRequest<DefinicijaAtributaDto>;

public sealed class DodajDefinicijuAtributaValidator : AbstractValidator<DodajDefinicijuAtributaCommand>
{
    public DodajDefinicijuAtributaValidator()
    {
        RuleFor(x => x.Naziv).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Kljuc).MaximumLength(60);
        RuleFor(x => x.Tip).IsInEnum();
        RuleFor(x => x.Opcije).NotNull().Must(o => o!.Count >= 2).When(x => x.Tip == TipAtributa.Izbor)
            .WithMessage("Atribut tipa 'Izbor' mora imati bar dve opcije.");
        RuleForEach(x => x.Opcije).MaximumLength(50).Must(o => !o.Contains('|')).WithMessage("Opcija ne sme sadržati znak '|'.");
        RuleFor(x => x.JedinicaMere).MaximumLength(20);
    }
}

internal sealed class DodajDefinicijuAtributaHandler(IUnitOfWork uow, IMapper mapper, IAuditServis audit)
    : IRequestHandler<DodajDefinicijuAtributaCommand, DefinicijaAtributaDto>
{
    public async Task<DefinicijaAtributaDto> Handle(DodajDefinicijuAtributaCommand zahtev, CancellationToken ct)
    {
        var kategorija = await uow.Kategorije.PronadjiSaAtributimaAsync(zahtev.KategorijaId, ct)
                         ?? throw new NijePronadjenoIzuzetak("Kategorija", zahtev.KategorijaId);

        var definicija = kategorija.DodajAtribut(new DefinicijaAtributa(kategorija.Id, zahtev.Naziv,
            string.IsNullOrWhiteSpace(zahtev.Kljuc) ? zahtev.Naziv : zahtev.Kljuc,
            zahtev.Tip, zahtev.Obavezan, zahtev.Opcije, zahtev.JedinicaMere, zahtev.Redosled));

        await uow.SacuvajAsync(ct);
        await audit.ZabeleziAsync(KategorijaAudita.Administracija, "DodatAtribut", nameof(DefinicijaAtributa), definicija.Id.ToString(),
            new { zahtev.KategorijaId, definicija.Kljuc }, ct);
        await uow.SacuvajAsync(ct);
        return mapper.Map<DefinicijaAtributaDto>(definicija);
    }
}
