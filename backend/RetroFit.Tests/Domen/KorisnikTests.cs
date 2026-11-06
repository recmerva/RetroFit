using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;
using static RetroFit.Tests.Pomocne.TestPodaci;

namespace RetroFit.Tests.Domen;

public class KorisnikTests
{
    [Fact]
    public void Email_se_normalizuje_za_jedinstvenost_bez_obzira_na_velika_slova()
    {
        var k = new Korisnik("  Pera@Primer.RS ", "Pera", "Perić", null);
        k.Email.Should().Be("Pera@Primer.RS");
        k.NormalizovanEmail.Should().Be("PERA@PRIMER.RS");
    }

    [Fact]
    public void Prosecna_ocena_se_racuna_inkrementalno()
    {
        var k = NoviKorisnik(1);
        k.DodajOcenu(5);
        k.DodajOcenu(4);
        k.DodajOcenu(3);

        k.BrojRecenzija.Should().Be(3);
        k.ProsecnaOcena.Should().Be(4.00m);
    }

    [Fact]
    public void Jedna_petica_ne_daje_maksimalnu_reputaciju()
    {
        ReputacijaPravila.Izracunaj(5, 1, 0, false).Should().BeLessThan(60, "Bajesov prosek 'vuče' malo ocena ka 3.0");
        ReputacijaPravila.Izracunaj(5, 50, 20, true).Should().BeGreaterThan(95);
    }

    [Fact]
    public void Promena_telefona_ponistava_verifikaciju()
    {
        var k = NoviKorisnik(1);
        k.PostaviTelefon("+381641111111");
        k.PotvrdiTelefon("+381641111111", Sada);
        k.TelefonVerifikovan.Should().BeTrue();

        k.PostaviTelefon("+381642222222");

        k.TelefonVerifikovan.Should().BeFalse();
    }

    [Fact]
    public void Brisanje_naloga_anonimizuje_licne_podatke()
    {
        var k = NoviKorisnik(7);
        k.PostaviTelefon("+381641111111");

        k.Obrisi(Sada);

        k.Obrisan.Should().BeTrue();
        k.Telefon.Should().BeNull();
        k.Ime.Should().Be("Obrisan");
        k.Email.Should().StartWith("obrisan-7-");
        k.PrikaznoIme.Should().Be("Obrisan korisnik");
    }

    [Fact]
    public void Prikazno_ime_ne_otkriva_puno_prezime()
    {
        NoviKorisnik(1).PrikaznoIme.Should().Be("Petar P.");
    }
}

public class VerifikacijaTests
{
    [Fact]
    public void Posle_pet_pogresnih_pokusaja_kod_vise_ne_vazi()
    {
        var v = new VerifikacijaKorisnika(1, TipVerifikacije.Telefon, "+381641111111", "ISPRAVAN", Sada);
        for (var i = 0; i < PoslovnaPravila.MaksPokusajaVerifikacije; i++)
        {
            v.Proveri("POGRESAN", Sada).Should().BeFalse();
        }

        v.Invoking(x => x.Proveri("ISPRAVAN", Sada)).Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("verifikacija_istekla");
    }

    [Fact]
    public void Kod_istice_posle_10_minuta()
    {
        var v = new VerifikacijaKorisnika(1, TipVerifikacije.Telefon, "+381641111111", "ISPRAVAN", Sada);
        v.MozeSeKoristiti(Sada.AddMinutes(9)).Should().BeTrue();
        v.MozeSeKoristiti(Sada.AddMinutes(11)).Should().BeFalse();
    }

    [Fact]
    public void Ispravan_kod_se_moze_iskoristiti_samo_jednom()
    {
        var v = new VerifikacijaKorisnika(1, TipVerifikacije.Telefon, "+381641111111", "ISPRAVAN", Sada);
        v.Proveri("ISPRAVAN", Sada).Should().BeTrue();
        v.MozeSeKoristiti(Sada).Should().BeFalse();
    }
}

public class RefreshTokenTests
{
    [Fact]
    public void Opozvan_token_nije_aktivan_i_pamti_naslednika()
    {
        var t = new RefreshToken(1, new string('a', 64), Guid.NewGuid(), Sada, Sada.AddDays(14), "127.0.0.1");
        t.JeAktivan(Sada).Should().BeTrue();

        t.Opozovi(Sada, "Rotacija", new string('b', 64));

        t.JeAktivan(Sada).Should().BeFalse();
        t.ZamenjenHashom.Should().Be(new string('b', 64));
    }

    [Fact]
    public void Istekao_token_nije_aktivan()
    {
        var t = new RefreshToken(1, new string('a', 64), Guid.NewGuid(), Sada, Sada.AddDays(14), null);
        t.JeAktivan(Sada.AddDays(15)).Should().BeFalse();
    }

    [Fact]
    public void Blokada_sa_krajem_u_proslosti_nije_dozvoljena_a_ukinuta_nije_aktivna()
    {
        var akcija = () => new BlokadaKorisnika(1, 2, "razlog", Sada, Sada.AddDays(-1));
        akcija.Should().Throw<DomenskiIzuzetak>();

        var b = new BlokadaKorisnika(1, 2, "razlog", Sada, null);
        b.JeAktivna(Sada.AddYears(5)).Should().BeTrue("trajna blokada");
        b.Ukini(2, Sada);
        b.JeAktivna(Sada).Should().BeFalse();
    }
}
