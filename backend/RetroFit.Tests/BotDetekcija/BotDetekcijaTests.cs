using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RetroFit.API.Middleware.BotDetekcija;

namespace RetroFit.Tests.BotDetekcija;

public class AnalizatorRizikaTests
{
    private static readonly KlijentskiSignali Signali = new() { Ekran = "1920x1080", Zona = "Europe/Belgrade", Jezik = "sr-RS", Webdriver = false };
    private const string ChromeUa = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/130.0 Safari/537.36";

    private static UlazAnalize Pregledac(SazetakPonasanja? p = null) => new()
    {
        UserAgent = ChromeUa,
        AcceptLanguage = "sr-RS,sr;q=0.9",
        Accept = "application/json",
        Signali = Signali,
        Ponasanje = p ?? new SazetakPonasanja { ZahtevaUProzoru = 1, LimitZahteva = 100 }
    };

    [Fact]
    public void Obican_pregledac_ima_nulti_rizik()
    {
        var p = AnalizatorRizika.Proceni(Pregledac());
        p.Rizik.Should().Be(0);
        p.Razlozi.Should().BeEmpty();
    }

    [Fact]
    public void Skripta_bez_zaglavlja_ima_visok_rizik_sa_objasnjenjem()
    {
        var p = AnalizatorRizika.Proceni(new UlazAnalize { UserAgent = "python-requests/2.31" });

        p.Rizik.Should().Be(65); // 35 UA + 15 jezik + 5 accept + 10 bez signala
        p.Razlozi.Should().Contain(r => r.Contains("automatizaciju")).And.Contain(r => r.Contains("Accept-Language"));
    }

    [Fact]
    public void Webdriver_otkriva_automatizovan_pregledac()
    {
        var p = AnalizatorRizika.Proceni(Pregledac() with { Signali = Signali with { Webdriver = true } });
        p.Rizik.Should().Be(40);
    }

    [Fact]
    public void Prekoracen_limit_i_scraping_podizu_rizik()
    {
        var p = AnalizatorRizika.Proceni(Pregledac(new SazetakPonasanja
        {
            ZahtevaUProzoru = 130, LimitZahteva = 120, RazlicitihOglasa5Min = 80, SekvencijalnihPregleda = 10
        }));

        p.PrekoracenLimit.Should().BeTrue();
        p.Rizik.Should().Be(95); // 30 limit + 30 scraping + 35 enumeracija
    }

    [Fact]
    public void Ista_poruka_u_vise_razgovora_je_spam()
    {
        var p = AnalizatorRizika.Proceni(Pregledac(new SazetakPonasanja { ZahtevaUProzoru = 3, LimitZahteva = 20, IstaPorukaURazgovora = 3 }));
        p.Spam.Should().BeTrue();
    }

    [Fact]
    public void Resen_izazov_umanjuje_rizik_najvise_za_30()
    {
        var bez = AnalizatorRizika.Proceni(new UlazAnalize { UserAgent = "curl/8.0" });
        var sa = AnalizatorRizika.Proceni(new UlazAnalize { UserAgent = "curl/8.0", ValidanIzazov = true });
        (bez.Rizik - sa.Rizik).Should().Be(30);
    }

    [Fact]
    public void Rizik_je_uvek_izmedju_0_i_100()
    {
        var p = AnalizatorRizika.Proceni(new UlazAnalize
        {
            Signali = Signali with { Webdriver = true },
            TlsOtisakSumnjiv = true,
            Ponasanje = new SazetakPonasanja
            {
                ZahtevaUProzoru = 999, LimitZahteva = 1, RazlicitihOglasa5Min = 999, SekvencijalnihPregleda = 99,
                RobotskiRitam = true, IstaPorukaURazgovora = 9, Gresaka404U5Min = 99
            }
        });
        p.Rizik.Should().Be(100);
    }
}

public class OdlukaBotDetekcijeTests
{
    private readonly BotDetekcijaOpcije _o = new();

    private static ProcenaRizika Procena(int rizik, bool spam = false, bool limit = false) => new(rizik, [], spam, limit);

