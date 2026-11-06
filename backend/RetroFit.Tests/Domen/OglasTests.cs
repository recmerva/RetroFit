using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;
using static RetroFit.Tests.Pomocne.TestPodaci;

namespace RetroFit.Tests.Domen;

public class OglasTests
{
    [Fact]
    public void Objava_postavlja_status_Aktivan_i_rok_od_60_dana()
    {
        var oglas = AktivanOglas();

        oglas.Status.Should().Be(StatusOglasa.Aktivan);
        oglas.ObjavljenoUtc.Should().Be(Sada);
        oglas.IsticeUtc.Should().Be(Sada.AddDays(PoslovnaPravila.TrajanjeOglasaDana));
    }

    [Fact]
    public void Prodaja_bez_cene_nije_dozvoljena()
    {
        var akcija = () => Oglas.Kreiraj(1, 11, 1, "Naslov oglasa", "Opis oglasa ovde", TipOglasa.Prodaja, null,
            StanjeProizvoda.Novo, true, true, null, true, Sada);

        akcija.Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("validacija");
    }

    [Theory]
    [InlineData(TipOglasa.Poklon)]
    [InlineData(TipOglasa.Razmena)]
    public void Poklon_i_razmena_ne_smeju_imati_cenu(TipOglasa tip)
    {
        var akcija = () => Oglas.Kreiraj(1, 11, 1, "Naslov oglasa", "Opis oglasa ovde", tip, 500, StanjeProizvoda.Novo, true, true, null, true, Sada);
        akcija.Should().Throw<DomenskiIzuzetak>();
    }

    [Fact]
    public void Mora_postojati_bar_jedan_nacin_preuzimanja()
    {
        var akcija = () => Oglas.Kreiraj(1, 11, 1, "Naslov oglasa", "Opis oglasa ovde", TipOglasa.Prodaja, 100, StanjeProizvoda.Novo,
            dostavaMoguca: false, licnoPreuzimanjeMoguce: false, null, true, Sada);
        akcija.Should().Throw<DomenskiIzuzetak>();
    }

    [Fact]
    public void Rezervacija_je_moguca_samo_za_aktivan_oglas()
    {
        var oglas = AktivanOglas();
        oglas.Rezervisi();

        oglas.Status.Should().Be(StatusOglasa.Rezervisan);
        oglas.Invoking(o => o.Rezervisi()).Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("nedozvoljen_prelaz");
    }

    [Fact]
    public void Prodat_oglas_je_zavrsno_stanje()
    {
        var oglas = AktivanOglas();
        oglas.OznaciKaoProdat();

        oglas.Invoking(o => o.Obnovi(Sada)).Should().Throw<DomenskiIzuzetak>();
        oglas.Invoking(o => o.Ukloni("razlog")).Should().Throw<DomenskiIzuzetak>();
        oglas.Invoking(o => o.Izmeni(11, 1, "Novi naslov", "Novi opis oglasa", TipOglasa.Prodaja, 5, StanjeProizvoda.Novo, true, true, null))
            .Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("oglas_zakljucan");
    }

    [Fact]
    public void Istek_nije_dozvoljen_pre_roka()
    {
        var oglas = AktivanOglas();
        oglas.Invoking(o => o.Istekni(Sada.AddDays(10))).Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("oglas_nije_istekao");
    }

    [Fact]
    public void Istekli_oglas_se_obnavlja_na_novih_60_dana()
    {
        var oglas = AktivanOglas();
        var posleIsteka = Sada.AddDays(61);
        oglas.Istekni(posleIsteka);

        oglas.Obnovi(posleIsteka);

        oglas.Status.Should().Be(StatusOglasa.Aktivan);
        oglas.IsticeUtc.Should().Be(posleIsteka.AddDays(60));
    }

    [Fact]
    public void Aktivan_oglas_moze_da_se_produzi_tek_kada_istice_za_manje_od_7_dana()
    {
        var oglas = AktivanOglas();
        oglas.Invoking(o => o.Obnovi(Sada.AddDays(10))).Should().Throw<DomenskiIzuzetak>();
        oglas.Obnovi(Sada.AddDays(55));
        oglas.IsticeUtc.Should().Be(Sada.AddDays(115));
    }

    [Fact]
    public void Rezervisan_oglas_ne_moze_da_se_obrise()
    {
        var oglas = AktivanOglas();
        oglas.Rezervisi();
        oglas.Invoking(o => o.ObrisiOdStraneVlasnika(Sada)).Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("oglas_rezervisan");
    }

