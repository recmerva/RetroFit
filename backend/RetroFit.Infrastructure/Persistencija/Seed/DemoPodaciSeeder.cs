using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;

namespace RetroFit.Infrastructure.Persistencija.Seed;

/// <summary>
/// Demo podaci za razvoj i odbranu projekta. Svi podaci se prave KROZ DOMENSKE METODE
/// (Oglas.Kreiraj, Ponuda.Prihvati, Transakcija.Potvrdi...), pa poštuju ista poslovna pravila
/// kao i podaci koje unose korisnici. Pokreće se samo ako u bazi nema nijednog korisnika osim admina.
///
/// Nalozi (lozinka za sve obične korisnike: Lozinka123):
///   admin@retrofit.rs / Admin123!          (Admin + Moderator)
///   moderator@retrofit.rs / Moderator123!  (Moderator)
///   marko@retrofit.rs, ana@retrofit.rs, jovan@retrofit.rs, milica@retrofit.rs
/// </summary>
internal static class DemoPodaciSeeder
{
    public static async Task PopuniAsync(IServiceProvider sp, RetroFitDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.Oglasi.AnyAsync(ct) || await db.Korisnici.CountAsync(ct) > 1)
        {
            logger.LogInformation("Demo podaci već postoje, preskačem.");
            return;
        }

        var sada = sp.GetRequiredService<IDatumVreme>().SadaUtc;
        var hes = sp.GetRequiredService<IHesiranjeLozinke>();
        var uloge = await db.Uloge.ToDictionaryAsync(u => u.Naziv, ct);
        var definicije = await db.DefinicijeAtributa.ToListAsync(ct);

        Korisnik NoviKorisnik(string email, string ime, string prezime, int gradId, string lozinka, params string[] dodatneUloge)
        {
            var k = new Korisnik(email, ime, prezime, gradId);
            k.PostaviLozinkuHash(hes.Hesiraj(lozinka));
            k.DodeliUlogu(uloge[NaziviUloga.Korisnik], sada);
            foreach (var u in dodatneUloge)
            {
                k.DodeliUlogu(uloge[u], sada);
            }

            db.Korisnici.Add(k);
            return k;
        }

        var admin = await db.Korisnici.Include(k => k.Uloge).ThenInclude(u => u.Uloga)
                        .FirstOrDefaultAsync(k => k.Uloge.Any(u => u.UlogaId == PocetniPodaci.UlogaAdminId), ct)
                    ?? NoviKorisnik("admin@retrofit.rs", "Admin", "RetroFit", 1, "Admin123!", NaziviUloga.Moderator, NaziviUloga.Admin);
        NoviKorisnik("moderator@retrofit.rs", "Mira", "Moderatović", 2, "Moderator123!", NaziviUloga.Moderator);
        var marko = NoviKorisnik("marko@retrofit.rs", "Marko", "Petrović", 1, "Lozinka123");
        var ana = NoviKorisnik("ana@retrofit.rs", "Ana", "Jovanović", 2, "Lozinka123");
        var jovan = NoviKorisnik("jovan@retrofit.rs", "Jovan", "Nikolić", 3, "Lozinka123");
        var milica = NoviKorisnik("milica@retrofit.rs", "Milica", "Ilić", 4, "Lozinka123");
        marko.PostaviTelefon("+381641234567");
        marko.PotvrdiTelefon("+381641234567", sada);
        ana.PostaviTelefon("+381652345678");
        ana.PotvrdiTelefon("+381652345678", sada);
        await db.SaveChangesAsync(ct);

        Oglas Oglas(Korisnik prodavac, int kategorija, int grad, string naslov, string opis, TipOglasa tip, decimal? cena,
            StanjeProizvoda stanje, int danaUnazad, bool dostava = true, Dictionary<string, string>? atributi = null, string? zamena = null)
        {
            var o = Domain.Entiteti.Oglas.Kreiraj(prodavac.Id, kategorija, grad, naslov, opis, tip, cena, stanje, dostava, true, zamena,
                objaviOdmah: false, sada);
            o.Objavi(sada.AddDays(-danaUnazad).AddHours(-danaUnazad % 7));
            if (atributi is not null)
            {
                var defKategorije = definicije.Where(d => d.KategorijaId == kategorija).ToList();
                o.PostaviAtribute(atributi.ToDictionary(a => defKategorije.Single(d => d.Kljuc == a.Key).Id, a => (string?)a.Value),
                    defKategorije);
            }

            db.Oglasi.Add(o);
            return o;
        }

