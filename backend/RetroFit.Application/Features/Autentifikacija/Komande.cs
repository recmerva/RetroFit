using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Application.Common.Validacija;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;

namespace RetroFit.Application.Features.Autentifikacija;

// =====================================================================
// REGISTRACIJA
// =====================================================================

public sealed record RegistracijaCommand(string Email, string Lozinka, string Ime, string Prezime, int? GradId)
    : IRequest<AutentifikacijaRezultat>;

public sealed class RegistracijaValidator : AbstractValidator<RegistracijaCommand>
{
    public RegistracijaValidator()
    {
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Lozinka).JakaLozinka();
        RuleFor(x => x.Ime).Ime("Ime");
        RuleFor(x => x.Prezime).Ime("Prezime");
        RuleFor(x => x.GradId).GreaterThan(0).When(x => x.GradId.HasValue).WithMessage("Grad nije ispravan.");
    }
}

internal sealed class RegistracijaHandler(
    IUnitOfWork uow,
    IHesiranjeLozinke hesiranje,
    IzdavanjeTokena izdavanje,
    ITrenutniKorisnik trenutni,
    IAuditServis audit,
    IDatumVreme vreme) : IRequestHandler<RegistracijaCommand, AutentifikacijaRezultat>
{
    public async Task<AutentifikacijaRezultat> Handle(RegistracijaCommand zahtev, CancellationToken ct)
    {
        var normalizovan = Korisnik.Normalizuj(zahtev.Email);
        if (await uow.Korisnici.PostojiEmailAsync(normalizovan, ct))
        {
            // Napomena: ovo otkriva da email postoji. Za registraciju je to prihvaćen kompromis
            // (korisnik mora znati zašto ne može da se registruje); zaštitu od enumeracije daje rate limit.
            throw new KonfliktIzuzetak("Nalog sa ovom email adresom već postoji.");
        }

        if (zahtev.GradId is not null && !await uow.Gradovi.PostojiAsync(g => g.Id == zahtev.GradId, ct))
        {
            throw new NijePronadjenoIzuzetak("Grad", zahtev.GradId);
        }

        var korisnik = new Korisnik(zahtev.Email, zahtev.Ime, zahtev.Prezime, zahtev.GradId);
        korisnik.PostaviLozinkuHash(hesiranje.Hesiraj(zahtev.Lozinka));
        var uloga = await uow.Uloge.PrviAsync(u => u.Naziv == NaziviUloga.Korisnik, ct)
                    ?? throw new InvalidOperationException("Uloga 'Korisnik' ne postoji u bazi.");
        korisnik.DodeliUlogu(uloga, vreme.SadaUtc);
        await uow.Korisnici.DodajAsync(korisnik, ct);

        // Povezivanje sa Bot Detection middleware-om: broj registracija sa istog otiska uređaja.
        if (trenutni.OtisakUredjaja is not null)
        {
            var otisak = await uow.OtisciUredjaja.PoHashuAsync(trenutni.OtisakUredjaja, ct);
            if (otisak is null)
            {
                otisak = new OtisakUredjaja(trenutni.OtisakUredjaja, null, vreme.SadaUtc);
                await uow.OtisciUredjaja.DodajAsync(otisak, ct);
            }

            otisak.ZabeleziRegistraciju();
        }

        await uow.SacuvajAsync(ct);
        await audit.ZabeleziAsync(KategorijaAudita.Bezbednost, "Registracija", nameof(Korisnik), korisnik.Id.ToString(), null, ct);

        return await izdavanje.IzdajAsync(korisnik, null, null, ct);
    }
}

// =====================================================================
// PRIJAVA
// =====================================================================

public sealed record PrijavaCommand(string Email, string Lozinka) : IRequest<AutentifikacijaRezultat>;

public sealed class PrijavaValidator : AbstractValidator<PrijavaCommand>
{
    public PrijavaValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email je obavezan.").MaximumLength(256);
        RuleFor(x => x.Lozinka).NotEmpty().WithMessage("Lozinka je obavezna.").MaximumLength(128);
    }
}

