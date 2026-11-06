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

    /// <summary>
    /// Sedišta svih 145 jedinica lokalne samouprave u Srbiji (28 gradova + 117 opština, bez Kosova)
    /// i veća prigradska mesta Beograda. Id 1-28 su gradovi iz prve verzije (na njih se oslanjaju demo podaci),
    /// ostala mesta su grupisana po upravnim okruzima.
    /// </summary>
    private static readonly (int Id, string Naziv, string Pb, double Lat, double Lng)[] ListaGradova =
    [
        // ---------- Gradovi ----------
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
        (28, "Loznica", "15300", 44.5333, 19.2233),

        // ---------- Grad Beograd (prigradska mesta) ----------
        (29, "Barajevo", "11460", 44.5786, 20.4161),
        (30, "Grocka", "11306", 44.6714, 20.7172),
        (31, "Lazarevac", "11550", 44.3803, 20.2564),
        (32, "Mladenovac", "11400", 44.4381, 20.6969),
        (33, "Obrenovac", "11500", 44.6550, 20.2000),
        (34, "Sopot", "11450", 44.5194, 20.5753),
        (35, "Surčin", "11271", 44.7931, 20.2803),

        // ---------- Severnobački okrug ----------
        (36, "Bačka Topola", "24300", 45.8150, 19.6350),
        (37, "Mali Iđoš", "24321", 45.7072, 19.6650),

        // ---------- Srednjobanatski okrug ----------
        (38, "Novi Bečej", "23272", 45.5967, 20.1356),
        (39, "Nova Crnja", "23218", 45.6683, 20.6053),
        (40, "Žitište", "23210", 45.4850, 20.5500),
        (41, "Sečanj", "23240", 45.3669, 20.7725),

        // ---------- Severnobanatski okrug ----------
        (42, "Ada", "24430", 45.8028, 20.1289),
        (43, "Senta", "24400", 45.9275, 20.0772),
        (44, "Kanjiža", "24420", 46.0667, 20.0500),
        (45, "Novi Kneževac", "23330", 46.0500, 20.1000),
        (46, "Čoka", "23320", 45.9422, 20.1433),

        // ---------- Južnobanatski okrug ----------
        (47, "Bela Crkva", "26340", 44.8975, 21.4172),
        (48, "Kovačica", "26210", 45.1117, 20.6214),
        (49, "Kovin", "26220", 44.7475, 20.9761),
        (50, "Alibunar", "26310", 45.0808, 20.9658),
        (51, "Opovo", "26204", 45.0522, 20.4303),
        (52, "Plandište", "26360", 45.2269, 21.1217),

        // ---------- Zapadnobački okrug ----------
        (53, "Apatin", "25260", 45.6711, 18.9850),
        (54, "Kula", "25230", 45.6083, 19.5272),
        (55, "Odžaci", "25250", 45.5083, 19.2611),

        // ---------- Južnobački okrug ----------
        (56, "Bač", "21420", 45.3919, 19.2383),
        (57, "Bačka Palanka", "21400", 45.2500, 19.3917),
        (58, "Bački Petrovac", "21470", 45.3600, 19.5917),
        (59, "Bečej", "21220", 45.6167, 20.0333),
        (60, "Beočin", "21300", 45.2083, 19.7208),
        (61, "Vrbas", "21460", 45.5714, 19.6408),
        (62, "Žabalj", "21230", 45.3722, 20.0639),
        (63, "Srbobran", "21480", 45.5486, 19.7917),
        (64, "Sremski Karlovci", "21205", 45.2025, 19.9342),
        (65, "Temerin", "21235", 45.4083, 19.8867),
        (66, "Titel", "21240", 45.2061, 20.2944),

        // ---------- Sremski okrug ----------
        (67, "Šid", "22240", 45.1283, 19.2264),
        (68, "Inđija", "22320", 45.0481, 20.0817),
        (69, "Irig", "22406", 45.1006, 19.8583),
        (70, "Ruma", "22400", 45.0081, 19.8222),
        (71, "Stara Pazova", "22300", 44.9850, 20.1608),
        (72, "Pećinci", "22410", 44.9089, 19.9656),

        // ---------- Mačvanski okrug ----------
        (73, "Bogatić", "15350", 44.8375, 19.4808),
        (74, "Vladimirci", "15225", 44.6136, 19.7856),
        (75, "Koceljeva", "15220", 44.4708, 19.8236),
        (76, "Mali Zvornik", "15318", 44.3733, 19.1067),
        (77, "Krupanj", "15314", 44.3667, 19.3622),
        (78, "Ljubovija", "15320", 44.1872, 19.3722),

        // ---------- Kolubarski okrug ----------
        (79, "Osečina", "14253", 44.3728, 19.6014),
        (80, "Ub", "14210", 44.4561, 20.0739),
        (81, "Lajkovac", "14224", 44.3711, 20.1650),
        (82, "Mionica", "14242", 44.2522, 20.0819),
        (83, "Ljig", "14240", 44.2264, 20.2394),

        // ---------- Podunavski okrug ----------
        (84, "Smederevska Palanka", "11420", 44.3653, 20.9589),
        (85, "Velika Plana", "11320", 44.3336, 21.0756),

        // ---------- Braničevski okrug ----------
        (86, "Veliko Gradište", "12220", 44.7639, 21.5164),
        (87, "Golubac", "12223", 44.6531, 21.6317),
        (88, "Malo Crniće", "12311", 44.5622, 21.2889),
        (89, "Žabari", "12374", 44.3561, 21.2147),
        (90, "Petrovac na Mlavi", "12300", 44.3786, 21.4192),
        (91, "Kučevo", "12240", 44.4794, 21.6714),
        (92, "Žagubica", "12320", 44.1978, 21.7894),

        // ---------- Šumadijski okrug ----------
        (93, "Aranđelovac", "34300", 44.3069, 20.5600),
        (94, "Topola", "34310", 44.2544, 20.6825),
        (95, "Rača", "34210", 44.2275, 20.9839),
        (96, "Batočina", "34227", 44.1531, 21.0811),
        (97, "Knić", "34240", 43.9233, 20.7186),
        (98, "Lapovo", "34220", 44.1842, 21.1003),

        // ---------- Pomoravski okrug ----------
        (99, "Ćuprija", "35230", 43.9275, 21.3700),
        (100, "Paraćin", "35250", 43.8608, 21.4078),
        (101, "Svilajnac", "35210", 44.2342, 21.1947),
        (102, "Despotovac", "35213", 44.0936, 21.4425),
        (103, "Rekovac", "35260", 43.8628, 21.0939),

        // ---------- Borski okrug ----------
        (104, "Kladovo", "19320", 44.6061, 22.6142),
        (105, "Majdanpek", "19250", 44.4214, 21.9356),
        (106, "Negotin", "19300", 44.2264, 22.5308),

        // ---------- Zaječarski okrug ----------
        (107, "Boljevac", "19370", 43.8303, 21.9514),
        (108, "Knjaževac", "19350", 43.5664, 22.2575),
        (109, "Sokobanja", "18230", 43.6431, 21.8711),

        // ---------- Zlatiborski okrug ----------
        (110, "Bajina Bašta", "31250", 43.9708, 19.5675),
        (111, "Kosjerić", "31260", 44.0000, 19.9083),
        (112, "Požega", "31210", 43.8461, 20.0364),
        (113, "Čajetina", "31310", 43.7497, 19.7156),
        (114, "Arilje", "31230", 43.7531, 20.0956),
        (115, "Priboj", "31330", 43.5836, 19.5258),
        (116, "Nova Varoš", "31320", 43.4603, 19.8131),
        (117, "Prijepolje", "31300", 43.3897, 19.6489),
        (118, "Sjenica", "36310", 43.2731, 20.0003),

        // ---------- Moravički okrug ----------
        (119, "Gornji Milanovac", "32300", 44.0261, 20.4614),
        (120, "Lučani", "32240", 43.8608, 20.1381),
        (121, "Ivanjica", "32250", 43.5797, 20.2292),

        // ---------- Raški okrug ----------
        (122, "Vrnjačka Banja", "36210", 43.6236, 20.8933),
        (123, "Raška", "36350", 43.2861, 20.6136),
        (124, "Tutin", "36320", 42.9908, 20.3372),

        // ---------- Rasinski okrug ----------
        (125, "Varvarin", "37260", 43.7231, 21.3664),
        (126, "Trstenik", "37240", 43.6167, 21.0025),
        (127, "Ćićevac", "37210", 43.7192, 21.4419),
        (128, "Aleksandrovac", "37230", 43.4586, 21.0472),
        (129, "Brus", "37220", 43.3836, 21.0331),

        // ---------- Nišavski okrug ----------
        (130, "Aleksinac", "18220", 43.5417, 21.7069),
        (131, "Svrljig", "18360", 43.4161, 22.1236),
        (132, "Merošina", "18252", 43.2833, 21.7222),
        (133, "Ražanj", "37215", 43.6739, 21.5486),
        (134, "Doljevac", "18410", 43.1967, 21.8328),
        (135, "Gadžin Han", "18240", 43.2236, 22.0333),

        // ---------- Toplički okrug ----------
        (136, "Blace", "18420", 43.2958, 21.2853),
        (137, "Kuršumlija", "18430", 43.1406, 21.2711),
        (138, "Žitorađa", "18412", 43.1847, 21.7136),

        // ---------- Pirotski okrug ----------
        (139, "Bela Palanka", "18310", 43.2178, 22.3083),
        (140, "Babušnica", "18330", 43.0675, 22.4114),
        (141, "Dimitrovgrad", "18320", 43.0161, 22.7756),

        // ---------- Jablanički okrug ----------
        (142, "Bojnik", "16205", 43.0142, 21.7186),
        (143, "Lebane", "16230", 42.9167, 21.7361),
        (144, "Medveđa", "16240", 42.8433, 21.5853),
        (145, "Vlasotince", "16210", 42.9658, 22.1289),
        (146, "Crna Trava", "16215", 42.8100, 22.2989),

        // ---------- Pčinjski okrug ----------
        (147, "Vladičin Han", "17510", 42.7072, 22.0664),
        (148, "Surdulica", "17530", 42.6908, 22.1700),
        (149, "Bosilegrad", "17540", 42.5000, 22.4728),
        (150, "Trgovište", "17525", 42.3511, 22.0856),
        (151, "Bujanovac", "17520", 42.4592, 21.7667),
        (152, "Preševo", "17523", 42.3092, 21.6497)
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