        var iphone = Oglas(marko, 11, 1, "iPhone 13 128GB, odlična baterija",
            "Telefon je korišćen godinu dana, uvek u maski i sa zaštitnim staklom. Baterija 89%. Uz telefon idu kutija i punjač.",
            TipOglasa.Prodaja, 52000, StanjeProizvoda.KaoNovo, 2, atributi: new() { ["marka"] = "Apple", ["memorija"] = "128 GB", ["boja"] = "Plava" });
        var golf = Oglas(jovan, 21, 3, "Volkswagen Golf 7 1.6 TDI",
            "Redovno servisiran, garažiran, prvi vlasnik u Srbiji. Nove gume i kočnice. Registrovan do juna.",
            TipOglasa.Prodaja, 1150000, StanjeProizvoda.Polovno, 5, dostava: false,
            atributi: new() { ["marka"] = "Volkswagen", ["godiste"] = "2015", ["kilometraza"] = "187000", ["gorivo"] = "Dizel", ["menjac"] = "Manuelni" });
        var gramofon = Oglas(ana, 62, 2, "Retro gramofon Tesla NC 440",
            "Čuvan, ispravan gramofon iz 80-ih. Nova igla. Idealan za kolekcionare vinila.",
            TipOglasa.Prodaja, 15000, StanjeProizvoda.Polovno, 1, atributi: new() { ["format"] = "LP" });
        var nes = Oglas(marko, 72, 1, "Nintendo NES sa dva džojstika i 5 igara",
            "Originalna konzola, radi besprekorno. Igre: Super Mario Bros, Tetris, Contra, Duck Hunt, Zelda.",
            TipOglasa.Prodaja, 18000, StanjeProizvoda.Polovno, 3, atributi: new() { ["platforma"] = "NES", ["originalna-kutija"] = "ne" });
        var bicikl = Oglas(milica, 23, 4, "Gradski bicikl Capriolo, veličina M",
            "Bicikl sa korpom i blatobranima, 7 brzina. Malo korišćen.",
            TipOglasa.Prodaja, 21000, StanjeProizvoda.KaoNovo, 8, atributi: new() { ["velicina-rama"] = "M", ["elektricni"] = "ne" });
        var laptop = Oglas(jovan, 12, 3, "Lenovo ThinkPad T480, i5, 16GB RAM",
            "Poslovni laptop u odličnom stanju, nova baterija, SSD 512 GB. Windows 11.",
            TipOglasa.Prodaja, 38000, StanjeProizvoda.Polovno, 4, atributi: new() { ["procesor"] = "Intel i5-8350U", ["ram"] = "16", ["skladiste"] = "512" });
        var kauc = Oglas(ana, 41, 2, "Ugaona garnitura na razvlačenje",
            "Siva ugaona garnitura sa sanducima, 2 godine stara. Preuzimanje lično, nosi kupac.",
            TipOglasa.Prodaja, 25000, StanjeProizvoda.Polovno, 10, dostava: false, atributi: new() { ["materijal"] = "Štof" });
        Oglas(milica, 61, 4, "Komplet knjiga Harry Potter (srpsko izdanje)",
            "Svih 7 knjiga, izdanje Evro Đunti. Očuvane, bez podvlačenja.",
            TipOglasa.Prodaja, 6500, StanjeProizvoda.Polovno, 6, atributi: new() { ["autor"] = "J. K. Rowling", ["jezik"] = "Srpski" });
        Oglas(marko, 81, 1, "Poklanjam LEGO kockice (oko 2 kg)",
            "Mešane LEGO kockice, deca su porasla. Poklanjam onome kome trebaju, preuzimanje na Vračaru.",
            TipOglasa.Poklon, null, StanjeProizvoda.Polovno, 1, dostava: false);
        Oglas(jovan, 14, 3, "Menjam Canon EOS 600D za dron",
            "Fotoaparat sa objektivom 18-55mm, torbom i dve baterije. Menjam za dron sličnog ranga.",
            TipOglasa.Razmena, null, StanjeProizvoda.Polovno, 7, zamena: "DJI Mini ili sličan dron");
        Oglas(ana, 31, 2, "Zimska jakna The North Face, vel. S",
            "Topla pernata jakna, nošena jednu sezonu.",
            TipOglasa.Prodaja, 9000, StanjeProizvoda.KaoNovo, 12, atributi: new() { ["velicina"] = "S" });
        Oglas(milica, 33, 4, "Patike Nike Air Max, broj 41",
            "Nošene par puta, bez oštećenja. Originalna kutija.",
            TipOglasa.Prodaja, 7500, StanjeProizvoda.KaoNovo, 9, atributi: new() { ["broj"] = "41" });
        Oglas(marko, 71, 1, "Stari radio Grundig iz 1960-ih",
            "Kolekcionarski primerak, ne radi - za delove ili restauraciju.",
            TipOglasa.Prodaja, 8000, StanjeProizvoda.ZaDelove, 15, atributi: new() { ["godina"] = "1964" });
        Oglas(jovan, 51, 3, "Bučice 2 x 10 kg sa šipkom",
            "Komplet tegova, gumirani. Preuzimanje lično u Nišu.",
            TipOglasa.Prodaja, 5500, StanjeProizvoda.Polovno, 20, dostava: false);
        Oglas(ana, 42, 2, "Mikrotalasna Gorenje 20L",
            "Ispravna, čista, sa uputstvom. Prodajem zbog selidbe.",
            TipOglasa.Prodaja, 4000, StanjeProizvoda.Polovno, 3);
        Oglas(milica, 91, 4, "Transporter za mačke + posuda",
            "Transporter srednje veličine, kao nov. Gratis keramička posuda.",
            TipOglasa.Prodaja, 1800, StanjeProizvoda.KaoNovo, 2);
        Oglas(marko, 13, 1, "Samsung televizor 55\" 4K",
            "Smart TV, 3 godine star, bez ogrebotina. Daljinski i stalak.",
            TipOglasa.Prodaja, 42000, StanjeProizvoda.Polovno, 11, atributi: new() { ["dijagonala"] = "55" });
        // Oglas objavljen pre 61 dan: pozadinski posao će ga označiti kao istekao (demonstracija automatskog isteka).
        Oglas(jovan, 43, 3, "Električna kosilica Bosch",
            "Radi ispravno, nož naoštren. Oglas je star - primer automatskog isteka posle 60 dana.",
            TipOglasa.Prodaja, 6000, StanjeProizvoda.Polovno, 61);
        await db.SaveChangesAsync(ct);

