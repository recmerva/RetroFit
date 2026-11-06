using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using static RetroFit.Tests.Pomocne.TestPodaci;

namespace RetroFit.Tests.Domen;

public class PonudaTests
{
    [Fact]
    public void Ponuda_je_moguca_samo_za_oglas_tipa_Prodaja()
    {
        var oglas = AktivanOglas(tip: TipOglasa.Poklon);
        var razgovor = NoviRazgovor(oglas);
        var akcija = () => Ponuda.Kreiraj(razgovor, oglas, KupacId, 100, null, Sada);
        akcija.Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("ponuda_nije_moguca");
    }

    [Fact]
    public void Prodavac_ne_moze_zapoceti_razgovor_o_svom_oglasu()
    {
        var oglas = AktivanOglas();
        var akcija = () => new Razgovor(oglas, ProdavacId, Sada);
        akcija.Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("sopstveni_oglas");
    }

    [Fact]
    public void Kupac_ne_moze_prihvatiti_sopstvenu_ponudu()
    {
        var oglas = AktivanOglas();
        var ponuda = PonudaNaCekanju(oglas, NoviRazgovor(oglas));
        ponuda.Invoking(p => p.Prihvati(KupacId, oglas, Sada)).Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("zabranjeno");
    }

    [Fact]
    public void Treca_osoba_ne_moze_odgovoriti_na_ponudu()
    {
        var oglas = AktivanOglas();
        var ponuda = PonudaNaCekanju(oglas, NoviRazgovor(oglas));
        ponuda.Invoking(p => p.Odbij(DrugiKupacId, Sada)).Should().Throw<DomenskiIzuzetak>();
    }

    [Fact]
    public void Kontraponuda_zatvara_staru_i_pravi_novu_na_koju_odgovara_kupac()
    {
        var oglas = AktivanOglas();
        var ponuda = PonudaNaCekanju(oglas, NoviRazgovor(oglas), 8_000);

        var kontra = ponuda.Kontraponuda(ProdavacId, 9_500, "Može 9.500", Sada.AddMinutes(5));

        ponuda.Status.Should().Be(StatusPonude.Kontraponuda);
        kontra.Status.Should().Be(StatusPonude.NaCekanju);
        kontra.PonudioKorisnikId.Should().Be(ProdavacId);
        kontra.OdgovaraKorisnikId.Should().Be(KupacId);
        kontra.PrethodnaPonuda.Should().BeSameAs(ponuda);
        kontra.Invoking(k => k.Prihvati(ProdavacId, oglas, Sada)).Should().Throw<DomenskiIzuzetak>("prodavac ne prihvata svoju kontraponudu");
        kontra.Prihvati(KupacId, oglas, Sada.AddMinutes(10));
        kontra.Status.Should().Be(StatusPonude.Prihvacena);
    }

    [Fact]
    public void Istekla_ponuda_ne_moze_da_se_prihvati()
    {
        var oglas = AktivanOglas();
        var ponuda = PonudaNaCekanju(oglas, NoviRazgovor(oglas));
        var kasnije = Sada.AddHours(49);

        ponuda.Invoking(p => p.Prihvati(ProdavacId, oglas, kasnije)).Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("ponuda_istekla");
        ponuda.Istekni(kasnije);
        ponuda.Status.Should().Be(StatusPonude.Istekla);
    }

    [Fact]
    public void Na_zatvorenu_ponudu_se_ne_moze_ponovo_odgovoriti()
    {
        var oglas = AktivanOglas();
        var ponuda = PonudaNaCekanju(oglas, NoviRazgovor(oglas));
        ponuda.Odbij(ProdavacId, Sada);
        ponuda.Invoking(p => p.Prihvati(ProdavacId, oglas, Sada)).Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("ponuda_zatvorena");
    }
}

public class RezervacijaTests
{
    [Fact]
    public void Rezervacija_pripada_konkretnom_kupcu_i_rezervise_oglas()
    {
        var (oglas, rezervacija) = Rezervisano();

        rezervacija.KupacId.Should().Be(KupacId);
        rezervacija.ProdavacId.Should().Be(ProdavacId);
        rezervacija.Status.Should().Be(StatusRezervacije.Aktivna);
        rezervacija.IsticeUtc.Should().Be(Sada.AddHours(72));
        oglas.Status.Should().Be(StatusOglasa.Rezervisan);
    }

    [Fact]
    public void Prodavac_ne_moze_rezervisati_sopstveni_oglas_za_sebe()
    {
        var oglas = AktivanOglas();
        var akcija = () => Rezervacija.Kreiraj(oglas, ProdavacId, 100, null, Sada);
        akcija.Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("sopstveni_oglas");
    }

    [Fact]
    public void Otkazivanje_vraca_oglas_u_aktivan()
    {
        var (oglas, rezervacija) = Rezervisano();
        rezervacija.Otkazi(KupacId, "Predomislio sam se", oglas, Sada.AddHours(1));

        rezervacija.Status.Should().Be(StatusRezervacije.Otkazana);
        oglas.Status.Should().Be(StatusOglasa.Aktivan);
    }