internal sealed class PrijavaHandler(
    IUnitOfWork uow,
    IHesiranjeLozinke hesiranje,
    IzdavanjeTokena izdavanje,
    IAuditServis audit,
    IDatumVreme vreme,
    ILogger<PrijavaHandler> logger) : IRequestHandler<PrijavaCommand, AutentifikacijaRezultat>
{
    // Lažni heš za slučaj kada korisnik ne postoji: provera traje isto kao prava, pa napadač
    // ne može po vremenu odgovora da zaključi da li email postoji (timing attack).
    private static string? _lazniHes;

    public async Task<AutentifikacijaRezultat> Handle(PrijavaCommand zahtev, CancellationToken ct)
    {
        var korisnik = await uow.Korisnici.PronadjiPoEmailuAsync(Korisnik.Normalizuj(zahtev.Email), ct);
        bool ispravna;
        if (korisnik is null)
        {
            _lazniHes ??= hesiranje.Hesiraj(Guid.NewGuid().ToString());
            hesiranje.Proveri(_lazniHes, zahtev.Lozinka);
            ispravna = false;
        }
        else
        {
            ispravna = hesiranje.Proveri(korisnik.LozinkaHash, zahtev.Lozinka);
        }

        if (korisnik is null || !ispravna)
        {
            logger.LogWarning("Neuspešna prijava za nalog {KorisnikId}", korisnik?.Id);
            await audit.ZabeleziAsync(KategorijaAudita.Bezbednost, "NeuspesnaPrijava", nameof(Korisnik), korisnik?.Id.ToString(),
                new { Razlog = korisnik is null ? "NepostojeciNalog" : "PogresnaLozinka" }, ct);
            await uow.SacuvajAsync(ct);
            throw new NeovlascenIzuzetak("Pogrešan email ili lozinka.");
        }

        var blokada = await uow.Blokade.AktivnaZaKorisnikaAsync(korisnik.Id, vreme.SadaUtc, ct);
        if (blokada is not null)
        {
            await audit.ZabeleziAsync(KategorijaAudita.Bezbednost, "PrijavaBlokiranogNaloga", nameof(Korisnik), korisnik.Id.ToString(), null, ct);
            await uow.SacuvajAsync(ct);
            var doKada = blokada.KrajUtc is null ? "trajno" : $"do {blokada.KrajUtc:dd.MM.yyyy. HH:mm} (UTC)";
            throw new BlokiranIzuzetak($"Nalog je blokiran {doKada}. Razlog: {blokada.Razlog}");
        }

        korisnik.EvidentirajPrijavu(vreme.SadaUtc);
        await audit.ZabeleziAsync(KategorijaAudita.Bezbednost, "Prijava", nameof(Korisnik), korisnik.Id.ToString(), null, ct);
        return await izdavanje.IzdajAsync(korisnik, null, null, ct);
    }
}

// =====================================================================
// OSVEŽAVANJE TOKENA (ROTACIJA)
// =====================================================================

public sealed record OsveziTokenCommand(string RefreshToken) : IRequest<AutentifikacijaRezultat>;

