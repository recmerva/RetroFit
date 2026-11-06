using System.Text.RegularExpressions;

namespace RetroFit.API.Middleware.BotDetekcija;

/// <summary>Sažetak ponašanja klijenta iz kliznih prozora (računa ga PracenjePonasanja).</summary>
public sealed record SazetakPonasanja
{
    public int ZahtevaUProzoru { get; init; }
    public int LimitZahteva { get; init; }
    public int RazlicitihOglasa5Min { get; init; }
    public int SekvencijalnihPregleda { get; init; }
    public bool RobotskiRitam { get; init; }
    public int IstaPorukaURazgovora { get; init; }
    public int Gresaka404U5Min { get; init; }

    public bool PrekoracenLimit => LimitZahteva > 0 && ZahtevaUProzoru > LimitZahteva;
}

public sealed record UlazAnalize
{
    public string? UserAgent { get; init; }
    public string? AcceptLanguage { get; init; }
    public string? Accept { get; init; }
    public KlijentskiSignali? Signali { get; init; }
    public bool ValidanIzazov { get; init; }
    public bool TlsOtisakSumnjiv { get; init; }
    public SazetakPonasanja Ponasanje { get; init; } = new();
    public int MaksRazlicitihOglasa { get; init; } = 60;
    public int IstihPorukaZaSpam { get; init; } = 3;
}

/// <summary>Rezultat procene: skor 0-100 i ČITLJIVI razlozi (objašnjivo, ne "crna kutija").</summary>
public sealed record ProcenaRizika(int Rizik, IReadOnlyList<string> Razlozi, bool Spam, bool PrekoracenLimit);

/// <summary>
/// Procena rizika da je zahtev automatizovan. Kombinuje STATIČKE signale (zaglavlja) i signale PONAŠANJA.
/// Nijedan signal sam po sebi nije dokaz - npr. IP i User-Agent se lako lažiraju ili dele - zato se
/// poeni sabiraju, a odluka zavisi od zbira i od osetljivosti rute.
///
/// Čista funkcija (bez stanja i I/O), pa se lako testira jediničnim testovima.
/// </summary>
public static partial class AnalizatorRizika
{
    [GeneratedRegex(@"curl|wget|python-requests|python-urllib|aiohttp|httpx|go-http-client|java/|okhttp|libwww|scrapy|httpclient|axios/|node-fetch|headlesschrome|phantomjs|selenium|puppeteer|playwright|\bbot\b|crawler|spider",
        RegexOptions.IgnoreCase)]
    private static partial Regex AlatZaAutomatizaciju();

    public static ProcenaRizika Proceni(UlazAnalize u)
    {
        var rizik = 0;
        var razlozi = new List<string>();

        void Dodaj(int poeni, string razlog)
        {
            rizik += poeni;
            razlozi.Add($"{razlog} (+{poeni})");
        }

        // ---- Statički signali ----
        if (string.IsNullOrWhiteSpace(u.UserAgent))
        {
            Dodaj(40, "Nedostaje User-Agent");
        }
        else if (AlatZaAutomatizaciju().IsMatch(u.UserAgent))
        {
            Dodaj(35, "User-Agent alata za automatizaciju");
        }

        if (string.IsNullOrWhiteSpace(u.AcceptLanguage))
        {
            Dodaj(15, "Nedostaje Accept-Language");
        }

        if (string.IsNullOrWhiteSpace(u.Accept))
        {
            Dodaj(5, "Nedostaje Accept");
        }

        if (u.Signali is null)
        {
            // Naš frontend uvek šalje signale; njihov izostanak znači direktan poziv API-ja.
            Dodaj(10, "Nema klijentskih signala");
        }
        else
        {
            if (u.Signali.Webdriver == true)
            {
                Dodaj(40, "navigator.webdriver = true (automatizovan pregledač)");
            }

            var mobilniUa = u.UserAgent?.Contains("Mobile", StringComparison.OrdinalIgnoreCase) == true;
            if (mobilniUa && u.Signali.SirinaEkrana >= 1920 && u.Signali.Dodir == false)
            {
                Dodaj(10, "Nedoslednost: mobilni User-Agent, desktop ekran bez dodira");
            }

            if (string.IsNullOrWhiteSpace(u.Signali.Zona))
            {
                Dodaj(5, "Nedostaje vremenska zona");
            }
        }

        if (u.TlsOtisakSumnjiv)
        {
            Dodaj(30, "TLS otisak poznatog alata za automatizaciju");
        }

        // ---- Ponašanje ----
        var p = u.Ponasanje;
        if (p.PrekoracenLimit)
        {
            Dodaj(30, $"Prekoračen limit zahteva ({p.ZahtevaUProzoru}/{p.LimitZahteva})");
        }
        else if (p.LimitZahteva > 0 && p.ZahtevaUProzoru > p.LimitZahteva * 0.8)
        {
            Dodaj(10, "Blizu limita zahteva");
        }

        if (p.RazlicitihOglasa5Min > u.MaksRazlicitihOglasa)
        {
            Dodaj(30, $"Scraping: {p.RazlicitihOglasa5Min} različitih oglasa za 5 minuta");
        }

        if (p.SekvencijalnihPregleda >= 8)
        {
            Dodaj(35, "Enumeracija: oglasi se otvaraju redom po ID-ju");
        }

        if (p.RobotskiRitam)
        {
            Dodaj(25, "Robotski ritam: zahtevi u skoro jednakim razmacima");
        }

        var spam = p.IstaPorukaURazgovora >= u.IstihPorukaZaSpam;
        if (spam)
        {
            Dodaj(40, $"Spam: ista poruka u {p.IstaPorukaURazgovora} razgovora");
        }

        if (p.Gresaka404U5Min > 20)
        {
            Dodaj(20, "Mnogo nepostojećih resursa (404)");
        }

        // ---- Umanjenje ----
        if (u.ValidanIzazov && rizik > 0)
        {
            var umanjenje = Math.Min(30, rizik);
            rizik -= umanjenje;
            razlozi.Add($"Rešen JS izazov (-{umanjenje})");
        }

        return new ProcenaRizika(Math.Clamp(rizik, 0, 100), razlozi, spam, p.PrekoracenLimit);
    }
}
