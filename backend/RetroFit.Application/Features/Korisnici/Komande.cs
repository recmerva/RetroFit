using System.Security.Cryptography;
using FluentValidation;
using MediatR;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Common.Validacija;
using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;

namespace RetroFit.Application.Features.Korisnici;

// =====================================================================
// UPITI
// =====================================================================

public sealed record MojProfilQuery : IRequest<MojProfilDto>;

internal sealed class MojProfilHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<MojProfilQuery, MojProfilDto>
{
    public async Task<MojProfilDto> Handle(MojProfilQuery zahtev, CancellationToken ct) =>
        await uow.Korisnici.MojProfilAsync(trenutni.ObavezanId, ct) ?? throw new NijePronadjenoIzuzetak("Korisnik", trenutni.ObavezanId);
}

public sealed record JavniProfilQuery(int KorisnikId) : IRequest<JavniProfilDto>;

internal sealed class JavniProfilHandler(IUnitOfWork uow) : IRequestHandler<JavniProfilQuery, JavniProfilDto>
{
    public async Task<JavniProfilDto> Handle(JavniProfilQuery zahtev, CancellationToken ct) =>
        await uow.Korisnici.JavniProfilAsync(zahtev.KorisnikId, ct) ?? throw new NijePronadjenoIzuzetak("Korisnik", zahtev.KorisnikId);
}

public sealed record KorisniciAdminQuery(string? Upit, Stranicenje Stranicenje) : IRequest<StranicaRezultata<KorisnikAdminDto>>;

internal sealed class KorisniciAdminHandler(IUnitOfWork uow, IDatumVreme vreme)
    : IRequestHandler<KorisniciAdminQuery, StranicaRezultata<KorisnikAdminDto>>
{
    public Task<StranicaRezultata<KorisnikAdminDto>> Handle(KorisniciAdminQuery zahtev, CancellationToken ct) =>
        uow.Korisnici.PretraziZaAdminaAsync(zahtev.Upit, zahtev.Stranicenje, vreme.SadaUtc, ct);
}

// =====================================================================
// IZMENA PROFILA
// =====================================================================

public sealed record AzurirajProfilCommand(string Ime, string Prezime, string? OKorisniku, int? GradId, bool PrikaziTelefonKupcu)
    : IRequest<MojProfilDto>;

public sealed class AzurirajProfilValidator : AbstractValidator<AzurirajProfilCommand>
{
    public AzurirajProfilValidator()
    {
        RuleFor(x => x.Ime).Ime("Ime");
        RuleFor(x => x.Prezime).Ime("Prezime");
        RuleFor(x => x.OKorisniku).MaximumLength(500).WithMessage("Opis može imati najviše 500 karaktera.");
        RuleFor(x => x.GradId).GreaterThan(0).When(x => x.GradId.HasValue);
    }
}

internal sealed class AzurirajProfilHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<AzurirajProfilCommand, MojProfilDto>
{
    public async Task<MojProfilDto> Handle(AzurirajProfilCommand zahtev, CancellationToken ct)
    {
        var korisnik = await uow.Korisnici.PronadjiPoIdAsync(trenutni.ObavezanId, ct)
                       ?? throw new NijePronadjenoIzuzetak("Korisnik", trenutni.ObavezanId);

        if (zahtev.GradId is not null && !await uow.Gradovi.PostojiAsync(g => g.Id == zahtev.GradId, ct))
        {
            throw new NijePronadjenoIzuzetak("Grad", zahtev.GradId);
        }

        korisnik.AzurirajProfil(zahtev.Ime, zahtev.Prezime, zahtev.OKorisniku, zahtev.GradId, zahtev.PrikaziTelefonKupcu);
        await uow.SacuvajAsync(ct);
        return (await uow.Korisnici.MojProfilAsync(korisnik.Id, ct))!;
    }
}

// =====================================================================
// PROMENA LOZINKE
// =====================================================================

public sealed record PromeniLozinkuCommand(string TrenutnaLozinka, string NovaLozinka) : IRequest<Unit>;

