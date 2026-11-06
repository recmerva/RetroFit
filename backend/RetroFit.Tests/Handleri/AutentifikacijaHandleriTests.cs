using Microsoft.Extensions.Logging.Abstractions;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Application.Features.Autentifikacija;
using RetroFit.Application.Features.Korisnici;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;
using RetroFit.Tests.Pomocne;
using static RetroFit.Tests.Pomocne.TestPodaci;

namespace RetroFit.Tests.Handleri;

public class AutentifikacijaHandleriTests
{
    private readonly LazniUnitOfWork _uow = new();
    private readonly Mock<IHesiranjeLozinke> _hes = new();
    private readonly Mock<ITokenServis> _tokeni = new();
    private readonly Mock<IAuditServis> _audit = new();
    private readonly FiksnoVreme _vreme = new(Sada);

    public AutentifikacijaHandleriTests()
    {
        _tokeni.Setup(t => t.KreirajAccessToken(It.IsAny<Korisnik>(), It.IsAny<IEnumerable<string>>())).Returns(("ACCESS", Sada.AddMinutes(15)));
        _tokeni.Setup(t => t.KreirajRefreshToken()).Returns(("REFRESH", "HES-NOVOG", Sada.AddDays(14)));
        _tokeni.Setup(t => t.Hesiraj(It.IsAny<string>())).Returns((string s) => "HES-" + s);
        _uow.Korisnici.Setup(k => k.UlogeKorisnikaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(["Korisnik"]);
        _uow.Korisnici.Setup(k => k.MojProfilAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new MojProfilDto { Id = id });
    }

    private IzdavanjeTokena Izdavanje() => new(_uow.Object, _tokeni.Object, LazniKorisnik.Za(null), _vreme);

    private PrijavaHandler Prijava() =>
        new(_uow.Object, _hes.Object, Izdavanje(), _audit.Object, _vreme, NullLogger<PrijavaHandler>.Instance);

    [Fact]
    public async Task Pogresna_lozinka_vraca_401_i_belezi_bezbednosni_dogadjaj()
    {
        var korisnik = NoviKorisnik(5);
        _uow.Korisnici.Setup(k => k.PronadjiPoEmailuAsync("TEST@PRIMER.RS", It.IsAny<CancellationToken>())).ReturnsAsync(korisnik);
        _hes.Setup(h => h.Proveri(It.IsAny<string>(), "pogresna")).Returns(false);

        var akcija = () => Prijava().Handle(new PrijavaCommand("test@primer.rs", "pogresna"), default);

        await akcija.Should().ThrowAsync<NeovlascenIzuzetak>().WithMessage("Pogrešan email ili lozinka.");
        _audit.Verify(a => a.ZabeleziAsync(KategorijaAudita.Bezbednost, "NeuspesnaPrijava", It.IsAny<string>(), "5",
            It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.RefreshTokeni.Verify(r => r.DodajAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Nepostojeci_nalog_daje_istu_poruku_kao_pogresna_lozinka()
    {
        _hes.Setup(h => h.Hesiraj(It.IsAny<string>())).Returns("LAZNI");
        var akcija = () => Prijava().Handle(new PrijavaCommand("nema@primer.rs", "bilo-sta-1"), default);
        await akcija.Should().ThrowAsync<NeovlascenIzuzetak>().WithMessage("Pogrešan email ili lozinka.");
        // Proverava se lažni heš, da odgovor traje isto kao za postojeći nalog (zaštita od timing napada).
        _hes.Verify(h => h.Proveri(It.IsAny<string>(), "bilo-sta-1"), Times.Once);
    }

    [Fact]
    public async Task Blokiran_korisnik_ne_moze_da_se_prijavi()
    {
        var korisnik = NoviKorisnik(5);
        _uow.Korisnici.Setup(k => k.PronadjiPoEmailuAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(korisnik);
        _hes.Setup(h => h.Proveri(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _uow.Blokade.Setup(b => b.AktivnaZaKorisnikaAsync(5, Sada, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlokadaKorisnika(5, 2, "Prevara", Sada.AddDays(-1), null));

        var akcija = () => Prijava().Handle(new PrijavaCommand("test@primer.rs", "Lozinka123"), default);

        await akcija.Should().ThrowAsync<BlokiranIzuzetak>().WithMessage("*trajno*Prevara*");
    }

    [Fact]
    public async Task Uspesna_prijava_izdaje_tokene_i_cuva_samo_hes_refresh_tokena()
    {
        var korisnik = NoviKorisnik(5);
        _uow.Korisnici.Setup(k => k.PronadjiPoEmailuAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(korisnik);
        _hes.Setup(h => h.Proveri(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        RefreshToken? sacuvan = null;
        _uow.RefreshTokeni.Setup(r => r.DodajAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback((RefreshToken t, CancellationToken _) => sacuvan = t);

        var rezultat = await Prijava().Handle(new PrijavaCommand("test@primer.rs", "Lozinka123"), default);

        rezultat.AccessToken.Should().Be("ACCESS");
        rezultat.RefreshToken.Should().Be("REFRESH");
        sacuvan!.TokenHash.Should().Be("HES-NOVOG", "u bazi je samo heš, ne sam token");
        korisnik.PoslednjaPrijavaUtc.Should().Be(Sada);
    }

    [Fact]
    public async Task Ponovna_upotreba_rotiranog_refresh_tokena_opoziva_celu_porodicu()
    {
        var porodica = Guid.NewGuid();
        var stari = new RefreshToken(5, "HES-STARI", porodica, Sada.AddDays(-1), Sada.AddDays(13), null);
        stari.Opozovi(Sada.AddHours(-1), "Rotacija", "HES-NOVIJI");
        _uow.RefreshTokeni.Setup(r => r.PronadjiPoHashuAsync("HES-STARI", It.IsAny<CancellationToken>())).ReturnsAsync(stari);
        var handler = new OsveziTokenHandler(_uow.Object, _tokeni.Object, Izdavanje(), _audit.Object, _vreme,
            NullLogger<OsveziTokenHandler>.Instance);

        var akcija = () => handler.Handle(new OsveziTokenCommand("STARI"), default);

        await akcija.Should().ThrowAsync<NeovlascenIzuzetak>();
        _uow.RefreshTokeni.Verify(r => r.OpozoviPorodicuAsync(porodica, "PonovnaUpotreba", Sada, It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.ZabeleziAsync(KategorijaAudita.Bezbednost, "PonovnaUpotrebaRefreshTokena", It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rotacija_opoziva_stari_token_i_izdaje_novi_u_istoj_porodici()
    {
        var porodica = Guid.NewGuid();
        var korisnik = NoviKorisnik(5);
        var stari = new RefreshToken(5, "HES-STARI", porodica, Sada.AddDays(-1), Sada.AddDays(13), null);
        TestPodaci.PostaviSvojstvo(stari, nameof(RefreshToken.Korisnik), korisnik);
        _uow.RefreshTokeni.Setup(r => r.PronadjiPoHashuAsync("HES-STARI", It.IsAny<CancellationToken>())).ReturnsAsync(stari);
        RefreshToken? novi = null;
        _uow.RefreshTokeni.Setup(r => r.DodajAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback((RefreshToken t, CancellationToken _) => novi = t);
        var handler = new OsveziTokenHandler(_uow.Object, _tokeni.Object, Izdavanje(), _audit.Object, _vreme,
            NullLogger<OsveziTokenHandler>.Instance);

        await handler.Handle(new OsveziTokenCommand("STARI"), default);

        stari.JeOpozvan.Should().BeTrue();
        stari.ZamenjenHashom.Should().Be("HES-NOVOG");
        novi!.PorodicaId.Should().Be(porodica);
    }

    private OsveziTokenHandler OsvezavanjeSaNaslednikom(Guid porodica, out RefreshToken naslednik, DateTime rotiranUtc)
    {
        var korisnik = NoviKorisnik(5);
        var stari = new RefreshToken(5, "HES-STARI", porodica, Sada.AddDays(-1), Sada.AddDays(13), null);
        TestPodaci.PostaviSvojstvo(stari, nameof(RefreshToken.Korisnik), korisnik);
        stari.Opozovi(rotiranUtc, RefreshToken.RazlogRotacije, "HES-NASLEDNIK");
        naslednik = new RefreshToken(5, "HES-NASLEDNIK", porodica, rotiranUtc, Sada.AddDays(14), null);
        _uow.RefreshTokeni.Setup(r => r.PronadjiPoHashuAsync("HES-STARI", It.IsAny<CancellationToken>())).ReturnsAsync(stari);
        _uow.RefreshTokeni.Setup(r => r.PronadjiPoHashuAsync("HES-NASLEDNIK", It.IsAny<CancellationToken>())).ReturnsAsync(naslednik);
        return new OsveziTokenHandler(_uow.Object, _tokeni.Object, Izdavanje(), _audit.Object, _vreme, NullLogger<OsveziTokenHandler>.Instance);
    }

    [Fact]
    public async Task Istovremeno_osvezavanje_u_okviru_tolerancije_ne_ponistava_sesiju()
    {
        var porodica = Guid.NewGuid();
        var handler = OsvezavanjeSaNaslednikom(porodica, out var naslednik, Sada.AddSeconds(-2));
        RefreshToken? novi = null;
        _uow.RefreshTokeni.Setup(r => r.DodajAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback((RefreshToken t, CancellationToken _) => novi = t);

        var rezultat = await handler.Handle(new OsveziTokenCommand("STARI"), default);

        rezultat.AccessToken.Should().Be("ACCESS");
        novi!.PorodicaId.Should().Be(porodica);
        naslednik.JeAktivan(Sada).Should().BeTrue("drugi tab i dalje koristi naslednika");
        _uow.RefreshTokeni.Verify(r => r.OpozoviPorodicuAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Stari_token_posle_isteka_tolerancije_je_ponovna_upotreba()
    {
        var porodica = Guid.NewGuid();
        var handler = OsvezavanjeSaNaslednikom(porodica, out _, Sada.AddSeconds(-(PoslovnaPravila.TolerancijaRotacijeTokenaSekundi + 1)));

        var akcija = () => handler.Handle(new OsveziTokenCommand("STARI"), default);

        await akcija.Should().ThrowAsync<NeovlascenIzuzetak>();
        _uow.RefreshTokeni.Verify(r => r.OpozoviPorodicuAsync(porodica, "PonovnaUpotreba", Sada, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Tolerancija_ne_vazi_ako_je_naslednik_opozvan_odjavom()
    {
        var porodica = Guid.NewGuid();
        var handler = OsvezavanjeSaNaslednikom(porodica, out var naslednik, Sada.AddSeconds(-2));
        naslednik.Opozovi(Sada.AddSeconds(-1), "Odjava");

        var akcija = () => handler.Handle(new OsveziTokenCommand("STARI"), default);

        await akcija.Should().ThrowAsync<NeovlascenIzuzetak>();
        _uow.RefreshTokeni.Verify(r => r.OpozoviPorodicuAsync(porodica, "PonovnaUpotreba", Sada, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Registracija_sa_postojecim_emailom_vraca_konflikt()
    {
        _uow.Korisnici.Setup(k => k.PostojiEmailAsync("PERA@PRIMER.RS", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new RegistracijaHandler(_uow.Object, _hes.Object, Izdavanje(), LazniKorisnik.Za(null), _audit.Object, _vreme);

        var akcija = () => handler.Handle(new RegistracijaCommand("pera@primer.rs", "Lozinka123", "Pera", "Perić", null), default);

        await akcija.Should().ThrowAsync<KonfliktIzuzetak>();
        _uow.Korisnici.Verify(k => k.DodajAsync(It.IsAny<Korisnik>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Promena_lozinke_ponistava_sve_sesije()
    {
        var korisnik = NoviKorisnik(5);
        _uow.Korisnici.Setup(k => k.PronadjiPoIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(korisnik);
        _hes.Setup(h => h.Proveri("HES", "Stara12345")).Returns(true);
        _hes.Setup(h => h.Hesiraj("Nova12345")).Returns("NOVI-HES");
        var handler = new PromeniLozinkuHandler(_uow.Object, LazniKorisnik.Za(5), _hes.Object, _audit.Object, _vreme);

        await handler.Handle(new PromeniLozinkuCommand("Stara12345", "Nova12345"), default);

        korisnik.LozinkaHash.Should().Be("NOVI-HES");
        _uow.RefreshTokeni.Verify(r => r.OpozoviSveZaKorisnikaAsync(5, "PromenaLozinke", Sada, It.IsAny<CancellationToken>()), Times.Once);
    }
}
