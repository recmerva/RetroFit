using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using RetroFit.Infrastructure.Identitet;
using RetroFit.Infrastructure.Pretraga;
using RetroFit.Infrastructure.Servisi;
using RetroFit.Tests.Pomocne;
using static RetroFit.Tests.Pomocne.TestPodaci;

namespace RetroFit.Tests.Infrastruktura;

public class FullTextUpitTests
{
    [Fact]
    public void Reci_se_pretvaraju_u_prefiksnu_pretragu_spojenu_sa_AND() =>
        FullTextUpit.Napravi("Crveni  bicikl").Should().Be("\"crveni*\" AND \"bicikl*\"");

    [Fact]
    public void Specijalni_znakovi_ne_mogu_da_promene_smisao_upita()
    {
        // Pokušaj "ubacivanja" FTS operatora: navodnici, OR, zagrade i zvezdice se uklanjaju.
        FullTextUpit.Napravi("a\" OR \"b*) NEAR(x").Should().Be("\"or*\" AND \"near*\"");
    }

    [Fact]
    public void Prazan_ili_prekratak_upit_nema_uslov()
    {
        FullTextUpit.Napravi("   ").Should().BeNull();
        FullTextUpit.Napravi("a b").Should().BeNull();
    }

    [Fact]
    public void Najvise_sest_reci_bez_duplikata()
    {
        FullTextUpit.Reci("jedan dva tri cetiri pet sest sedam osam jedan").Should().HaveCount(6).And.OnlyHaveUniqueItems();
        FullTextUpit.Reci("Čačak ČAČAK").Should().Equal("čačak");
    }
}

public class SkladisteTests
{
    [Fact]
    public void Tip_slike_se_prepoznaje_po_potpisu_a_ne_po_ekstenziji()
    {
        LokalnoSkladisteFajlova.PrepoznajTip([0xFF, 0xD8, 0xFF, 0xE0]).Tip.Should().Be("image/jpeg");
        LokalnoSkladisteFajlova.PrepoznajTip([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]).Tip.Should().Be("image/png");
        LokalnoSkladisteFajlova.PrepoznajTip("RIFF1234WEBPVP8 "u8.ToArray()).Tip.Should().Be("image/webp");
        LokalnoSkladisteFajlova.PrepoznajTip("<script>alert(1)</script>"u8.ToArray()).Tip.Should().BeNull();
        LokalnoSkladisteFajlova.PrepoznajTip("MZ\x90\x00"u8.ToArray()).Tip.Should().BeNull("exe fajl preimenovan u .jpg");
    }
}

public class TokenIHesiranjeTests
{
    private static TokenServis Servis() => new(Options.Create(new JwtOpcije
    {
        Kljuc = "test-kljuc-koji-ima-vise-od-32-bajta-123",
        Izdavac = "RetroFit",
        Publika = "Test",
        AccessTokenMinuta = 15,
        RefreshTokenDana = 14
    }), new FiksnoVreme(Sada));

    [Fact]
    public void Access_token_sadrzi_id_i_uloge_a_ne_privatne_podatke()
    {
        var korisnik = NoviKorisnik(42, "tajni@primer.rs");
        var (token, istice) = Servis().KreirajAccessToken(korisnik, ["Korisnik", "Moderator"]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Subject.Should().Be("42");
        jwt.Claims.Where(c => c.Type.EndsWith("role")).Select(c => c.Value).Should().BeEquivalentTo("Korisnik", "Moderator");
        token.Should().NotContain("tajni");
        jwt.Claims.Should().NotContain(c => c.Value.Contains("tajni@primer.rs"));
        istice.Should().Be(Sada.AddMinutes(15));
    }

    [Fact]
    public void Refresh_token_je_nasumican_a_u_bazu_ide_samo_hes()
    {
        var s = Servis();
        var (t1, h1, _) = s.KreirajRefreshToken();
        var (t2, _, istice) = s.KreirajRefreshToken();

        t1.Should().NotBe(t2);
        h1.Should().HaveLength(64).And.NotBe(t1);
        s.Hesiraj(t1).Should().Be(h1);
        istice.Should().Be(Sada.AddDays(14));
    }

    [Fact]
    public void Lozinka_se_hesira_sa_solju_i_proverava()
    {
        var h = new HesiranjeLozinke();
        var hes1 = h.Hesiraj("Lozinka123");
        var hes2 = h.Hesiraj("Lozinka123");

        hes1.Should().NotBe(hes2, "svaki heš ima nasumičnu so");
        hes1.Should().NotContain("Lozinka123");
        h.Proveri(hes1, "Lozinka123").Should().BeTrue();
        h.Proveri(hes1, "lozinka123").Should().BeFalse();
        h.Proveri("OBRISAN", "bilo-sta").Should().BeFalse("obrisan nalog nema važeću lozinku");
    }
}