internal sealed class OsveziTokenHandler(
    IUnitOfWork uow,
    ITokenServis tokeni,
    IzdavanjeTokena izdavanje,
    IAuditServis audit,
    IDatumVreme vreme,
    ILogger<OsveziTokenHandler> logger) : IRequestHandler<OsveziTokenCommand, AutentifikacijaRezultat>
{
    public async Task<AutentifikacijaRezultat> Handle(OsveziTokenCommand zahtev, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(zahtev.RefreshToken))
        {
            throw new NeovlascenIzuzetak("Sesija je istekla. Prijavite se ponovo.");
        }

        var sada = vreme.SadaUtc;
        var token = await uow.RefreshTokeni.PronadjiPoHashuAsync(tokeni.Hesiraj(zahtev.RefreshToken), ct);
        if (token is null)
        {
            throw new NeovlascenIzuzetak("Sesija je istekla. Prijavite se ponovo.");
        }

        var istovremeno = token.JeOpozvan && await JeIstovremenoOsvezavanjeAsync(token, sada, ct);
        if (token.JeOpozvan && !istovremeno)
        {
            // DETEKCIJA PONOVNE UPOTREBE: legitiman klijent nikad ne šalje već rotiran token.
            // Ako se to desi, verovatno je token ukraden -> opozivamo CELU porodicu (i napadača i žrtvu).
            var opozvano = await uow.RefreshTokeni.OpozoviPorodicuAsync(token.PorodicaId, "PonovnaUpotreba", sada, ct);
            logger.LogWarning("Ponovna upotreba refresh tokena za korisnika {KorisnikId}; opozvano {Broj} tokena iz porodice {Porodica}",
                token.KorisnikId, opozvano, token.PorodicaId);
            await audit.ZabeleziAsync(KategorijaAudita.Bezbednost, "PonovnaUpotrebaRefreshTokena", nameof(RefreshToken), token.Id.ToString(),
                new { token.PorodicaId, Opozvano = opozvano }, ct);
            await uow.SacuvajAsync(ct);
            throw new NeovlascenIzuzetak("Sesija je poništena iz bezbednosnih razloga. Prijavite se ponovo.");
        }

        if (token.JeIstekao(sada) || token.Korisnik.Obrisan)
        {
            throw new NeovlascenIzuzetak("Sesija je istekla. Prijavite se ponovo.");
        }

        if (await uow.Blokade.AktivnaZaKorisnikaAsync(token.KorisnikId, sada, ct) is not null)
        {
            token.Opozovi(sada, "NalogBlokiran");
            await uow.SacuvajAsync(ct);
            throw new BlokiranIzuzetak("Nalog je blokiran.");
        }

        if (istovremeno)
        {
            // Stari token je već rotiran (opozvan) - izdajemo još jedan token u istoj porodici, bez novog opoziva.
            logger.LogInformation("Istovremeno osvežavanje sesije korisnika {KorisnikId} (porodica {Porodica}) - u okviru tolerancije",
                token.KorisnikId, token.PorodicaId);
            return await izdavanje.IzdajAsync(token.Korisnik, token.PorodicaId, null, ct);
        }

        return await izdavanje.IzdajAsync(token.Korisnik, token.PorodicaId, token, ct);
    }

    /// <summary>
    /// Opozvan token se ipak prihvata ako je rotiran pre najviše nekoliko sekundi I njegov naslednik je još
    /// važeći. Tipičan slučaj: dva taba ili dva paralelna zahteva šalju isti kolačić; prvi rotira token, drugi
    /// stiže milisekundama kasnije. Ukradeni token upotrebljen kasnije (ili posle odjave / opoziva porodice)
    /// i dalje pokreće detekciju ponovne upotrebe.
    /// </summary>
    private async Task<bool> JeIstovremenoOsvezavanjeAsync(RefreshToken token, DateTime sada, CancellationToken ct)
    {
        var tolerancija = TimeSpan.FromSeconds(PoslovnaPravila.TolerancijaRotacijeTokenaSekundi);
        if (!token.JeNedavnoRotiran(sada, tolerancija) || token.ZamenjenHashom is null)
        {
            return false;
        }

        var naslednik = await uow.RefreshTokeni.PronadjiPoHashuAsync(token.ZamenjenHashom, ct);
        return naslednik is not null && (naslednik.JeAktivan(sada) || naslednik.JeNedavnoRotiran(sada, tolerancija));
    }
}

// =====================================================================
// ODJAVA
// =====================================================================

public sealed record OdjavaCommand(string? RefreshToken, bool SaSvihUredjaja) : IRequest<Unit>;

internal sealed class OdjavaHandler(IUnitOfWork uow, ITokenServis tokeni, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<OdjavaCommand, Unit>
{
    public async Task<Unit> Handle(OdjavaCommand zahtev, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        if (zahtev.SaSvihUredjaja && trenutni.Id is { } korisnikId)
        {
            await uow.RefreshTokeni.OpozoviSveZaKorisnikaAsync(korisnikId, "OdjavaSaSvihUredjaja", sada, ct);
            return Unit.Value;
        }

        if (!string.IsNullOrWhiteSpace(zahtev.RefreshToken))
        {
            var token = await uow.RefreshTokeni.PronadjiPoHashuAsync(tokeni.Hesiraj(zahtev.RefreshToken), ct);
            if (token is not null && (trenutni.Id is null || trenutni.Id == token.KorisnikId))
            {
                token.Opozovi(sada, "Odjava");
                await uow.SacuvajAsync(ct);
            }
        }

        return Unit.Value;
    }
}