        // ---- Završena kupovina sa recenzijama: Ana kupuje NES od Marka ----
        var razgovor = new Razgovor(nes, ana.Id, sada.AddDays(-2));
        db.Razgovori.Add(razgovor);
        razgovor.PosaljiPoruku(ana.Id, "Zdravo! Da li je konzola još dostupna?", TipPoruke.BrzaPoruka, sada.AddDays(-2));
        razgovor.PosaljiPoruku(marko.Id, "Jeste, dostupna je. Sve radi kao na slikama.", TipPoruke.Tekst, sada.AddDays(-2).AddMinutes(20));
        var ponuda = Ponuda.Kreiraj(razgovor, nes, ana.Id, 16000, "Može 16.000?", sada.AddDays(-2).AddHours(1));
        db.Ponude.Add(ponuda);
        await db.SaveChangesAsync(ct);

        var kontra = ponuda.Kontraponuda(marko.Id, 17000, "Može za 17.000, uz poštarinu na moj račun.", sada.AddDays(-2).AddHours(2));
        db.Ponude.Add(kontra);
        await db.SaveChangesAsync(ct);
        kontra.Prihvati(ana.Id, nes, sada.AddDays(-2).AddHours(3));
        var rezervacija = Rezervacija.Kreiraj(nes, ana.Id, kontra.Iznos, kontra, sada.AddDays(-2).AddHours(3));
        db.Rezervacije.Add(rezervacija);
        await db.SaveChangesAsync(ct);