    [Fact]
    public void Najvise_10_slika_po_oglasu()
    {
        var oglas = AktivanOglas();
        for (var i = 0; i < PoslovnaPravila.MaksSlikaPoOglasu; i++)
        {
            oglas.DodajSliku($"/uploads/{i}.jpg", "image/jpeg", 1000, Sada);
        }

        oglas.Slike.Select(s => s.Redosled).Should().BeEquivalentTo(Enumerable.Range(0, 10));
        oglas.Invoking(o => o.DodajSliku("/uploads/11.jpg", "image/jpeg", 1000, Sada))
            .Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("previse_slika");
    }

    [Fact]
    public void Uklanjanje_slike_prenumerise_preostale_bez_rupa()
    {
        var oglas = AktivanOglas();
        var slike = Enumerable.Range(0, 4).Select(i => oglas.DodajSliku($"/{i}.jpg", "image/jpeg", 10, Sada).SaId(i + 1)).ToList();

        oglas.UkloniSliku(2);

        oglas.Slike.OrderBy(s => s.Redosled).Select(s => (s.Id, s.Redosled)).Should().Equal((1, 0), (3, 1), (4, 2));
    }

    [Fact]
    public void Novi_redosled_mora_biti_permutacija_postojecih_slika()
    {
        var oglas = AktivanOglas();
        oglas.DodajSliku("/a.jpg", "image/jpeg", 10, Sada).SaId(1);
        oglas.DodajSliku("/b.jpg", "image/jpeg", 10, Sada).SaId(2);

        oglas.PripremiNoviRedosled([2, 1]).Should().BeEquivalentTo(new Dictionary<int, int> { [2] = 0, [1] = 1 });
        oglas.Invoking(o => o.PripremiNoviRedosled([1, 3])).Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("neispravan_redosled");
    }

    [Fact]
    public void Obavezan_atribut_mora_biti_popunjen_a_vrednost_odgovarati_tipu()
    {
        var oglas = AktivanOglas();
        var marka = new DefinicijaAtributa(11, "Marka", "marka", TipAtributa.Tekst, true, null, null, 1).SaId(1);
        var godiste = new DefinicijaAtributa(11, "Godište", "godiste", TipAtributa.Broj, false, null, null, 2).SaId(2);
        var definicije = new[] { marka, godiste };

        oglas.Invoking(o => o.PostaviAtribute(new Dictionary<int, string?> { [2] = "2010" }, definicije))
            .Should().Throw<DomenskiIzuzetak>().WithMessage("*Marka*obavezan*");
        oglas.Invoking(o => o.PostaviAtribute(new Dictionary<int, string?> { [1] = "Opel", [2] = "dvehiljade" }, definicije))
            .Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("atribut_nevalidan");
        oglas.Invoking(o => o.PostaviAtribute(new Dictionary<int, string?> { [1] = "Opel", [99] = "x" }, definicije))
            .Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("atribut_nepoznat");

        oglas.PostaviAtribute(new Dictionary<int, string?> { [1] = "Opel", [2] = "2010" }, definicije);
        oglas.Atributi.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(TipAtributa.DaNe, "da", true)]
    [InlineData(TipAtributa.DaNe, "mozda", false)]
    [InlineData(TipAtributa.Broj, "12.5", true)]
    [InlineData(TipAtributa.Broj, "12,5abc", false)]
    public void Definicija_atributa_proverava_tip_vrednosti(TipAtributa tip, string vrednost, bool validno)
    {
        var def = new DefinicijaAtributa(1, "Atribut", "atribut", tip, false, null, null, 0);
        def.JeValidnaVrednost(vrednost).Should().Be(validno);
    }

    [Fact]
    public void Atribut_tipa_izbor_prihvata_samo_ponudjene_opcije()
    {
        var def = new DefinicijaAtributa(1, "Gorivo", "gorivo", TipAtributa.Izbor, true, ["Benzin", "Dizel"], null, 0);
        def.JeValidnaVrednost("Dizel").Should().BeTrue();
        def.JeValidnaVrednost("Struja").Should().BeFalse();
        def.JeValidnaVrednost(null).Should().BeFalse("atribut je obavezan");
    }

    [Fact]
    public void Slug_kategorije_uklanja_kvacice_i_specijalne_znakove()
    {
        Kategorija.NapraviSlug("Kuća i bašta / Đubrivo!").Should().Be("kuca-i-basta-djubrivo");
    }
}
