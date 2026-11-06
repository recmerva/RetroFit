using FluentValidation;
using MediatR;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Application.Common.Modeli;
using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;

namespace RetroFit.Application.Features.Moderacija;

// =====================================================================
// PRIJAVE OGLASA
// =====================================================================

public sealed record PrijaviOglasCommand(int OglasId, RazlogPrijave Razlog, string? Opis) : IRequest<Unit>;

public sealed class PrijaviOglasValidator : AbstractValidator<PrijaviOglasCommand>
{
    public PrijaviOglasValidator()
    {
        RuleFor(x => x.Razlog).IsInEnum();
        RuleFor(x => x.Opis).MaximumLength(1000);
        RuleFor(x => x.Opis).NotEmpty().When(x => x.Razlog == RazlogPrijave.Ostalo).WithMessage("Opišite razlog prijave.");
    }
}

internal sealed class PrijaviOglasHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme) : IRequestHandler<PrijaviOglasCommand, Unit>
{
    public async Task<Unit> Handle(PrijaviOglasCommand z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        var oglas = await uow.Oglasi.PronadjiPoIdAsync(z.OglasId, ct);
        if (oglas is null || oglas.Obrisan)
        {
            throw new NijePronadjenoIzuzetak("Oglas", z.OglasId);
        }

        if (await uow.Prijave.PostojiAsync(p => p.OglasId == z.OglasId && p.PrijavioKorisnikId == korisnikId && p.Status == StatusPrijave.Otvorena, ct))
        {
            throw new KonfliktIzuzetak("Već ste prijavili ovaj oglas; prijava čeka obradu.");
        }

        await uow.Prijave.DodajAsync(new Prijava(oglas, korisnikId, z.Razlog, z.Opis, vreme.SadaUtc), ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record PrijaveQuery(StatusPrijave? Status, Stranicenje Stranicenje) : IRequest<StranicaRezultata<PrijavaDto>>;

internal sealed class PrijaveHandler(IUnitOfWork uow) : IRequestHandler<PrijaveQuery, StranicaRezultata<PrijavaDto>>
{
    public Task<StranicaRezultata<PrijavaDto>> Handle(PrijaveQuery z, CancellationToken ct) => uow.Prijave.ListaAsync(z.Status, z.Stranicenje, ct);
}

/// <summary>Zajednička logika uklanjanja oglasa (direktno ili posle prihvaćene prijave).</summary>
internal static class UklanjanjeOglasa
{
    public static async Task UkloniAsync(IUnitOfWork uow, IObavestenjaServis obavestenja, Oglas oglas, string razlog, DateTime sada, CancellationToken ct)
    {
        if (await uow.Transakcije.PostojiAsync(t => t.OglasId == oglas.Id && t.Status == StatusTransakcije.UToku, ct))
        {
            throw new DomenskiIzuzetak("kupovina_u_toku",
                "Za oglas je kupovina u toku. Kupovina mora biti završena ili otkazana pre uklanjanja oglasa.");
        }

        var rezervacija = await uow.Rezervacije.AktivnaZaOglasAsync(oglas.Id, ct);
        if (rezervacija is not null)
        {
            rezervacija.OtkaziSistemski("Oglas je uklonjen od strane moderatora.", sada);
            await obavestenja.ObavestiAsync(rezervacija.KupacId, TipObavestenja.RezervacijaOtkazana, "Rezervacija je otkazana",
                $"Oglas \"{oglas.Naslov}\" je uklonjen od strane moderatora, pa je rezervacija otkazana.", null, ct);
        }

        oglas.Ukloni(razlog);
        foreach (var ponuda in await uow.Ponude.NaCekanjuZaOglasAsync(oglas.Id, ct))
        {
            ponuda.OdbijAutomatski(sada);
        }

        await obavestenja.ObavestiAsync(oglas.ProdavacId, TipObavestenja.OglasUklonjen, "Oglas je uklonjen",
            $"Vaš oglas \"{oglas.Naslov}\" je uklonjen. Razlog: {razlog}", $"/oglasi/{oglas.Id}", ct);
    }
}

public sealed record ObradiPrijavuCommand(int PrijavaId, bool Prihvati, bool UkloniOglas, string? Napomena) : IRequest<Unit>;

public sealed class ObradiPrijavuValidator : AbstractValidator<ObradiPrijavuCommand>
{
    public ObradiPrijavuValidator()
    {
        RuleFor(x => x.Napomena).MaximumLength(500);
        RuleFor(x => x).Must(x => x.Prihvati || !x.UkloniOglas).WithName("UkloniOglas")
            .WithMessage("Oglas se može ukloniti samo uz prihvaćenu prijavu.");
        RuleFor(x => x.Napomena).NotEmpty().When(x => x.UkloniOglas).WithMessage("Unesite razlog uklanjanja oglasa.");
    }
}

internal sealed class ObradiPrijavuHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja,
    IAuditServis audit, IDatumVreme vreme) : IRequestHandler<ObradiPrijavuCommand, Unit>
{
    public async Task<Unit> Handle(ObradiPrijavuCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var moderatorId = trenutni.ObavezanId;
        var prijava = await uow.Prijave.PronadjiPoIdAsync(z.PrijavaId, ct) ?? throw new NijePronadjenoIzuzetak("Prijava", z.PrijavaId);

        if (!z.Prihvati)
        {
            prijava.Odbij(moderatorId, z.Napomena, sada);
        }
        else
        {
            // Prihvatanje jedne prijave zatvara SVE otvorene prijave za isti oglas.
            foreach (var p in await uow.Prijave.OtvoreneZaOglasAsync(prijava.OglasId, ct))
            {
                p.Prihvati(moderatorId, z.Napomena, sada);
            }

            if (z.UkloniOglas)
            {
                var oglas = await uow.Oglasi.PronadjiPoIdAsync(prijava.OglasId, ct) ?? throw new NijePronadjenoIzuzetak("Oglas", prijava.OglasId);
                await UklanjanjeOglasa.UkloniAsync(uow, obavestenja, oglas, z.Napomena!, sada, ct);
            }
        }

        await audit.ZabeleziAsync(KategorijaAudita.Moderacija, z.Prihvati ? "PrihvacenaPrijava" : "OdbijenaPrijava", nameof(Prijava),
            prijava.Id.ToString(), new { prijava.OglasId, z.UkloniOglas, z.Napomena }, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record UkloniOglasCommand(int OglasId, string Razlog) : IRequest<Unit>;

public sealed class UkloniOglasValidator : AbstractValidator<UkloniOglasCommand>
{
    public UkloniOglasValidator() => RuleFor(x => x.Razlog).NotEmpty().WithMessage("Razlog je obavezan.").MaximumLength(500);
}

internal sealed class UkloniOglasHandler(IUnitOfWork uow, IObavestenjaServis obavestenja, IAuditServis audit, IDatumVreme vreme)
    : IRequestHandler<UkloniOglasCommand, Unit>
{
    public async Task<Unit> Handle(UkloniOglasCommand z, CancellationToken ct)
    {
        var oglas = await uow.Oglasi.PronadjiPoIdAsync(z.OglasId, ct) ?? throw new NijePronadjenoIzuzetak("Oglas", z.OglasId);
        await UklanjanjeOglasa.UkloniAsync(uow, obavestenja, oglas, z.Razlog, vreme.SadaUtc, ct);
        await audit.ZabeleziAsync(KategorijaAudita.Moderacija, "UklonjenOglas", nameof(Oglas), oglas.Id.ToString(), new { z.Razlog }, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

// =====================================================================
// BLOKIRANJE KORISNIKA
// =====================================================================

public sealed record BlokirajKorisnikaCommand(int KorisnikId, string Razlog, int? TrajanjeDana) : IRequest<Unit>;

public sealed class BlokirajKorisnikaValidator : AbstractValidator<BlokirajKorisnikaCommand>
{
    public BlokirajKorisnikaValidator()
    {
        RuleFor(x => x.Razlog).NotEmpty().WithMessage("Razlog je obavezan.").MaximumLength(500);
        RuleFor(x => x.TrajanjeDana).InclusiveBetween(1, 3650).When(x => x.TrajanjeDana.HasValue);
    }
}

/// <summary>
/// Razlika u ovlašćenjima: moderator može blokirati samo obične korisnike, admin i moderatore,
/// a administratora ne može niko da blokira preko API-ja.
/// </summary>
internal sealed class BlokirajKorisnikaHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja,
    IAuditServis audit, IDatumVreme vreme) : IRequestHandler<BlokirajKorisnikaCommand, Unit>
{
    public async Task<Unit> Handle(BlokirajKorisnikaCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var moderatorId = trenutni.ObavezanId;
        var uloge = await uow.Korisnici.UlogeKorisnikaAsync(z.KorisnikId, ct);
        if (uloge.Count == 0)
        {
            throw new NijePronadjenoIzuzetak("Korisnik", z.KorisnikId);
        }

        if (uloge.Contains(NaziviUloga.Admin))
        {
            throw new ZabranjenoIzuzetak("Administrator ne može biti blokiran.");
        }

        if (uloge.Contains(NaziviUloga.Moderator) && !trenutni.JeUUlozi(NaziviUloga.Admin))
        {
            throw new ZabranjenoIzuzetak("Samo administrator može blokirati moderatora.");
        }

        if (await uow.Blokade.AktivnaZaKorisnikaAsync(z.KorisnikId, sada, ct) is not null)
        {
            throw new KonfliktIzuzetak("Korisnik je već blokiran.");
        }

        var kraj = z.TrajanjeDana is null ? (DateTime?)null : sada.AddDays(z.TrajanjeDana.Value);
        await uow.Blokade.DodajAsync(new BlokadaKorisnika(z.KorisnikId, moderatorId, z.Razlog, sada, kraj), ct);
        // Blokiran korisnik se odmah odjavljuje sa svih uređaja (refresh tokeni prestaju da važe).
        await uow.RefreshTokeni.OpozoviSveZaKorisnikaAsync(z.KorisnikId, "NalogBlokiran", sada, ct);
        await obavestenja.ObavestiAsync(z.KorisnikId, TipObavestenja.NalogBlokiran, "Nalog je blokiran",
            $"Vaš nalog je blokiran {(kraj is null ? "trajno" : $"do {kraj:dd.MM.yyyy.}")}. Razlog: {z.Razlog}", null, ct);
        await audit.ZabeleziAsync(KategorijaAudita.Moderacija, "BlokiranKorisnik", nameof(Korisnik), z.KorisnikId.ToString(),
            new { z.Razlog, z.TrajanjeDana }, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record UkiniBlokaduKorisnikaCommand(int BlokadaId) : IRequest<Unit>;

internal sealed class UkiniBlokaduKorisnikaHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IAuditServis audit, IDatumVreme vreme)
    : IRequestHandler<UkiniBlokaduKorisnikaCommand, Unit>
{
    public async Task<Unit> Handle(UkiniBlokaduKorisnikaCommand z, CancellationToken ct)
    {
        var blokada = await uow.Blokade.PronadjiPoIdAsync(z.BlokadaId, ct) ?? throw new NijePronadjenoIzuzetak("Blokada", z.BlokadaId);
        blokada.Ukini(trenutni.ObavezanId, vreme.SadaUtc);
        await audit.ZabeleziAsync(KategorijaAudita.Moderacija, "UkinutaBlokada", nameof(Korisnik), blokada.KorisnikId.ToString(), null, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record BlokadeKorisnikaQuery(bool SamoAktivne, Stranicenje Stranicenje) : IRequest<StranicaRezultata<BlokadaKorisnikaDto>>;

internal sealed class BlokadeKorisnikaHandler(IUnitOfWork uow, IDatumVreme vreme) : IRequestHandler<BlokadeKorisnikaQuery, StranicaRezultata<BlokadaKorisnikaDto>>
{
    public Task<StranicaRezultata<BlokadaKorisnikaDto>> Handle(BlokadeKorisnikaQuery z, CancellationToken ct) =>
        uow.Blokade.ListaAsync(z.SamoAktivne, z.Stranicenje, vreme.SadaUtc, ct);
}

// =====================================================================
// UREĐAJI (Bot Detection) - samo Admin
// =====================================================================

public sealed record SumnjiviUredjajiQuery(int MinRizik, Stranicenje Stranicenje) : IRequest<StranicaRezultata<OtisakUredjajaDto>>;

internal sealed class SumnjiviUredjajiHandler(IUnitOfWork uow, IDatumVreme vreme) : IRequestHandler<SumnjiviUredjajiQuery, StranicaRezultata<OtisakUredjajaDto>>
{
    public Task<StranicaRezultata<OtisakUredjajaDto>> Handle(SumnjiviUredjajiQuery z, CancellationToken ct) =>
        uow.OtisciUredjaja.SumnjiviAsync(Math.Clamp(z.MinRizik, 0, 100), z.Stranicenje, vreme.SadaUtc, ct);
}

public sealed record BlokadeUredjajaQuery(bool SamoAktivne, Stranicenje Stranicenje) : IRequest<StranicaRezultata<BlokadaUredjajaDto>>;

internal sealed class BlokadeUredjajaHandler(IUnitOfWork uow, IDatumVreme vreme) : IRequestHandler<BlokadeUredjajaQuery, StranicaRezultata<BlokadaUredjajaDto>>
{
    public Task<StranicaRezultata<BlokadaUredjajaDto>> Handle(BlokadeUredjajaQuery z, CancellationToken ct) =>
        uow.OtisciUredjaja.BlokadeAsync(z.SamoAktivne, z.Stranicenje, vreme.SadaUtc, ct);
}

public sealed record BlokirajUredjajCommand(int OtisakId, string Razlog, int? TrajanjeDana) : IRequest<Unit>;

public sealed class BlokirajUredjajValidator : AbstractValidator<BlokirajUredjajCommand>
{
    public BlokirajUredjajValidator()
    {
        RuleFor(x => x.Razlog).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TrajanjeDana).InclusiveBetween(1, 365).When(x => x.TrajanjeDana.HasValue);
    }
}

internal sealed class BlokirajUredjajHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IAuditServis audit, IDatumVreme vreme)
    : IRequestHandler<BlokirajUredjajCommand, Unit>
{
    public async Task<Unit> Handle(BlokirajUredjajCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var otisak = await uow.OtisciUredjaja.PronadjiPoIdAsync(z.OtisakId, ct) ?? throw new NijePronadjenoIzuzetak("Uređaj", z.OtisakId);
        if (await uow.OtisciUredjaja.ImaAktivnuBlokaduAsync(otisak.Id, sada, ct))
        {
            throw new KonfliktIzuzetak("Uređaj je već blokiran.");
        }

        await uow.OtisciUredjaja.DodajBlokaduAsync(new BlokadaUredjaja(otisak.Id, z.Razlog, sada,
            z.TrajanjeDana is null ? null : sada.AddDays(z.TrajanjeDana.Value), trenutni.ObavezanId), ct);
        await audit.ZabeleziAsync(KategorijaAudita.Bezbednost, "BlokiranUredjaj", nameof(OtisakUredjaja), otisak.Id.ToString(), new { z.Razlog }, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record UkiniBlokaduUredjajaCommand(int BlokadaId) : IRequest<Unit>;

internal sealed class UkiniBlokaduUredjajaHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IAuditServis audit, IDatumVreme vreme)
    : IRequestHandler<UkiniBlokaduUredjajaCommand, Unit>
{
    public async Task<Unit> Handle(UkiniBlokaduUredjajaCommand z, CancellationToken ct)
    {
        var blokada = await uow.OtisciUredjaja.BlokadaPoIdAsync(z.BlokadaId, ct) ?? throw new NijePronadjenoIzuzetak("Blokada", z.BlokadaId);
        blokada.Ukini(trenutni.ObavezanId, vreme.SadaUtc);
        await audit.ZabeleziAsync(KategorijaAudita.Bezbednost, "UkinutaBlokadaUredjaja", nameof(OtisakUredjaja),
            blokada.OtisakUredjajaId.ToString(), null, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}