public sealed class PromeniLozinkuValidator : AbstractValidator<PromeniLozinkuCommand>
{
    public PromeniLozinkuValidator()
    {
        RuleFor(x => x.TrenutnaLozinka).NotEmpty().WithMessage("Unesite trenutnu lozinku.");
        RuleFor(x => x.NovaLozinka).JakaLozinka()
            .NotEqual(x => x.TrenutnaLozinka).WithMessage("Nova lozinka mora biti različita od trenutne.");
    }
}

internal sealed class PromeniLozinkuHandler(
    IUnitOfWork uow, ITrenutniKorisnik trenutni, IHesiranjeLozinke hesiranje, IAuditServis audit, IDatumVreme vreme)
    : IRequestHandler<PromeniLozinkuCommand, Unit>
{
    public async Task<Unit> Handle(PromeniLozinkuCommand zahtev, CancellationToken ct)
    {
        var korisnik = await uow.Korisnici.PronadjiPoIdAsync(trenutni.ObavezanId, ct)
                       ?? throw new NijePronadjenoIzuzetak("Korisnik", trenutni.ObavezanId);

        if (!hesiranje.Proveri(korisnik.LozinkaHash, zahtev.TrenutnaLozinka))
        {
            throw new DomenskiIzuzetak("pogresna_lozinka", "Trenutna lozinka nije ispravna.");
        }

        korisnik.PostaviLozinkuHash(hesiranje.Hesiraj(zahtev.NovaLozinka));
        // Posle promene lozinke sve postojeće sesije (refresh tokeni) prestaju da važe.
        await uow.RefreshTokeni.OpozoviSveZaKorisnikaAsync(korisnik.Id, "PromenaLozinke", vreme.SadaUtc, ct);
        await audit.ZabeleziAsync(KategorijaAudita.Bezbednost, "PromenaLozinke", nameof(Korisnik), korisnik.Id.ToString(), null, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

// =====================================================================
// VERIFIKACIJA TELEFONA (mock SMS)
// =====================================================================

public sealed record VerifikacijaPoslataDto(string Telefon, DateTime IsticeUtc);

public sealed record ZatraziVerifikacijuTelefonaCommand(string Telefon) : IRequest<VerifikacijaPoslataDto>;

public sealed class ZatraziVerifikacijuTelefonaValidator : AbstractValidator<ZatraziVerifikacijuTelefonaCommand>
{
    public ZatraziVerifikacijuTelefonaValidator() => RuleFor(x => x.Telefon).Telefon();
}

internal sealed class ZatraziVerifikacijuTelefonaHandler(
    IUnitOfWork uow, ITrenutniKorisnik trenutni, ISmsServis sms, ITokenServis tokeni, IDatumVreme vreme)
    : IRequestHandler<ZatraziVerifikacijuTelefonaCommand, VerifikacijaPoslataDto>
{
    private const int MaksKodovaU15Minuta = 3;

    public async Task<VerifikacijaPoslataDto> Handle(ZatraziVerifikacijuTelefonaCommand zahtev, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var korisnikId = trenutni.ObavezanId;
        var telefon = BrojTelefona.Normalizuj(zahtev.Telefon)!;

        var nedavno = await uow.Verifikacije.PrebrojAsync(v => v.KorisnikId == korisnikId && v.KreiranoUtc > sada.AddMinutes(-15), ct);
        if (nedavno >= MaksKodovaU15Minuta)
        {
            throw new DomenskiIzuzetak("previse_zahteva", "Zatražili ste previše kodova. Pokušajte ponovo za 15 minuta.");
        }

        if (await uow.Korisnici.TelefonZauzetAsync(telefon, korisnikId, ct))
        {
            throw new KonfliktIzuzetak("Ovaj broj telefona već koristi drugi nalog.");
        }

        var korisnik = await uow.Korisnici.PronadjiPoIdAsync(korisnikId, ct) ?? throw new NijePronadjenoIzuzetak("Korisnik", korisnikId);
        korisnik.PostaviTelefon(telefon);

        // Prethodni neiskorišćeni kodovi se poništavaju - važi samo poslednji.
        foreach (var stara in await uow.Verifikacije.PronadjiAsync(v => v.KorisnikId == korisnikId && v.IskorisceneUtc == null, ct))
        {
            stara.Ponisti(sada);
        }

        // Kriptografski slučajan šestocifren kod; u bazi se čuva samo heš.
        var kod = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var verifikacija = new VerifikacijaKorisnika(korisnikId, TipVerifikacije.Telefon, telefon, tokeni.Hesiraj($"{korisnikId}:{kod}"), sada);
        await uow.Verifikacije.DodajAsync(verifikacija, ct);
        await uow.SacuvajAsync(ct);

        await sms.PosaljiAsync(telefon, $"RetroFit kod za potvrdu broja: {kod}. Važi {PoslovnaPravila.TrajanjeVerifikacionogKodaMinuta} minuta.", ct);
        return new VerifikacijaPoslataDto(telefon, verifikacija.IsticeUtc);
    }
}

public sealed record PotvrdiTelefonCommand(string Kod) : IRequest<MojProfilDto>;

public sealed class PotvrdiTelefonValidator : AbstractValidator<PotvrdiTelefonCommand>
{
    public PotvrdiTelefonValidator() =>
        RuleFor(x => x.Kod).NotEmpty().Length(6).Must(k => k.All(char.IsDigit)).WithMessage("Kod mora imati tačno 6 cifara.");
}

internal sealed class PotvrdiTelefonHandler(
    IUnitOfWork uow, ITrenutniKorisnik trenutni, ITokenServis tokeni, IAuditServis audit, IDatumVreme vreme)
    : IRequestHandler<PotvrdiTelefonCommand, MojProfilDto>
{
    public async Task<MojProfilDto> Handle(PotvrdiTelefonCommand zahtev, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var korisnikId = trenutni.ObavezanId;

        var verifikacije = await uow.Verifikacije.PronadjiAsync(
            v => v.KorisnikId == korisnikId && v.Tip == TipVerifikacije.Telefon && v.IskorisceneUtc == null, ct);
        var verifikacija = verifikacije.OrderByDescending(v => v.KreiranoUtc).FirstOrDefault(v => v.MozeSeKoristiti(sada))
                           ?? throw new DomenskiIzuzetak("verifikacija_istekla", "Nema aktivnog koda. Zatražite novi kod.");

        var ispravan = verifikacija.Proveri(tokeni.Hesiraj($"{korisnikId}:{zahtev.Kod}"), sada);
        if (!ispravan)
        {
            // Pokušaj se ČUVA i kada je kod pogrešan (inače bi ograničenje broja pokušaja bilo besmisleno).
            await uow.SacuvajAsync(ct);
            var preostalo = PoslovnaPravila.MaksPokusajaVerifikacije - verifikacija.BrojPokusaja;
            throw new DomenskiIzuzetak("pogresan_kod", $"Kod nije ispravan. Preostalo pokušaja: {Math.Max(0, preostalo)}.");
        }

        var korisnik = await uow.Korisnici.PronadjiPoIdAsync(korisnikId, ct) ?? throw new NijePronadjenoIzuzetak("Korisnik", korisnikId);
        korisnik.PotvrdiTelefon(verifikacija.Vrednost, sada);
        await audit.ZabeleziAsync(KategorijaAudita.Bezbednost, "TelefonVerifikovan", nameof(Korisnik), korisnikId.ToString(), null, ct);
        await uow.SacuvajAsync(ct);
        return (await uow.Korisnici.MojProfilAsync(korisnikId, ct))!;
    }
}

// =====================================================================
// BRISANJE NALOGA
// =====================================================================

public sealed record ObrisiNalogCommand(string Lozinka) : IRequest<Unit>;

internal sealed class ObrisiNalogHandler(
    IUnitOfWork uow, ITrenutniKorisnik trenutni, IHesiranjeLozinke hesiranje, IAuditServis audit, IDatumVreme vreme)
    : IRequestHandler<ObrisiNalogCommand, Unit>
{
    public async Task<Unit> Handle(ObrisiNalogCommand zahtev, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var korisnikId = trenutni.ObavezanId;
        var korisnik = await uow.Korisnici.PronadjiPoIdAsync(korisnikId, ct) ?? throw new NijePronadjenoIzuzetak("Korisnik", korisnikId);

        if (!hesiranje.Proveri(korisnik.LozinkaHash, zahtev.Lozinka))
        {
            throw new DomenskiIzuzetak("pogresna_lozinka", "Lozinka nije ispravna.");
        }

        var imaOtvoreno = await uow.Rezervacije.PostojiAsync(
                              r => (r.KupacId == korisnikId || r.ProdavacId == korisnikId) && r.Status == StatusRezervacije.Aktivna, ct)
                          || await uow.Transakcije.PostojiAsync(
                              t => (t.KupacId == korisnikId || t.ProdavacId == korisnikId) && t.Status == StatusTransakcije.UToku, ct);
        if (imaOtvoreno)
        {
            throw new DomenskiIzuzetak("otvorene_transakcije",
                "Nalog ne može da se obriše dok imate aktivne rezervacije ili kupovine u toku.");
        }

        foreach (var oglas in await uow.Oglasi.PronadjiAsync(o => o.ProdavacId == korisnikId && !o.Obrisan, ct))
        {
            if (oglas.Status != StatusOglasa.Prodat)
            {
                oglas.ObrisiOdStraneVlasnika(sada);
            }
        }

        korisnik.Obrisi(sada);
        await uow.RefreshTokeni.OpozoviSveZaKorisnikaAsync(korisnikId, "BrisanjeNaloga", sada, ct);
        await audit.ZabeleziAsync(KategorijaAudita.Bezbednost, "BrisanjeNaloga", nameof(Korisnik), korisnikId.ToString(), null, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

// =====================================================================
// ADMIN: PROMENA ULOGA
// =====================================================================

public sealed record PromeniUlogeCommand(int KorisnikId, IReadOnlyList<string> Uloge) : IRequest<Unit>;

public sealed class PromeniUlogeValidator : AbstractValidator<PromeniUlogeCommand>
{
    public PromeniUlogeValidator()
    {
        RuleFor(x => x.Uloge).NotEmpty().WithMessage("Korisnik mora imati bar jednu ulogu.");
        RuleForEach(x => x.Uloge).Must(u => NaziviUloga.Sve.Contains(u)).WithMessage("Nepoznata uloga: {PropertyValue}.");
        RuleFor(x => x.Uloge).Must(u => u.Contains(NaziviUloga.Korisnik)).WithMessage("Uloga 'Korisnik' je obavezna.");
    }
}

internal sealed class PromeniUlogeHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IAuditServis audit, IDatumVreme vreme)
    : IRequestHandler<PromeniUlogeCommand, Unit>
{
    public async Task<Unit> Handle(PromeniUlogeCommand zahtev, CancellationToken ct)
    {
        if (zahtev.KorisnikId == trenutni.ObavezanId && !zahtev.Uloge.Contains(NaziviUloga.Admin))
        {
            throw new DomenskiIzuzetak("sopstvena_uloga", "Ne možete sebi ukloniti administratorsku ulogu.");
        }

        var korisnik = await uow.Korisnici.PronadjiSaUlogamaAsync(zahtev.KorisnikId, ct)
                       ?? throw new NijePronadjenoIzuzetak("Korisnik", zahtev.KorisnikId);
        var sveUloge = await uow.Uloge.PronadjiAsync(_ => true, ct);
        var stare = korisnik.Uloge.Select(u => u.Uloga.Naziv).ToList();

        foreach (var uloga in sveUloge)
        {
            if (zahtev.Uloge.Contains(uloga.Naziv))
            {
                korisnik.DodeliUlogu(uloga, vreme.SadaUtc);
            }
            else
            {
                korisnik.UkloniUlogu(uloga.Id);
            }
        }

        await audit.ZabeleziAsync(KategorijaAudita.Administracija, "PromenaUloga", nameof(Korisnik), korisnik.Id.ToString(),
            new { Stare = stare, Nove = zahtev.Uloge }, ct);
        // Uloge su u access tokenu, pa korisnik mora ponovo da se prijavi da bi nove uloge važile.
        await uow.RefreshTokeni.OpozoviSveZaKorisnikaAsync(korisnik.Id, "PromenaUloga", vreme.SadaUtc, ct);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}