    [Fact]
    public void Registracija_uvek_trazi_izazov() =>
        BotDetekcijaMiddleware.Odluci(_o, GrupaRute.Registracija, Procena(0), validanIzazov: false).Should().Be(OdlukaBotDetekcije.TraziIzazov);

    [Fact]
    public void Registracija_sa_resenim_izazovom_prolazi() =>
        BotDetekcijaMiddleware.Odluci(_o, GrupaRute.Registracija, Procena(0), validanIzazov: true).Should().Be(OdlukaBotDetekcije.Propusti);

    [Theory]
    [InlineData(GrupaRute.Prijava, 60, OdlukaBotDetekcije.TraziIzazov)]
    [InlineData(GrupaRute.Prijava, 40, OdlukaBotDetekcije.Propusti)]
    [InlineData(GrupaRute.Pretraga, 60, OdlukaBotDetekcije.Propusti)] // pretraga nije osetljiva akcija
    [InlineData(GrupaRute.Pretraga, 90, OdlukaBotDetekcije.PrivremenoBlokiraj)]
    public void Odluka_zavisi_od_rizika_i_osetljivosti_rute(GrupaRute grupa, int rizik, OdlukaBotDetekcije ocekivano) =>
        BotDetekcijaMiddleware.Odluci(_o, grupa, Procena(rizik), false).Should().Be(ocekivano);

    [Fact]
    public void Prekoracen_limit_vraca_ogranicenje_brzine() =>
        BotDetekcijaMiddleware.Odluci(_o, GrupaRute.Pretraga, Procena(30, limit: true), false).Should().Be(OdlukaBotDetekcije.OgraniciBrzinu);

    [Fact]
    public void Spam_ima_prednost() =>
        BotDetekcijaMiddleware.Odluci(_o, GrupaRute.Poruke, Procena(40, spam: true), true).Should().Be(OdlukaBotDetekcije.Spam);
}

public class KlasifikacijaRutaTests
{
    [Theory]
    [InlineData("POST", "/api/v1/autentifikacija/registracija", GrupaRute.Registracija)]
    [InlineData("POST", "/api/v1/autentifikacija/prijava", GrupaRute.Prijava)]
    [InlineData("POST", "/api/v1/razgovori/5/poruke", GrupaRute.Poruke)]
    [InlineData("POST", "/api/v1/razgovori", GrupaRute.Poruke)]
    [InlineData("GET", "/api/v1/oglasi", GrupaRute.Pretraga)]
    [InlineData("GET", "/api/v1/oglasi/15", GrupaRute.PregledOglasa)]
    [InlineData("PUT", "/api/v1/oglasi/15", GrupaRute.Pisanje)]
    [InlineData("GET", "/api/v1/kategorije", GrupaRute.Opste)]
    [InlineData("GET", "/swagger/index.html", GrupaRute.Ignorisi)]
    [InlineData("GET", "/uploads/oglasi/1/a.jpg", GrupaRute.Ignorisi)]
    public void Rute_se_ispravno_klasifikuju(string metoda, string putanja, GrupaRute ocekivano) =>
        KlasifikacijaRuta.Klasifikuj(metoda, new PathString(putanja)).Grupa.Should().Be(ocekivano);

    [Fact]
    public void Pregled_oglasa_izdvaja_id() =>
        KlasifikacijaRuta.Klasifikuj("GET", new PathString("/api/v1/oglasi/42")).OglasId.Should().Be(42);
}

