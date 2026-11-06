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

namespace RetroFit.Application.Features.Oglasi;

// =====================================================================
// DETALJI OGLASA
// =====================================================================

public sealed record OglasDetaljiQuery(int Id) : IRequest<OglasDetaljiDto>;

internal sealed class OglasDetaljiHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<OglasDetaljiQuery, OglasDetaljiDto>
{
    public async Task<OglasDetaljiDto> Handle(OglasDetaljiQuery z, CancellationToken ct)
    {
        var dto = await uow.Oglasi.DetaljiAsync(z.Id, vreme.SadaUtc, ct) ?? throw new NijePronadjenoIzuzetak("Oglas", z.Id);
        var korisnikId = trenutni.Id;
        var jeMoj = korisnikId == dto.Prodavac.Id;
        var jeModerator = trenutni.JeUUlozi(NaziviUloga.Moderator) || trenutni.JeUUlozi(NaziviUloga.Admin);

        // Nacrt, istekao i uklonjen oglas vide samo vlasnik i moderatori; za ostale "ne postoji" (404, ne 403).
        var javnoVidljiv = dto.Status is StatusOglasa.Aktivan or StatusOglasa.Rezervisan or StatusOglasa.Prodat;
        if (!javnoVidljiv && !jeMoj && !jeModerator)
        {
            throw new NijePronadjenoIzuzetak("Oglas", z.Id);
        }

        if (korisnikId is null || jeMoj)
        {
            return dto with { JeMoj = jeMoj };
        }

        var jeOmiljen = await uow.Omiljeni.PronadjiAsync(korisnikId.Value, dto.Id, ct) is not null;
        var razgovor = await uow.Razgovori.PronadjiZaOglasIKupcaAsync(dto.Id, korisnikId.Value, ct);
        var telefon = await TelefonProdavcaAsync(dto, korisnikId.Value, ct);

        return dto with { JeOmiljen = jeOmiljen, MojRazgovorId = razgovor?.Id, TelefonProdavca = telefon };
    }

    /// <summary>
    /// PRAVILO PRIVATNOSTI: broj telefona prodavca se prikazuje SAMO ako
    ///  1) gledalac ima aktivnu ili realizovanu rezervaciju za ovaj oglas (nastaje iz prihvaćene ponude),
    ///  2) prodavac je u podešavanjima dozvolio prikaz telefona kupcu, i
    ///  3) broj je verifikovan.
    /// </summary>
    private async Task<string?> TelefonProdavcaAsync(OglasDetaljiDto dto, int korisnikId, CancellationToken ct)
    {
        if (!await uow.Rezervacije.KupacImaPravoNaKontaktAsync(dto.Id, korisnikId, ct))
        {
            return null;
        }

        var prodavac = await uow.Korisnici.PronadjiPoIdAsync(dto.Prodavac.Id, ct);
        return prodavac is { PrikaziTelefonKupcu: true, TelefonVerifikovan: true } ? prodavac.Telefon : null;
    }
}

// =====================================================================
// PRETRAGA
// =====================================================================

public sealed record PretragaOglasaQuery(KriterijumiPretrage Kriterijumi) : IRequest<StranicaRezultata<OglasKarticaDto>>;

public sealed class PretragaOglasaValidator : AbstractValidator<PretragaOglasaQuery>
{
    public PretragaOglasaValidator()
    {
        RuleFor(x => x.Kriterijumi.Upit).MaximumLength(200);
        RuleFor(x => x.Kriterijumi.CenaOd).GreaterThanOrEqualTo(0).When(x => x.Kriterijumi.CenaOd.HasValue);
        RuleFor(x => x.Kriterijumi).Must(k => k.CenaOd is null || k.CenaDo is null || k.CenaOd <= k.CenaDo)
            .WithName("Cena").WithMessage("'Cena od' mora biti manja ili jednaka 'Cena do'.");
        RuleFor(x => x.Kriterijumi.RadijusKm).InclusiveBetween(1, 500).When(x => x.Kriterijumi.RadijusKm.HasValue)
            .WithMessage("Radijus mora biti između 1 i 500 km.");
        RuleFor(x => x.Kriterijumi.GeografskaSirina).InclusiveBetween(-90, 90).When(x => x.Kriterijumi.GeografskaSirina.HasValue);
        RuleFor(x => x.Kriterijumi.GeografskaDuzina).InclusiveBetween(-180, 180).When(x => x.Kriterijumi.GeografskaDuzina.HasValue);
        RuleFor(x => x.Kriterijumi).Must(k => (k.GeografskaSirina is null) == (k.GeografskaDuzina is null))
            .WithName("Lokacija").WithMessage("Unesite i geografsku širinu i dužinu.");
        RuleFor(x => x.Kriterijumi).Must(k => k.Sortiranje != SortiranjeOglasa.Udaljenost || k.GeografskaSirina is not null || k.GradId is not null)
            .WithName("Sortiranje").WithMessage("Za sortiranje po udaljenosti potrebna je lokacija ili grad.");
    }
}

