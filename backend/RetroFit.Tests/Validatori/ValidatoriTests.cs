using FluentValidation.TestHelper;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Common.Validacija;
using RetroFit.Application.Features.Autentifikacija;
using RetroFit.Application.Features.Korisnici;
using RetroFit.Application.Features.Oglasi;
using RetroFit.Application.Features.Trgovina;
using RetroFit.Domain.Enumi;

namespace RetroFit.Tests.Validatori;

public class RegistracijaValidatorTests
{
    private readonly RegistracijaValidator _v = new();

    [Fact]
    public void Ispravna_registracija_prolazi()
    {
        _v.TestValidate(new RegistracijaCommand("pera@primer.rs", "Lozinka123", "Pera", "Perić", 1)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("kratka1")]
    [InlineData("samoslovabezcifara")]
    [InlineData("12345678901")]
    public void Slaba_lozinka_se_odbija(string lozinka)
    {
        _v.TestValidate(new RegistracijaCommand("pera@primer.rs", lozinka, "Pera", "Perić", null)).ShouldHaveValidationErrorFor(x => x.Lozinka);
    }

    [Theory]
    [InlineData("nije-email")]
    [InlineData("")]
    public void Neispravan_email_se_odbija(string email)
    {
        _v.TestValidate(new RegistracijaCommand(email, "Lozinka123", "Pera", "Perić", null)).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Ime_sa_ciframa_se_odbija()
    {
        _v.TestValidate(new RegistracijaCommand("pera@primer.rs", "Lozinka123", "Pera123", "Perić", null)).ShouldHaveValidationErrorFor(x => x.Ime);
    }
}

public class KreirajOglasValidatorTests
{
    private readonly KreirajOglasValidator _v = new();

    private static KreirajOglasCommand Komanda(TipOglasa tip = TipOglasa.Prodaja, decimal? cena = 1000, bool dostava = true, bool licno = true,
        string naslov = "Bicikl Capriolo") =>
        new(23, 1, naslov, "Gradski bicikl u dobrom stanju.", tip, cena, StanjeProizvoda.Polovno, dostava, licno, null, null);

    [Fact]
    public void Ispravan_oglas_prolazi() => _v.TestValidate(Komanda()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Prodaja_bez_cene_se_odbija() => _v.TestValidate(Komanda(cena: null)).ShouldHaveValidationErrorFor(x => x.Cena);

    [Fact]
    public void Cena_sa_vise_od_dve_decimale_se_odbija() => _v.TestValidate(Komanda(cena: 10.555m)).ShouldHaveValidationErrorFor(x => x.Cena);

    [Fact]
    public void Poklon_sa_cenom_se_odbija() => _v.TestValidate(Komanda(TipOglasa.Poklon, 500)).ShouldHaveValidationErrorFor(x => x.Cena);

    [Fact]
    public void Poklon_bez_cene_prolazi() => _v.TestValidate(Komanda(TipOglasa.Poklon, null)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Bez_nacina_preuzimanja_se_odbija() =>
        _v.TestValidate(Komanda(dostava: false, licno: false)).Errors.Should().Contain(e => e.ErrorMessage.Contains("preuzimanje"));

    [Fact]
    public void Prekratak_naslov_se_odbija() => _v.TestValidate(Komanda(naslov: "abc")).ShouldHaveValidationErrorFor(x => x.Naslov);
}

public class OstaliValidatoriTests
{
    [Fact]
    public void Pretraga_sa_cenom_od_vecom_od_cene_do_se_odbija()
    {
        var v = new PretragaOglasaValidator();
        v.TestValidate(new PretragaOglasaQuery(new KriterijumiPretrage { CenaOd = 500, CenaDo = 100 })).IsValid.Should().BeFalse();
        v.TestValidate(new PretragaOglasaQuery(new KriterijumiPretrage { CenaOd = 100, CenaDo = 500 })).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Sortiranje_po_udaljenosti_zahteva_lokaciju()
    {
        var v = new PretragaOglasaValidator();
        v.TestValidate(new PretragaOglasaQuery(new KriterijumiPretrage { Sortiranje = SortiranjeOglasa.Udaljenost })).IsValid.Should().BeFalse();
        v.TestValidate(new PretragaOglasaQuery(new KriterijumiPretrage { Sortiranje = SortiranjeOglasa.Udaljenost, GradId = 1 }))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Samo_sirina_bez_duzine_nije_ispravna_lokacija()
    {
        new PretragaOglasaValidator().TestValidate(new PretragaOglasaQuery(new KriterijumiPretrage { GeografskaSirina = 44.8 }))
            .IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void Ocena_recenzije_mora_biti_1_do_5(int ocena, bool ispravno) =>
        new OstaviRecenzijuValidator().TestValidate(new OstaviRecenzijuCommand(1, ocena, null)).IsValid.Should().Be(ispravno);

    [Fact]
    public void Nova_lozinka_ne_sme_biti_ista_kao_stara() =>
        new PromeniLozinkuValidator().TestValidate(new PromeniLozinkuCommand("Lozinka123", "Lozinka123"))
            .ShouldHaveValidationErrorFor(x => x.NovaLozinka);

    [Fact]
    public void Verifikacioni_kod_mora_imati_6_cifara()
    {
        var v = new PotvrdiTelefonValidator();
        v.TestValidate(new PotvrdiTelefonCommand("12345")).IsValid.Should().BeFalse();
        v.TestValidate(new PotvrdiTelefonCommand("12a456")).IsValid.Should().BeFalse();
        v.TestValidate(new PotvrdiTelefonCommand("123456")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Slike_drugog_tipa_se_odbijaju()
    {
        var v = new DodajSlikeValidator();
        var fajl = new OtpremljenFajl(Stream.Null, "virus.exe", "application/octet-stream", 100);
        v.TestValidate(new DodajSlikeCommand(1, [fajl])).IsValid.Should().BeFalse();
        v.TestValidate(new DodajSlikeCommand(1, [])).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Kupovina_dostavom_zahteva_adresu() =>
        new ZapocniKupovinuValidator().TestValidate(new ZapocniKupovinuCommand(1, NacinPreuzimanja.Dostava, null, false))
            .ShouldHaveValidationErrorFor(x => x.AdresaDostave);

    [Fact]
    public void Stranicenje_ima_sigurne_granice()
    {
        var s = new Stranicenje { Stranica = -5, VelicinaStranice = 100_000 };
        s.Stranica.Should().Be(1);
        s.VelicinaStranice.Should().Be(Stranicenje.MaksVelicina);
    }

    [Theory]
    [InlineData("064 123 4567", "+381641234567")]
    [InlineData("+381 65 234 5678", "+381652345678")]
    [InlineData("00381631234567", "+381631234567")]
    [InlineData("011 123 4567", null)]
    [InlineData("abc", null)]
    public void Broj_telefona_se_normalizuje_u_E164(string unos, string? ocekivano) =>
        BrojTelefona.Normalizuj(unos).Should().Be(ocekivano);
}
