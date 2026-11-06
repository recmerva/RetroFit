using NetTopologySuite.Geometries;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;

namespace RetroFit.Infrastructure.Persistencija.Seed;

/// <summary>
/// Referentni (šifarnički) podaci koji su deo migracije (EF Core HasData):
/// uloge, gradovi, kategorije i definicije atributa. Ovi podaci su potrebni da bi aplikacija
/// uopšte radila, pa pripadaju šemi baze, a ne demo podacima.
/// Demo korisnici i oglasi se dodaju posebno (DemoPodaciSeeder), samo u razvojnom okruženju.
/// </summary>
internal static class PocetniPodaci
{
    private static readonly DateTime Datum = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public const int UlogaKorisnikId = 1;
    public const int UlogaModeratorId = 2;
    public const int UlogaAdminId = 3;

    public static object[] Uloge() =>
    [
        new { Id = UlogaKorisnikId, Naziv = NaziviUloga.Korisnik, Opis = "Običan korisnik: kupuje i prodaje." },
        new { Id = UlogaModeratorId, Naziv = NaziviUloga.Moderator, Opis = "Obrađuje prijave, uklanja oglase i blokira korisnike." },
        new { Id = UlogaAdminId, Naziv = NaziviUloga.Admin, Opis = "Puna administracija: kategorije, uloge, statistika, audit." }
    ];

    private static readonly (int Id, string Naziv, string Pb, double Lat, double Lng)[] ListaGradova =
    [
        (1, "Beograd", "11000", 44.8125, 20.4612),
        (2, "Novi Sad", "21000", 45.2671, 19.8335),
        (3, "Niš", "18000", 43.3209, 21.8958),
        (4, "Kragujevac", "34000", 44.0128, 20.9114),
        (5, "Subotica", "24000", 46.1003, 19.6658),
        (6, "Zrenjanin", "23000", 45.3816, 20.3686),
        (7, "Pančevo", "26000", 44.8708, 20.6403),
        (8, "Čačak", "32000", 43.8914, 20.3497),
        (9, "Kraljevo", "36000", 43.7258, 20.6894),
        (10, "Novi Pazar", "36300", 43.1367, 20.5122),
        (11, "Smederevo", "11300", 44.6628, 20.9300),
        (12, "Leskovac", "16000", 42.9981, 21.9461),
        (13, "Valjevo", "14000", 44.2750, 19.8900),
        (14, "Kruševac", "37000", 43.5800, 21.3339),
        (15, "Vranje", "17500", 42.5514, 21.9000),
        (16, "Šabac", "15000", 44.7489, 19.6908),
        (17, "Užice", "31000", 43.8556, 19.8425),
        (18, "Sombor", "25000", 45.7742, 19.1122),
        (19, "Požarevac", "12000", 44.6213, 21.1878),
        (20, "Pirot", "18300", 43.1531, 22.5861),
        (21, "Zaječar", "19000", 43.9036, 22.2847),
        (22, "Kikinda", "23300", 45.8297, 20.4653),
        (23, "Sremska Mitrovica", "22000", 44.9764, 19.6122),
        (24, "Jagodina", "35000", 43.9772, 21.2611),
        (25, "Vršac", "26300", 45.1167, 21.3036),
        (26, "Bor", "19210", 44.0747, 22.0956),
        (27, "Prokuplje", "18400", 43.2342, 21.5881),
        (28, "Loznica", "15300", 44.5333, 19.2233)
    ];

    public static object[] Gradovi() => ListaGradova
        .Select(g => (object)new
        {
            g.Id,
            g.Naziv,
            PostanskiBroj = g.Pb,
            GeografskaSirina = g.Lat,
            GeografskaDuzina = g.Lng,
            // NetTopologySuite: X = geografska dužina, Y = geografska širina. SRID 4326 = WGS84 (GPS).
            Lokacija = new Point(g.Lng, g.Lat) { SRID = 4326 }
        })
        .ToArray();