        var transakcija = Transakcija.IzRezervacije(rezervacija, nes, ana.Id, NacinPreuzimanja.Dostava, "Bulevar oslobođenja 10, Novi Sad",
            koristiEscrow: true, sada.AddDays(-2).AddHours(4));
        db.Transakcije.Add(transakcija);
        await db.SaveChangesAsync(ct);
        transakcija.EvidentirajUplatu(ana.Id);
        db.Placanja.Add(new Placanje(ana.Id, transakcija.Id, TipPlacanja.EscrowUplata, StatusPlacanja.Uspesno, 17000,
            $"MOCK-NAP-{Guid.NewGuid():N}", null, "Escrow uplata za oglas #" + nes.Id, sada.AddDays(-2).AddHours(4)));
        transakcija.Potvrdi(marko.Id, sada.AddDays(-1));
        transakcija.Potvrdi(ana.Id, sada.AddHours(-20));
        db.Placanja.Add(new Placanje(marko.Id, transakcija.Id, TipPlacanja.EscrowIsplata, StatusPlacanja.Uspesno, 17000,
            $"MOCK-ISP-{Guid.NewGuid():N}", null, "Escrow isplata za oglas #" + nes.Id, sada.AddHours(-20)));
        marko.EvidentirajZavrsenuTransakciju();
        ana.EvidentirajZavrsenuTransakciju();
        await db.SaveChangesAsync(ct);

        db.Recenzije.Add(Recenzija.Kreiraj(transakcija, ana.Id, 5, "Sve kao u opisu, brzo poslato. Preporuka!", sada.AddHours(-19)));
        marko.DodajOcenu(5);
        db.Recenzije.Add(Recenzija.Kreiraj(transakcija, marko.Id, 5, "Korektna kupovina, brza uplata.", sada.AddHours(-18)));
        ana.DodajOcenu(5);

        // ---- Ponuda na čekanju: Milica nudi za iPhone ----
        var razgovor2 = new Razgovor(iphone, milica.Id, sada.AddHours(-5));
        db.Razgovori.Add(razgovor2);
        razgovor2.PosaljiPoruku(milica.Id, "Da li je moguća zamena za Samsung uz doplatu?", TipPoruke.Tekst, sada.AddHours(-5));
        await db.SaveChangesAsync(ct);
        db.Ponude.Add(Ponuda.Kreiraj(razgovor2, iphone, milica.Id, 48000, null, sada.AddHours(-4)));

        // ---- Omiljeni, prijava, zahtev "Tražim", izdvojeni oglasi ----
        db.OmiljeniOglasi.Add(new OmiljeniOglas(ana.Id, golf.Id, sada.AddHours(-3)));
        golf.PromeniBrojOmiljenih(1);
        db.OmiljeniOglasi.Add(new OmiljeniOglas(jovan.Id, gramofon.Id, sada.AddHours(-2)));
        gramofon.PromeniBrojOmiljenih(1);
        db.Prijave.Add(new Prijava(kauc, milica.Id, RazlogPrijave.Duplikat, "Isti oglas je objavljen dva puta.", sada.AddHours(-1)));

        var zahtev = new ZahtevTrazim(jovan.Id, "Tražim retro konzolu Sega Mega Drive",
            "Kupujem ispravnu Sega Mega Drive konzolu, poželjno sa igrama.", 72, 3, 20000);
        db.ZahteviTrazim.Add(zahtev);
        await db.SaveChangesAsync(ct);
        zahtev.DodajOdgovor(bicikl, milica.Id, "Nemam Segu, ali možda vas zanima ovaj bicikl u zamenu?", sada);

        db.IstaknutiOglasi.Add(IstaknutiOglas.Izdvoji(gramofon, admin.Id, sada, sada.AddDays(14)));
        db.IstaknutiOglasi.Add(IstaknutiOglas.Izdvoji(golf, admin.Id, sada, sada.AddDays(14)));

        db.Obavestenja.Add(new Obavestenje(marko.Id, TipObavestenja.NovaPonuda, "Nova ponuda",
            "Milica I. nudi 48.000 RSD za \"iPhone 13 128GB, odlična baterija\".", $"/razgovori/{razgovor2.Id}", sada.AddHours(-4)));

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo podaci kreirani: {Korisnika} korisnika, {Oglasa} oglasa.",
            await db.Korisnici.CountAsync(ct), await db.Oglasi.CountAsync(ct));
    }
}
