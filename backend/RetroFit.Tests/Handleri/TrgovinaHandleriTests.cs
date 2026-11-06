using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Application.Features.Moderacija;
using RetroFit.Application.Features.Odrzavanje;
using RetroFit.Application.Features.Oglasi;
using RetroFit.Application.Features.Trgovina;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;
using RetroFit.Tests.Pomocne;
using Microsoft.Extensions.Logging.Abstractions;
using static RetroFit.Tests.Pomocne.TestPodaci;

namespace RetroFit.Tests.Handleri;

public class TrgovinaHandleriTests
{
    private readonly LazniUnitOfWork _uow = new();
    private readonly Mock<IObavestenjaServis> _obavestenja = new();
    private readonly FiksnoVreme _vreme = new(Sada);

    [Fact]
    public async Task Prihvatanje_ponude_pravi_rezervaciju_i_odbija_ostale_ponude()
    {
        var oglas = AktivanOglas();
        var razgovor = NoviRazgovor(oglas);
        var ponuda = PonudaNaCekanju(oglas, razgovor);
        TestPodaci.PostaviSvojstvo(ponuda, nameof(Ponuda.Oglas), oglas);
        var drugiRazgovor = NoviRazgovor(oglas, DrugiKupacId, 101);
        var druga = PonudaNaCekanju(oglas, drugiRazgovor, 8_500, 201);

        _uow.Ponude.Setup(p => p.PronadjiSaRazgovoromAsync(200, It.IsAny<CancellationToken>())).ReturnsAsync(ponuda);
        _uow.Ponude.Setup(p => p.NaCekanjuZaOglasAsync(oglas.Id, It.IsAny<CancellationToken>())).ReturnsAsync([ponuda, druga]);
        Rezervacija? nova = null;
        _uow.Rezervacije.Setup(r => r.DodajAsync(It.IsAny<Rezervacija>(), It.IsAny<CancellationToken>()))
            .Callback((Rezervacija r, CancellationToken _) => nova = r);
        _uow.Rezervacije.Setup(r => r.DtoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new RezervacijaDto());

        var handler = new PrihvatiPonuduHandler(_uow.Object, LazniKorisnik.Za(ProdavacId), _obavestenja.Object, _vreme);
        await handler.Handle(new PrihvatiPonuduCommand(200), default);

        ponuda.Status.Should().Be(StatusPonude.Prihvacena);
        druga.Status.Should().Be(StatusPonude.Odbijena);
        oglas.Status.Should().Be(StatusOglasa.Rezervisan);
        nova!.KupacId.Should().Be(KupacId);
        nova.DogovorenaCena.Should().Be(9_000);
        _obavestenja.Verify(o => o.ObavestiAsync(DrugiKupacId, TipObavestenja.PonudaOdbijena, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _obavestenja.Verify(o => o.ObavestiAsync(KupacId, TipObavestenja.PonudaPrihvacena, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ponudu_iz_tudjeg_razgovora_korisnik_ne_vidi()
    {
        var oglas = AktivanOglas();
        var ponuda = PonudaNaCekanju(oglas, NoviRazgovor(oglas));
        _uow.Ponude.Setup(p => p.PronadjiSaRazgovoromAsync(200, It.IsAny<CancellationToken>())).ReturnsAsync(ponuda);
        var handler = new PrihvatiPonuduHandler(_uow.Object, LazniKorisnik.Za(DrugiKupacId), _obavestenja.Object, _vreme);

        var akcija = () => handler.Handle(new PrihvatiPonuduCommand(200), default);

        await akcija.Should().ThrowAsync<NijePronadjenoIzuzetak>();
    }

    [Fact]
    public async Task Druga_ponuda_dok_prva_ceka_odgovor_vraca_konflikt()
    {
        var oglas = AktivanOglas();
        var razgovor = NoviRazgovor(oglas);
        _uow.Oglasi.Setup(o => o.PronadjiPoIdAsync(oglas.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oglas);
        _uow.Razgovori.Setup(r => r.PronadjiZaOglasIKupcaAsync(oglas.Id, KupacId, It.IsAny<CancellationToken>())).ReturnsAsync(razgovor);
        _uow.Ponude.Setup(p => p.NaCekanjuURazgovoruAsync(razgovor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(PonudaNaCekanju(oglas, razgovor));
        var handler = new DajPonuduHandler(_uow.Object, LazniKorisnik.Za(KupacId), _obavestenja.Object, _vreme);

        var akcija = () => handler.Handle(new DajPonuduCommand(oglas.Id, 9_500, null), default);

        await akcija.Should().ThrowAsync<KonfliktIzuzetak>();
    }

    [Fact]
    public async Task Dupla_recenzija_vraca_konflikt_a_prva_atomicno_azurira_prosek()
    {
        var t = ZavrsenaTransakcija();
        _uow.Transakcije.Setup(x => x.PronadjiSaOglasomAsync(t.Id, It.IsAny<CancellationToken>())).ReturnsAsync(t);
        _uow.Recenzije.SetupSequence(r => r.PostojiAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Recenzija, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        _uow.Recenzije.Setup(r => r.PronadjiPoIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Recenzija.Kreiraj(t, KupacId, 5, null, Sada.AddHours(4)));
        _uow.Recenzije.Setup(r => r.ZaKorisnikaAsync(It.IsAny<int>(), It.IsAny<Application.Common.Modeli.Stranicenje>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Application.Common.Modeli.StranicaRezultata<RecenzijaDto>([new RecenzijaDto { Id = 0 }], 1, 50, 1));
        var handler = new OstaviRecenzijuHandler(_uow.Object, LazniKorisnik.Za(KupacId), _obavestenja.Object, new FiksnoVreme(Sada.AddHours(4)));

        await handler.Handle(new OstaviRecenzijuCommand(t.Id, 5, "Super"), default);
        var druga = () => handler.Handle(new OstaviRecenzijuCommand(t.Id, 1, "Ipak ne"), default);

        await druga.Should().ThrowAsync<KonfliktIzuzetak>();
        _uow.Korisnici.Verify(k => k.DodajOcenuAsync(ProdavacId, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Zavrsetak_transakcije_isplacuje_escrow_i_uvecava_brojace()
    {
        var t = TransakcijaUToku(escrow: true);
        t.EvidentirajUplatu(KupacId);
        t.Potvrdi(ProdavacId, Sada.AddHours(2));
        _uow.Transakcije.Setup(x => x.PronadjiSaOglasomAsync(t.Id, It.IsAny<CancellationToken>())).ReturnsAsync(t);
        _uow.Transakcije.Setup(x => x.DetaljiAsync(t.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new TransakcijaDto { KupacId = KupacId, ProdavacId = ProdavacId });
        var placanja = new Mock<Application.Common.Interfejsi.Persistencija.IRepository<Placanje>>();
        _uow.Uow.SetupGet(u => u.Placanja).Returns(placanja.Object);
        var procesor = new Mock<IPlatniProcesor>();
        procesor.Setup(p => p.IsplatiAsync(ProdavacId, 10_000, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RezultatPlacanja(true, "MOCK-ISP-1", null));
        var handler = new PotvrdiTransakcijuHandler(_uow.Object, LazniKorisnik.Za(KupacId), procesor.Object, _obavestenja.Object, _vreme);

        await handler.Handle(new PotvrdiTransakcijuCommand(t.Id), default);

        t.Status.Should().Be(StatusTransakcije.Zavrsena);
        placanja.Verify(p => p.DodajAsync(It.Is<Placanje>(x => x.Tip == TipPlacanja.EscrowIsplata && x.KorisnikId == ProdavacId),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Korisnici.Verify(k => k.UvecajZavrseneTransakcijeAsync(
            It.Is<IReadOnlyCollection<int>>(l => l.Contains(KupacId) && l.Contains(ProdavacId)), It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class OstaliHandleriTests
{
    private readonly LazniUnitOfWork _uow = new();
    private readonly FiksnoVreme _vreme = new(Sada);

    private static OglasDetaljiDto Detalji(StatusOglasa status = StatusOglasa.Aktivan) => new()
    {
        Id = 1,
        Status = status,
        Prodavac = new Application.Features.Korisnici.JavniProfilDto { Id = ProdavacId }
    };

    [Fact]
    public async Task Telefon_prodavca_se_ne_prikazuje_bez_rezervacije()
    {
        _uow.Oglasi.Setup(o => o.DetaljiAsync(1, Sada, It.IsAny<CancellationToken>())).ReturnsAsync(Detalji());
        _uow.Rezervacije.Setup(r => r.KupacImaPravoNaKontaktAsync(1, KupacId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dto = await new OglasDetaljiHandler(_uow.Object, LazniKorisnik.Za(KupacId), _vreme).Handle(new OglasDetaljiQuery(1), default);

        dto.TelefonProdavca.Should().BeNull();
        _uow.Korisnici.Verify(k => k.PronadjiPoIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Telefon_prodavca_vidi_kupac_sa_rezervacijom_ako_je_prodavac_dozvolio()
    {
        var prodavac = NoviKorisnik(ProdavacId);
        prodavac.PostaviTelefon("+381641234567");
        prodavac.PotvrdiTelefon("+381641234567", Sada);
        _uow.Oglasi.Setup(o => o.DetaljiAsync(1, Sada, It.IsAny<CancellationToken>())).ReturnsAsync(Detalji(StatusOglasa.Rezervisan));
        _uow.Rezervacije.Setup(r => r.KupacImaPravoNaKontaktAsync(1, KupacId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _uow.Korisnici.Setup(k => k.PronadjiPoIdAsync(ProdavacId, It.IsAny<CancellationToken>())).ReturnsAsync(prodavac);

        var dto = await new OglasDetaljiHandler(_uow.Object, LazniKorisnik.Za(KupacId), _vreme).Handle(new OglasDetaljiQuery(1), default);
        dto.TelefonProdavca.Should().Be("+381641234567");

        prodavac.AzurirajProfil("Petar", "Petrović", null, 1, prikaziTelefonKupcu: false);
        dto = await new OglasDetaljiHandler(_uow.Object, LazniKorisnik.Za(KupacId), _vreme).Handle(new OglasDetaljiQuery(1), default);
        dto.TelefonProdavca.Should().BeNull("prodavac je isključio prikaz telefona");
    }

    [Fact]
    public async Task Nacrt_tudjeg_oglasa_se_ponasa_kao_da_ne_postoji()
    {
        _uow.Oglasi.Setup(o => o.DetaljiAsync(1, Sada, It.IsAny<CancellationToken>())).ReturnsAsync(Detalji(StatusOglasa.Nacrt));
        var akcija = () => new OglasDetaljiHandler(_uow.Object, LazniKorisnik.Za(KupacId), _vreme).Handle(new OglasDetaljiQuery(1), default);
        await akcija.Should().ThrowAsync<NijePronadjenoIzuzetak>();

        var zaModeratora = await new OglasDetaljiHandler(_uow.Object, LazniKorisnik.Za(2, NaziviUloga.Moderator), _vreme)
            .Handle(new OglasDetaljiQuery(1), default);
        zaModeratora.Status.Should().Be(StatusOglasa.Nacrt);
    }

    [Fact]
    public async Task Moderator_ne_moze_blokirati_moderatora_a_admina_niko()
    {
        var audit = new Mock<IAuditServis>();
        var obavestenja = new Mock<IObavestenjaServis>();
        _uow.Korisnici.Setup(k => k.UlogeKorisnikaAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(["Korisnik", "Moderator"]);
        _uow.Korisnici.Setup(k => k.UlogeKorisnikaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(["Korisnik", "Admin"]);

        var kaoModerator = new BlokirajKorisnikaHandler(_uow.Object, LazniKorisnik.Za(2, NaziviUloga.Moderator), obavestenja.Object, audit.Object, _vreme);
        await kaoModerator.Invoking(h => h.Handle(new BlokirajKorisnikaCommand(7, "razlog", null), default)).Should().ThrowAsync<ZabranjenoIzuzetak>();

        var kaoAdmin = new BlokirajKorisnikaHandler(_uow.Object, LazniKorisnik.Za(3, NaziviUloga.Admin), obavestenja.Object, audit.Object, _vreme);
        await kaoAdmin.Invoking(h => h.Handle(new BlokirajKorisnikaCommand(1, "razlog", null), default)).Should().ThrowAsync<ZabranjenoIzuzetak>();
        await kaoAdmin.Handle(new BlokirajKorisnikaCommand(7, "Zloupotreba", 7), default);

        _uow.Blokade.Verify(b => b.DodajAsync(It.Is<BlokadaKorisnika>(x => x.KorisnikId == 7 && x.KrajUtc == Sada.AddDays(7)),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.RefreshTokeni.Verify(r => r.OpozoviSveZaKorisnikaAsync(7, "NalogBlokiran", Sada, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pozadinski_istek_menja_status_i_obavestava_prodavca()
    {
        var oglas = AktivanOglas();
        var kasnije = Sada.AddDays(61);
        _uow.Oglasi.Setup(o => o.ZaIstekAsync(kasnije, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([oglas]);
        _uow.Ponude.Setup(p => p.NaCekanjuZaOglasAsync(oglas.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var obavestenja = new Mock<IObavestenjaServis>();
        var handler = new IstekniOglaseHandler(_uow.Object, obavestenja.Object, new FiksnoVreme(kasnije), NullLogger<IstekniOglaseHandler>.Instance);

        var broj = await handler.Handle(new IstekniOglaseCommand(), default);

        broj.Should().Be(1);
        oglas.Status.Should().Be(StatusOglasa.Istekao);
        obavestenja.Verify(o => o.ObavestiAsync(ProdavacId, TipObavestenja.OglasIstekao, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Uow.Verify(u => u.SacuvajAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Tudji_oglas_ne_moze_da_se_menja()
    {
        var oglas = AktivanOglas();
        _uow.Oglasi.Setup(o => o.PronadjiZaIzmenuAsync(oglas.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oglas);
        var handler = new ObrisiOglasHandler(_uow.Object, LazniKorisnik.Za(KupacId), _vreme);

        await handler.Invoking(h => h.Handle(new ObrisiOglasCommand(oglas.Id), default)).Should().ThrowAsync<ZabranjenoIzuzetak>();
        oglas.Obrisan.Should().BeFalse();
    }

    [Fact]
    public void Stablo_kategorija_sabira_oglase_podkategorija()
    {
        var ravno = new List<Application.Features.Katalog.KategorijaDto>
        {
            new() { Id = 1, Naziv = "Elektronika", BrojAktivnihOglasa = 1 },
            new() { Id = 11, Naziv = "Telefoni", RoditeljId = 1, BrojAktivnihOglasa = 3 },
            new() { Id = 12, Naziv = "Računari", RoditeljId = 1, BrojAktivnihOglasa = 2 },
            new() { Id = 2, Naziv = "Vozila", BrojAktivnihOglasa = 0 }
        };

        var stablo = Application.Features.Katalog.KategorijeHandler.NapraviStablo(ravno);

        stablo.Should().HaveCount(2);
        stablo.First(k => k.Id == 1).Podkategorije.Should().HaveCount(2);
        stablo.First(k => k.Id == 1).BrojAktivnihOglasa.Should().Be(6);
    }
}