    private static readonly (int Id, string Naziv, string Slug, int? Roditelj, string Ikonica, int Redosled)[] ListaKategorija =
    [
        (1, "Elektronika", "elektronika", null, "elektronika", 1),
        (2, "Vozila", "vozila", null, "vozila", 2),
        (3, "Odeća i obuća", "odeca-i-obuca", null, "odeca", 3),
        (4, "Kuća i bašta", "kuca-i-basta", null, "kuca", 4),
        (5, "Sport i rekreacija", "sport-i-rekreacija", null, "sport", 5),
        (6, "Knjige, muzika i filmovi", "knjige-muzika-filmovi", null, "knjige", 6),
        (7, "Retro i kolekcionarstvo", "retro-i-kolekcionarstvo", null, "retro", 7),
        (8, "Bebe i deca", "bebe-i-deca", null, "deca", 8),
        (9, "Kućni ljubimci", "kucni-ljubimci", null, "ljubimci", 9),
        (10, "Ostalo", "ostalo", null, "ostalo", 10),

        (11, "Mobilni telefoni", "mobilni-telefoni", 1, "telefon", 1),
        (12, "Računari i laptopovi", "racunari-i-laptopovi", 1, "racunar", 2),
        (13, "TV i audio", "tv-i-audio", 1, "tv", 3),
        (14, "Foto i video", "foto-i-video", 1, "kamera", 4),

        (21, "Automobili", "automobili", 2, "automobil", 1),
        (22, "Motocikli", "motocikli", 2, "motocikl", 2),
        (23, "Bicikli", "bicikli", 2, "bicikl", 3),
        (24, "Delovi i oprema za vozila", "delovi-i-oprema-za-vozila", 2, "alat", 4),

        (31, "Ženska odeća", "zenska-odeca", 3, "odeca", 1),
        (32, "Muška odeća", "muska-odeca", 3, "odeca", 2),
        (33, "Obuća", "obuca", 3, "obuca", 3),
        (34, "Dečja odeća", "decja-odeca", 3, "odeca", 4),

        (41, "Nameštaj", "namestaj", 4, "namestaj", 1),
        (42, "Kućni aparati", "kucni-aparati", 4, "aparat", 2),
        (43, "Bašta i alat", "basta-i-alat", 4, "alat", 3),

        (51, "Fitnes oprema", "fitnes-oprema", 5, "fitnes", 1),
        (52, "Kampovanje", "kampovanje", 5, "kamp", 2),
        (53, "Zimski sportovi", "zimski-sportovi", 5, "skije", 3),

        (61, "Knjige", "knjige", 6, "knjige", 1),
        (62, "Vinil i CD", "vinil-i-cd", 6, "ploca", 2),
        (63, "Filmovi", "filmovi", 6, "film", 3),

        (71, "Antikviteti", "antikviteti", 7, "retro", 1),
        (72, "Retro konzole i igre", "retro-konzole-i-igre", 7, "konzola", 2),
        (73, "Markice, novčići i nalepnice", "markice-novcici-nalepnice", 7, "novcic", 3),

        (81, "Igračke", "igracke", 8, "igracka", 1),
        (82, "Kolica i oprema", "kolica-i-oprema", 8, "kolica", 2),

        (91, "Oprema za ljubimce", "oprema-za-ljubimce", 9, "ljubimci", 1)
    ];

    public static object[] Kategorije() => ListaKategorija
        .Select(k => (object)new
        {
            k.Id,
            k.Naziv,
            k.Slug,
            RoditeljId = k.Roditelj,
            k.Ikonica,
            k.Redosled,
            Aktivna = true,
            KreiranoUtc = Datum
        })
        .ToArray();