internal sealed class PretragaOglasaHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<PretragaOglasaQuery, StranicaRezultata<OglasKarticaDto>>
{
    public async Task<StranicaRezultata<OglasKarticaDto>> Handle(PretragaOglasaQuery z, CancellationToken ct)
    {
        var k = z.Kriterijumi;
        IReadOnlyList<int>? kategorije = k.KategorijaId is null ? null : await uow.Kategorije.IdSaPodkategorijamaAsync(k.KategorijaId.Value, ct);
        var rezultat = await uow.Oglasi.PretraziAsync(k, kategorije, vreme.SadaUtc, ct);
        return await OznaciOmiljene.UStraniciAsync(uow, trenutni, rezultat, ct);
    }
}

/// <summary>Označava kartice koje je prijavljeni korisnik dodao u omiljene (jedan dodatni upit za celu stranicu).</summary>
internal static class OznaciOmiljene
{
    public static async Task<IReadOnlyList<OglasKarticaDto>> ListaAsync(IUnitOfWork uow, ITrenutniKorisnik trenutni,
        IReadOnlyList<OglasKarticaDto> kartice, CancellationToken ct)
    {
        if (trenutni.Id is null || kartice.Count == 0)
        {
            return kartice;
        }

        var omiljeni = (await uow.Omiljeni.IdOmiljenihAsync(trenutni.Id.Value, kartice.Select(x => x.Id).ToList(), ct)).ToHashSet();
        return kartice.Select(x => x with { JeOmiljen = omiljeni.Contains(x.Id) }).ToList();
    }

    public static async Task<StranicaRezultata<OglasKarticaDto>> UStraniciAsync(IUnitOfWork uow, ITrenutniKorisnik trenutni,
        StranicaRezultata<OglasKarticaDto> stranica, CancellationToken ct) =>
        stranica with { Stavke = await ListaAsync(uow, trenutni, stranica.Stavke, ct) };
}

public sealed record MojiOglasiQuery(StatusOglasa? Status, Stranicenje Stranicenje) : IRequest<StranicaRezultata<OglasKarticaDto>>;

internal sealed class MojiOglasiHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<MojiOglasiQuery, StranicaRezultata<OglasKarticaDto>>
{
    public Task<StranicaRezultata<OglasKarticaDto>> Handle(MojiOglasiQuery z, CancellationToken ct) =>
        uow.Oglasi.OglasiProdavcaAsync(trenutni.ObavezanId, false, z.Status, z.Stranicenje, vreme.SadaUtc, ct);
}

public sealed record OglasiKorisnikaQuery(int KorisnikId, Stranicenje Stranicenje) : IRequest<StranicaRezultata<OglasKarticaDto>>;

internal sealed class OglasiKorisnikaHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<OglasiKorisnikaQuery, StranicaRezultata<OglasKarticaDto>>
{
    public async Task<StranicaRezultata<OglasKarticaDto>> Handle(OglasiKorisnikaQuery z, CancellationToken ct)
    {
        var rezultat = await uow.Oglasi.OglasiProdavcaAsync(z.KorisnikId, true, null, z.Stranicenje, vreme.SadaUtc, ct);
        return await OznaciOmiljene.UStraniciAsync(uow, trenutni, rezultat, ct);
    }
}

public sealed record IzdvojeniOglasiQuery(int Maks = 8) : IRequest<IReadOnlyList<OglasKarticaDto>>;

internal sealed class IzdvojeniOglasiHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<IzdvojeniOglasiQuery, IReadOnlyList<OglasKarticaDto>>
{
    public async Task<IReadOnlyList<OglasKarticaDto>> Handle(IzdvojeniOglasiQuery z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var id = await uow.IstaknutiOglasi.IzdvojeniIdAsync(Math.Clamp(z.Maks, 1, 24), sada, ct);
        var kartice = await uow.Oglasi.KarticeAsync(id, sada, ct);
        return await OznaciOmiljene.ListaAsync(uow, trenutni, kartice, ct);
    }
}

// =====================================================================
// OMILJENI OGLASI (lista želja)
// =====================================================================

public sealed record DodajUOmiljeneCommand(int OglasId) : IRequest<Unit>;