    [Fact]
    public void Istek_rezervacije_vraca_oglas_u_aktivan()
    {
        var (oglas, rezervacija) = Rezervisano();
        rezervacija.Invoking(r => r.Istekni(oglas, Sada.AddHours(10))).Should().Throw<DomenskiIzuzetak>();

        rezervacija.Istekni(oglas, Sada.AddHours(73));
        rezervacija.Status.Should().Be(StatusRezervacije.Istekla);
        oglas.Status.Should().Be(StatusOglasa.Aktivan);
    }
}

public class TransakcijaTests
{
    [Fact]
    public void Kupovinu_pokrece_samo_kupac_iz_rezervacije()
    {
        var (oglas, rezervacija) = Rezervisano();
        var akcija = () => Transakcija.IzRezervacije(rezervacija, oglas, DrugiKupacId, NacinPreuzimanja.LicnoPreuzimanje, null, false, Sada);
        akcija.Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("zabranjeno");
    }

    [Fact]
    public void Dostava_zahteva_adresu()
    {
        var (oglas, rezervacija) = Rezervisano();
        var akcija = () => Transakcija.IzRezervacije(rezervacija, oglas, KupacId, NacinPreuzimanja.Dostava, null, false, Sada);
        akcija.Should().Throw<DomenskiIzuzetak>();
    }

    [Fact]
    public void Sa_escrowom_potvrda_nije_moguca_pre_uplate()
    {
        var t = TransakcijaUToku(escrow: true);
        t.StatusEscrowa.Should().Be(StatusEscrowa.CekaUplatu);
        t.Invoking(x => x.Potvrdi(ProdavacId, Sada)).Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("escrow_stanje");
    }

    [Fact]
    public void Kada_obe_strane_potvrde_transakcija_je_zavrsena_oglas_prodat_a_novac_isplacen()
    {
        var t = TransakcijaUToku(escrow: true);
        t.EvidentirajUplatu(KupacId);
        t.StatusEscrowa.Should().Be(StatusEscrowa.Deponovano);

        t.Potvrdi(ProdavacId, Sada.AddHours(2)).Should().BeFalse();
        t.Potvrdi(KupacId, Sada.AddHours(3)).Should().BeTrue();

        t.Status.Should().Be(StatusTransakcije.Zavrsena);
        t.StatusEscrowa.Should().Be(StatusEscrowa.Isplaceno);
        t.Oglas.Status.Should().Be(StatusOglasa.Prodat);
    }

    [Fact]
    public void Ista_strana_ne_moze_potvrditi_dva_puta()
    {
        var t = TransakcijaUToku(escrow: false);
        t.Potvrdi(ProdavacId, Sada);
        t.Invoking(x => x.Potvrdi(ProdavacId, Sada)).Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("vec_potvrdjeno");
    }

    [Fact]
    public void Otkazivanje_posle_uplate_vraca_novac_i_oglas_u_aktivan()
    {
        var t = TransakcijaUToku(escrow: true);
        t.EvidentirajUplatu(KupacId);

        t.Otkazi(ProdavacId, "Predmet je oštećen", Sada.AddHours(2));

        t.Status.Should().Be(StatusTransakcije.Otkazana);
        t.StatusEscrowa.Should().Be(StatusEscrowa.Vraceno);
        t.Oglas.Status.Should().Be(StatusOglasa.Aktivan);
    }

    [Fact]
    public void Uplatu_vrsi_samo_kupac()
    {
        var t = TransakcijaUToku(escrow: true);
        t.Invoking(x => x.EvidentirajUplatu(ProdavacId)).Should().Throw<DomenskiIzuzetak>();
    }
}

public class RecenzijaTests
{
    [Fact]
    public void Recenzija_nije_moguca_za_nezavrsenu_transakciju()
    {
        var t = TransakcijaUToku(escrow: false);
        var akcija = () => Recenzija.Kreiraj(t, KupacId, 5, null, Sada);
        akcija.Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("transakcija_nije_zavrsena");
    }

    [Fact]
    public void Ocenjuje_se_druga_strana_u_transakciji()
    {
        var t = ZavrsenaTransakcija();
        Recenzija.Kreiraj(t, KupacId, 5, "Odlično", Sada.AddHours(4)).OcenjeniKorisnikId.Should().Be(ProdavacId);
        Recenzija.Kreiraj(t, ProdavacId, 4, null, Sada.AddHours(4)).OcenjeniKorisnikId.Should().Be(KupacId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Ocena_mora_biti_od_1_do_5(int ocena)
    {
        var akcija = () => Recenzija.Kreiraj(ZavrsenaTransakcija(), KupacId, ocena, null, Sada.AddHours(4));
        akcija.Should().Throw<DomenskiIzuzetak>();
    }

    [Fact]
    public void Osoba_van_transakcije_ne_moze_ostaviti_recenziju()
    {
        var akcija = () => Recenzija.Kreiraj(ZavrsenaTransakcija(), DrugiKupacId, 1, null, Sada.AddHours(4));
        akcija.Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("zabranjeno");
    }

    [Fact]
    public void Recenzija_posle_30_dana_nije_dozvoljena()
    {
        var akcija = () => Recenzija.Kreiraj(ZavrsenaTransakcija(), KupacId, 5, null, Sada.AddDays(31));
        akcija.Should().Throw<DomenskiIzuzetak>().Which.Kod.Should().Be("rok_istekao");
    }
}