    public static object[] DefinicijeAtributa()
    {
        var id = 0;
        object D(int kategorijaId, string naziv, string kljuc, TipAtributa tip, bool obavezan, string? opcije = null,
            string? jedinica = null, int redosled = 0) =>
            new
            {
                Id = ++id,
                KategorijaId = kategorijaId,
                Naziv = naziv,
                Kljuc = kljuc,
                Tip = tip,
                Obavezan = obavezan,
                Opcije = opcije,
                JedinicaMere = jedinica,
                Redosled = redosled
            };

        return
        [
            // Mobilni telefoni
            D(11, "Marka", "marka", TipAtributa.Tekst, true, redosled: 1),
            D(11, "Memorija", "memorija", TipAtributa.Izbor, false, "32 GB|64 GB|128 GB|256 GB|512 GB|1 TB", redosled: 2),
            D(11, "Boja", "boja", TipAtributa.Tekst, false, redosled: 3),
            // Računari
            D(12, "Procesor", "procesor", TipAtributa.Tekst, false, redosled: 1),
            D(12, "RAM", "ram", TipAtributa.Broj, false, jedinica: "GB", redosled: 2),
            D(12, "Skladište", "skladiste", TipAtributa.Broj, false, jedinica: "GB", redosled: 3),
            // TV
            D(13, "Dijagonala", "dijagonala", TipAtributa.Broj, false, jedinica: "inča", redosled: 1),
            // Automobili
            D(21, "Marka", "marka", TipAtributa.Tekst, true, redosled: 1),
            D(21, "Godište", "godiste", TipAtributa.Broj, true, redosled: 2),
            D(21, "Kilometraža", "kilometraza", TipAtributa.Broj, true, jedinica: "km", redosled: 3),
            D(21, "Gorivo", "gorivo", TipAtributa.Izbor, true, "Benzin|Dizel|Benzin + gas|Električni|Hibrid", redosled: 4),
            D(21, "Menjač", "menjac", TipAtributa.Izbor, false, "Manuelni|Automatski", redosled: 5),
            // Motocikli
            D(22, "Godište", "godiste", TipAtributa.Broj, true, redosled: 1),
            D(22, "Kubikaža", "kubikaza", TipAtributa.Broj, false, jedinica: "cm³", redosled: 2),
            // Bicikli
            D(23, "Veličina rama", "velicina-rama", TipAtributa.Izbor, false, "XS|S|M|L|XL", redosled: 1),
            D(23, "Električni", "elektricni", TipAtributa.DaNe, false, redosled: 2),
            // Odeća
            D(31, "Veličina", "velicina", TipAtributa.Izbor, true, "XS|S|M|L|XL|XXL", redosled: 1),
            D(32, "Veličina", "velicina", TipAtributa.Izbor, true, "XS|S|M|L|XL|XXL", redosled: 1),
            D(33, "Broj", "broj", TipAtributa.Broj, true, redosled: 1),
            D(34, "Uzrast", "uzrast", TipAtributa.Tekst, false, redosled: 1),
            // Nameštaj
            D(41, "Materijal", "materijal", TipAtributa.Tekst, false, redosled: 1),
            // Knjige
            D(61, "Autor", "autor", TipAtributa.Tekst, false, redosled: 1),
            D(61, "Jezik", "jezik", TipAtributa.Izbor, false, "Srpski|Engleski|Ostalo", redosled: 2),
            // Vinil
            D(62, "Izvođač", "izvodjac", TipAtributa.Tekst, false, redosled: 1),
            D(62, "Format", "format", TipAtributa.Izbor, false, "LP|EP|Singl|CD|Kaseta", redosled: 2),
            // Retro konzole
            D(72, "Platforma", "platforma", TipAtributa.Tekst, true, redosled: 1),
            D(72, "Originalna kutija", "originalna-kutija", TipAtributa.DaNe, false, redosled: 2),
            // Antikviteti
            D(71, "Približna godina", "godina", TipAtributa.Broj, false, redosled: 1)
        ];
    }
}