public class PracenjePonasanjaTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Sekvencijalno_otvaranje_oglasa_se_prepoznaje()
    {
        PracenjePonasanja.Sekvencijalnih([5, 6, 7, 8, 9, 10]).Should().Be(5);
        PracenjePonasanja.Sekvencijalnih([5, 19, 3, 44, 8]).Should().Be(0);
    }

    [Fact]
    public void Savrseno_ravnomeran_ritam_je_robotski_a_ljudski_nije()
    {
        var robot = Enumerable.Range(0, 20).Select(i => T0.AddMilliseconds(i * 1000)).ToList();
        var covek = new[] { 0, 800, 3100, 3500, 9000, 9400, 15000, 15300, 21000, 22500, 23000, 30000, 31000, 39000, 40000, 47000 }
            .Select(ms => T0.AddMilliseconds(ms)).ToList();

        PracenjePonasanja.RobotskiRitam(robot).Should().BeTrue();
        PracenjePonasanja.RobotskiRitam(covek).Should().BeFalse();
    }

    [Fact]
    public void Klizni_prozor_broji_samo_zahteve_unutar_prozora()
    {
        using var p = new PracenjePonasanja();
        var limit = new LimitGrupe { Zahteva = 3, ProzorSekundi = 60 };
        for (var i = 0; i < 4; i++)
        {
            p.Zabelezi("k:1", GrupaRute.Pretraga, limit, null, T0.AddSeconds(i));
        }

        p.Zabelezi("k:1", GrupaRute.Pretraga, limit, null, T0.AddSeconds(4)).PrekoracenLimit.Should().BeTrue();
        p.Zabelezi("k:1", GrupaRute.Pretraga, limit, null, T0.AddSeconds(120)).ZahtevaUProzoru.Should().Be(1, "stari zahtevi su ispali iz prozora");
    }

    [Fact]
    public void Ista_poruka_se_broji_po_razlicitim_razgovorima_bez_obzira_na_razmake_i_velika_slova()
    {
        using var p = new PracenjePonasanja();
        p.ZabeleziPoruku("k:1", "Kupujem ODMAH, javite se!", 1, T0).Should().Be(1);
        p.ZabeleziPoruku("k:1", "kupujem odmah javite se", 1, T0).Should().Be(1, "isti razgovor");
        p.ZabeleziPoruku("k:1", "kupujem odmah, javite se", 2, T0).Should().Be(2);
    }

    [Fact]
    public void Privremena_blokada_traje_zadato_vreme()
    {
        using var p = new PracenjePonasanja();
        p.BlokirajPrivremeno("k:1", T0, TimeSpan.FromMinutes(15)).Should().Be(1);
        p.PrivremenoBlokiranDo("k:1", T0.AddMinutes(10)).Should().NotBeNull();
        p.PrivremenoBlokiranDo("k:1", T0.AddMinutes(16)).Should().BeNull();
    }
}

public class JsIzazovTests
{
    private static JsIzazovServis Servis(int tezina = 8) => new(
        Options.Create(new BotDetekcijaOpcije { TajniKljuc = new string('k', 40), TezinaIzazova = tezina }),
        new MemoryCache(new MemoryCacheOptions()));

    private static readonly DateTime Sada = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    private static long Resi(JsIzazovDto iz)
    {
        for (long x = 0; ; x++)
        {
            if (JsIzazovServis.ImaVodeceNule(SHA256.HashData(Encoding.UTF8.GetBytes($"{iz.Nonce}:{x}")), iz.Tezina))
            {
                return x;
            }
        }
    }

    [Fact]
    public void Ispravno_resenje_daje_token_vezan_za_otisak()
    {
        var s = Servis();
        var iz = s.Izdaj(Sada);
        var token = s.Proveri(new ResenjeIzazovaDto(iz.Nonce, iz.Tezina, iz.IsticeUnix, iz.Potpis, Resi(iz)), "otisak-A", Sada);

        token.Should().NotBeNull();
        s.ValidanToken(token!.Token, "otisak-A", Sada).Should().BeTrue();
        s.ValidanToken(token.Token, "otisak-B", Sada).Should().BeFalse("token se ne može preneti na drugi uređaj");
        s.ValidanToken(token.Token, "otisak-A", Sada.AddHours(1)).Should().BeFalse("token je istekao");
    }

    [Fact]
    public void Isti_izazov_ne_moze_da_se_iskoristi_dva_puta()
    {
        var s = Servis();
        var iz = s.Izdaj(Sada);
        var resenje = new ResenjeIzazovaDto(iz.Nonce, iz.Tezina, iz.IsticeUnix, iz.Potpis, Resi(iz));

        s.Proveri(resenje, "o", Sada).Should().NotBeNull();
        s.Proveri(resenje, "o", Sada).Should().BeNull();
    }