internal sealed class DodajUOmiljeneHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme) : IRequestHandler<DodajUOmiljeneCommand, Unit>
{
    public async Task<Unit> Handle(DodajUOmiljeneCommand z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        var oglas = await uow.Oglasi.PronadjiPoIdAsync(z.OglasId, ct);
        if (oglas is null || oglas.Obrisan || oglas.Status is not (StatusOglasa.Aktivan or StatusOglasa.Rezervisan))
        {
            throw new NijePronadjenoIzuzetak("Oglas", z.OglasId);
        }

        if (oglas.JeVlasnik(korisnikId))
        {
            throw new DomenskiIzuzetak("sopstveni_oglas", "Ne možete dodati sopstveni oglas u omiljene.");
        }

        // Idempotentno: ako je već u omiljenima, ne radi ništa (umesto greške).
        if (await uow.Omiljeni.PronadjiAsync(korisnikId, z.OglasId, ct) is not null)
        {
            return Unit.Value;
        }

        await uow.Omiljeni.DodajAsync(new OmiljeniOglas(korisnikId, z.OglasId, vreme.SadaUtc), ct);
        oglas.PromeniBrojOmiljenih(+1);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record UkloniIzOmiljenihCommand(int OglasId) : IRequest<Unit>;

internal sealed class UkloniIzOmiljenihHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<UkloniIzOmiljenihCommand, Unit>
{
    public async Task<Unit> Handle(UkloniIzOmiljenihCommand z, CancellationToken ct)
    {
        var omiljeni = await uow.Omiljeni.PronadjiAsync(trenutni.ObavezanId, z.OglasId, ct);
        if (omiljeni is null)
        {
            return Unit.Value;
        }

        uow.Omiljeni.Ukloni(omiljeni);
        var oglas = await uow.Oglasi.PronadjiPoIdAsync(z.OglasId, ct);
        oglas?.PromeniBrojOmiljenih(-1);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record OmiljeniQuery(Stranicenje Stranicenje) : IRequest<StranicaRezultata<OglasKarticaDto>>;

internal sealed class OmiljeniHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<OmiljeniQuery, StranicaRezultata<OglasKarticaDto>>
{
    public async Task<StranicaRezultata<OglasKarticaDto>> Handle(OmiljeniQuery z, CancellationToken ct)
    {
        var stranica = await uow.Omiljeni.OmiljeniKorisnikaAsync(trenutni.ObavezanId, z.Stranicenje, vreme.SadaUtc, ct);
        return stranica with { Stavke = stranica.Stavke.Select(s => s with { JeOmiljen = true }).ToList() };
    }
}

// =====================================================================
// SAČUVANE PRETRAGE
// =====================================================================

public sealed record SacuvajPretraguCommand(int? Id, string Naziv, string? Upit, int? KategorijaId, int? GradId,
    decimal? CenaOd, decimal? CenaDo, StanjeProizvoda? Stanje, TipOglasa? Tip, bool Obavestavaj) : IRequest<SacuvanaPretragaDto>;

public sealed class SacuvajPretraguValidator : AbstractValidator<SacuvajPretraguCommand>
{
    public SacuvajPretraguValidator()
    {
        RuleFor(x => x.Naziv).NotEmpty().WithMessage("Naziv pretrage je obavezan.").MaximumLength(100);
        RuleFor(x => x.Upit).MaximumLength(200);
        RuleFor(x => x.CenaOd).GreaterThanOrEqualTo(0).When(x => x.CenaOd.HasValue);
        RuleFor(x => x).Must(x => x.CenaOd is null || x.CenaDo is null || x.CenaOd <= x.CenaDo)
            .WithName("Cena").WithMessage("'Cena od' mora biti manja ili jednaka 'Cena do'.");
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Upit) || x.KategorijaId is not null || x.GradId is not null
                                  || x.CenaOd is not null || x.CenaDo is not null || x.Stanje is not null || x.Tip is not null)
            .WithName("Kriterijumi").WithMessage("Sačuvana pretraga mora imati bar jedan kriterijum.");
    }
}

internal sealed class SacuvajPretraguHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<SacuvajPretraguCommand, SacuvanaPretragaDto>
{
    public async Task<SacuvanaPretragaDto> Handle(SacuvajPretraguCommand z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        SacuvanaPretraga pretraga;
        if (z.Id is null)
        {
            if (await uow.SacuvanePretrage.PrebrojAsync(p => p.KorisnikId == korisnikId, ct) >= PoslovnaPravila.MaksSacuvanihPretraga)
            {
                throw new DomenskiIzuzetak("previse_pretraga", $"Možete imati najviše {PoslovnaPravila.MaksSacuvanihPretraga} sačuvanih pretraga.");
            }

            pretraga = new SacuvanaPretraga(korisnikId, z.Naziv, z.Upit, z.KategorijaId, z.GradId, z.CenaOd, z.CenaDo, z.Stanje, z.Tip,
                z.Obavestavaj, vreme.SadaUtc);
            await uow.SacuvanePretrage.DodajAsync(pretraga, ct);
        }
        else
        {
            var postojeca = await uow.SacuvanePretrage.PronadjiPoIdAsync(z.Id.Value, ct);
            if (postojeca is null || postojeca.KorisnikId != korisnikId)
            {
                throw new NijePronadjenoIzuzetak("Sačuvana pretraga", z.Id);
            }

            pretraga = postojeca;

            pretraga.Izmeni(z.Naziv, z.Upit, z.KategorijaId, z.GradId, z.CenaOd, z.CenaDo, z.Stanje, z.Tip, z.Obavestavaj);
        }

        await uow.SacuvajAsync(ct);
        return (await uow.SacuvanePretrage.ZaKorisnikaAsync(korisnikId, ct)).First(p => p.Id == pretraga.Id);
    }
}

public sealed record ObrisiPretraguCommand(int Id) : IRequest<Unit>;

internal sealed class ObrisiPretraguHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<ObrisiPretraguCommand, Unit>
{
    public async Task<Unit> Handle(ObrisiPretraguCommand z, CancellationToken ct)
    {
        var pretraga = await uow.SacuvanePretrage.PronadjiPoIdAsync(z.Id, ct);
        if (pretraga is null || pretraga.KorisnikId != trenutni.ObavezanId)
        {
            throw new NijePronadjenoIzuzetak("Sačuvana pretraga", z.Id);
        }

        uow.SacuvanePretrage.Ukloni(pretraga);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record SacuvanePretrageQuery : IRequest<IReadOnlyList<SacuvanaPretragaDto>>;

internal sealed class SacuvanePretrageHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni)
    : IRequestHandler<SacuvanePretrageQuery, IReadOnlyList<SacuvanaPretragaDto>>
{
    public Task<IReadOnlyList<SacuvanaPretragaDto>> Handle(SacuvanePretrageQuery z, CancellationToken ct) =>
        uow.SacuvanePretrage.ZaKorisnikaAsync(trenutni.ObavezanId, ct);
}

// =====================================================================
// ADMIN: IZDVAJANJE OGLASA
// =====================================================================

public sealed record IzdvojiOglasCommand(int OglasId, int BrojDana) : IRequest<Unit>;

public sealed class IzdvojiOglasValidator : AbstractValidator<IzdvojiOglasCommand>
{
    public IzdvojiOglasValidator() => RuleFor(x => x.BrojDana).InclusiveBetween(1, 90);
}

internal sealed class IzdvojiOglasHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IAuditServis audit, IDatumVreme vreme)
    : IRequestHandler<IzdvojiOglasCommand, Unit>
{
    public async Task<Unit> Handle(IzdvojiOglasCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var oglas = await uow.Oglasi.PronadjiPoIdAsync(z.OglasId, ct) ?? throw new NijePronadjenoIzuzetak("Oglas", z.OglasId);
        if (await uow.IstaknutiOglasi.ImaAktivnoAsync(oglas.Id, TipIsticanja.Izdvojen, sada, ct))
        {
            throw new DomenskiIzuzetak("vec_izdvojen", "Oglas je već izdvojen.");
        }

        await uow.IstaknutiOglasi.DodajAsync(IstaknutiOglas.Izdvoji(oglas, trenutni.ObavezanId, sada, sada.AddDays(z.BrojDana)), ct);
        await audit.ZabeleziAsync(KategorijaAudita.Administracija, "IzdvojenOglas", nameof(Oglas), oglas.Id.ToString(), new { z.BrojDana }, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record OtkaziIsticanjeCommand(int IsticanjeId) : IRequest<Unit>;

internal sealed class OtkaziIsticanjeHandler(IUnitOfWork uow, IAuditServis audit, IDatumVreme vreme) : IRequestHandler<OtkaziIsticanjeCommand, Unit>
{
    public async Task<Unit> Handle(OtkaziIsticanjeCommand z, CancellationToken ct)
    {
        var isticanje = await uow.IstaknutiOglasi.PronadjiPoIdAsync(z.IsticanjeId, ct) ?? throw new NijePronadjenoIzuzetak("Isticanje", z.IsticanjeId);
        isticanje.Otkazi(vreme.SadaUtc);
        await audit.ZabeleziAsync(KategorijaAudita.Administracija, "OtkazanoIsticanje", nameof(IstaknutiOglas), isticanje.Id.ToString(), null, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record IstaknutiOglasiAdminQuery(bool SamoAktivna, Stranicenje Stranicenje) : IRequest<StranicaRezultata<IstaknutiOglasDto>>;

internal sealed class IstaknutiOglasiAdminHandler(IUnitOfWork uow, IDatumVreme vreme)
    : IRequestHandler<IstaknutiOglasiAdminQuery, StranicaRezultata<IstaknutiOglasDto>>
{
    public Task<StranicaRezultata<IstaknutiOglasDto>> Handle(IstaknutiOglasiAdminQuery z, CancellationToken ct) =>
        uow.IstaknutiOglasi.ListaAsync(z.SamoAktivna, z.Stranicenje, vreme.SadaUtc, ct);
}
