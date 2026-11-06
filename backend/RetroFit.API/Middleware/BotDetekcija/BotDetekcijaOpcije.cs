namespace RetroFit.API.Middleware.BotDetekcija;

public enum RezimBotDetekcije
{
    /// <summary>Middleware se preskače.</summary>
    Iskljuceno,
    /// <summary>Računa rizik i loguje, ali NIKAD ne blokira (podrazumevano u razvoju).</summary>
    SamoPracenje,
    /// <summary>Puna zaštita: rate limit, JS izazov, privremene i automatske blokade.</summary>
    Zastita
}

/// <summary>Ograničenje broja zahteva u kliznom vremenskom prozoru.</summary>
public sealed class LimitGrupe
{
    public int Zahteva { get; set; }
    public int ProzorSekundi { get; set; }
}

/// <summary>Podešavanja Bot Detection middleware-a (sekcija "BotDetekcija" u appsettings.json).</summary>
public sealed class BotDetekcijaOpcije
{
    public const string Sekcija = "BotDetekcija";

    public RezimBotDetekcije Rezim { get; set; } = RezimBotDetekcije.Zastita;

    /// <summary>Tajni ključ za HMAC (otisci uređaja i potpis JS izazova). Najmanje 32 znaka.</summary>
    public string TajniKljuc { get; set; } = string.Empty;

    /// <summary>IP adrese koje se samo prate, nikad ne blokiraju (npr. interni monitoring).</summary>
    public List<string> PouzdaneIpAdrese { get; set; } = [];

    /// <summary>Od ovog rizika osetljive akcije (prijava, registracija, poruke, izmene) traže rešen JS izazov.</summary>
    public int PragIzazova { get; set; } = 50;

    /// <summary>Od ovog rizika klijent se privremeno blokira.</summary>
    public int PragBlokade { get; set; } = 85;

    public int PrivremenaBlokadaMinuta { get; set; } = 15;

    /// <summary>Posle ovoliko privremenih blokada u 24h, uređaj se automatski blokira u bazi.</summary>
    public int PrivremenihBlokadaZaAutomatsku { get; set; } = 3;

    public int AutomatskaBlokadaSati { get; set; } = 24;

    /// <summary>Registracija UVEK zahteva rešen JS izazov (zaštita od masovnog pravljenja naloga).</summary>
    public bool IzazovZaRegistraciju { get; set; } = true;

    /// <summary>Broj vodećih nula (bitova) u SHA-256 rešenju proof-of-work izazova. 16 = oko 65.000 pokušaja.</summary>
    public int TezinaIzazova { get; set; } = 16;

    public int TrajanjeTokenaIzazovaMinuta { get; set; } = 30;

    /// <summary>
    /// JA3/JA4 TLS otisak se prihvata iz zaglavlja X-TLS-Fingerprint SAMO ako je ovo uključeno i zahtev
    /// stiže od pouzdanog reverse proxy-ja (ASP.NET Core iza proxy-ja ne vidi TLS handshake).
    /// </summary>
    public bool PrihvatiTlsOtisakOdProxyja { get; set; }

    public List<string> PouzdaniProxyji { get; set; } = [];

    /// <summary>Poznati TLS otisci alata za automatizaciju (npr. python-requests, curl).</summary>
    public List<string> SumnjiviTlsOtisci { get; set; } = [];

    /// <summary>Koliko različitih oglasa jedan klijent sme da otvori za 5 minuta pre nego što se posumnja na scraping.</summary>
    public int MaksRazlicitihOglasa5Min { get; set; } = 60;

    /// <summary>Isti tekst poruke u ovoliko RAZLIČITIH razgovora za 10 minuta = spam.</summary>
    public int IstihPorukaZaSpam { get; set; } = 3;

    /// <summary>Izlaže X-Risk-Score zaglavlje i /bezbednost/procena endpoint (samo za razvoj i demonstraciju).</summary>
    public bool PrikaziProcenu { get; set; }

    public Dictionary<string, LimitGrupe> Limiti { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(GrupaRute.Registracija)] = new() { Zahteva = 3, ProzorSekundi = 3600 },
        [nameof(GrupaRute.Prijava)] = new() { Zahteva = 10, ProzorSekundi = 300 },
        [nameof(GrupaRute.Izazov)] = new() { Zahteva = 30, ProzorSekundi = 60 },
        [nameof(GrupaRute.Poruke)] = new() { Zahteva = 20, ProzorSekundi = 60 },
        [nameof(GrupaRute.Pisanje)] = new() { Zahteva = 60, ProzorSekundi = 60 },
        [nameof(GrupaRute.Pretraga)] = new() { Zahteva = 90, ProzorSekundi = 60 },
        [nameof(GrupaRute.PregledOglasa)] = new() { Zahteva = 120, ProzorSekundi = 60 },
        [nameof(GrupaRute.Opste)] = new() { Zahteva = 300, ProzorSekundi = 60 }
    };

    public LimitGrupe Limit(GrupaRute grupa) =>
        Limiti.TryGetValue(grupa.ToString(), out var limit) ? limit : new LimitGrupe { Zahteva = 300, ProzorSekundi = 60 };
}