    [Fact]
    public void Lazni_potpis_smanjena_tezina_i_istekao_izazov_se_odbijaju()
    {
        var s = Servis();
        var iz = s.Izdaj(Sada);
        var x = Resi(iz);

        s.Proveri(new ResenjeIzazovaDto(iz.Nonce, iz.Tezina, iz.IsticeUnix, "lazni-potpis", x), "o", Sada).Should().BeNull();
        s.Proveri(new ResenjeIzazovaDto(iz.Nonce, 1, iz.IsticeUnix, iz.Potpis, x), "o", Sada).Should().BeNull();
        s.Proveri(new ResenjeIzazovaDto(iz.Nonce, iz.Tezina, iz.IsticeUnix, iz.Potpis, x), "o", Sada.AddMinutes(5)).Should().BeNull();
    }

    [Fact]
    public void Izmenjen_token_nije_validan()
    {
        var s = Servis();
        s.ValidanToken("abc.def", "o", Sada).Should().BeFalse();
        s.ValidanToken(null, "o", Sada).Should().BeFalse();
    }

    [Theory]
    [InlineData(new byte[] { 0x00, 0x0F }, 12, true)]
    [InlineData(new byte[] { 0x00, 0x1F }, 12, false)]
    [InlineData(new byte[] { 0x00, 0x00 }, 16, true)]
    public void Brojanje_vodecih_nultih_bitova(byte[] hes, int bitova, bool ocekivano) =>
        JsIzazovServis.ImaVodeceNule(hes, bitova).Should().Be(ocekivano);
}

public class IdentifikacijaKlijentaTests
{
    private const string Kljuc = "tajni-kljuc-za-testove-dovoljno-dug";

    [Fact]
    public void Otisak_je_stabilan_za_iste_signale_a_razlikuje_se_za_razlicite()
    {
        var s = new KlijentskiSignali { Ekran = "1920x1080", Zona = "Europe/Belgrade" };
        var a = IdentifikacijaKlijenta.Otisak(Kljuc, "Mozilla Chrome/1", "sr", s, null);

        IdentifikacijaKlijenta.Otisak(Kljuc, "Mozilla Chrome/1", "sr", s, null).Should().Be(a);
        IdentifikacijaKlijenta.Otisak(Kljuc, "Mozilla Chrome/1", "sr", s with { Ekran = "390x844" }, null).Should().NotBe(a);
        IdentifikacijaKlijenta.Otisak("drugi-kljuc-za-testove-dovoljno-dug", "Mozilla Chrome/1", "sr", s, null).Should().NotBe(a,
            "bez tajnog ključa otisak se ne može izračunati");
        a.Should().HaveLength(64);
    }

    [Theory]
    [InlineData("192.168.1.77", "192.168.1.0")]
    [InlineData("2001:db8:85a3:1234::8a2e:370:7334", "2001:db8:85a3::")]
    public void IP_adresa_se_maskira_u_logovima(string ip, string ocekivano) =>
        IdentifikacijaKlijenta.MaskirajIp(IPAddress.Parse(ip)).Should().Be(ocekivano);

    [Fact]
    public void Opis_klijenta_je_gruba_porodica_a_ne_pun_user_agent() =>
        IdentifikacijaKlijenta.OpisKlijenta("Mozilla/5.0 (Linux; Android 14) AppleWebKit Chrome/130 Mobile Safari/537")
            .Should().Be("Chrome / Android");

    [Fact]
    public void Predugacki_ili_neispravni_signali_se_odbacuju()
    {
        KlijentskiSignali.Parsiraj(new string('x', 600)).Should().BeNull();
        KlijentskiSignali.Parsiraj("{nije json").Should().BeNull();
        KlijentskiSignali.Parsiraj("""{"ekran":"1366x768","webdriver":true}""")!.Webdriver.Should().BeTrue();
    }
}
