using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RetroFit.Application.Common.Interfejsi;

namespace RetroFit.API.Middleware.BotDetekcija;

public enum OdlukaBotDetekcije
{
    Propusti,
    TraziIzazov,
    OgraniciBrzinu,
    Spam,
    PrivremenoBlokiraj,
    UredjajBlokiran
}

/// <summary>
/// BOT DETECTION / FINGERPRINTING MIDDLEWARE (sopstveni middleware projekta).
///
/// Za svaki API zahtev:
///  1. klasifikuje rutu (registracija, prijava, poruke, pretraga, pregled oglasa...);
///  2. izračunava otisak uređaja (HMAC grubih signala) i ključ klijenta (korisnik ili otisak);
///  3. proverava blokade: trajne (baza, BlokiraniUredjaji) i privremene (memorija);
///  4. beleži zahtev u klizne prozore i računa ponašanje (brzina, scraping, ritam, spam, 404);
///  5. procenjuje rizik 0-100 (AnalizatorRizika) i donosi odluku:
///       propusti | traži JS izazov (428) | ograniči brzinu (429) | privremeno blokiraj (429) | uređaj blokiran (403).
///
/// Ograničenja (iskreno, za odbranu):
///  - IP adresa i User-Agent NISU pouzdani identifikatori uređaja (NAT, VPN, lažiranje) - koriste se samo kao signali;
///  - TLS otisak (JA3/JA4) obična ASP.NET Core aplikacija iza reverse proxy-ja NE VIDI (TLS se završava na proxy-ju).
///    Prihvata se samo ako ga pouzdan proxy prosledi u zaglavlju X-TLS-Fingerprint;
///  - stanje je u memoriji jedne instance (za više instanci: Redis).
///
/// U razvoju radi u režimu "SamoPracenje": računa i loguje sve, ali nikad ne blokira legitimne zahteve.
/// </summary>
public sealed class BotDetekcijaMiddleware(
    RequestDelegate sledeci,
    IOptionsMonitor<BotDetekcijaOpcije> opcije,
    PracenjePonasanja pracenje,
    JsIzazovServis izazovi,
    IDatumVreme vreme,
    ILogger<BotDetekcijaMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext kontekst, IProblemDetailsService problemi)
    {
        var o = opcije.CurrentValue;
        var (grupa, oglasId) = KlasifikacijaRuta.Klasifikuj(kontekst.Request.Method, kontekst.Request.Path);
        if (o.Rezim == RezimBotDetekcije.Iskljuceno || grupa == GrupaRute.Ignorisi)
        {
            await sledeci(kontekst);
            return;
        }

        var sada = vreme.SadaUtc;
        var zahtev = kontekst.Request;
        var ip = kontekst.Connection.RemoteIpAddress;
        var ipTekst = ip?.ToString() ?? "-";
        var userAgent = zahtev.Headers.UserAgent.ToString();
        var jezik = zahtev.Headers.AcceptLanguage.ToString();
        var signali = KlijentskiSignali.Parsiraj(zahtev.Headers[BotDetekcijaKljucevi.ZaglavljeSignala]);
        var tls = TlsOtisak(kontekst, o);

        var otisak = IdentifikacijaKlijenta.Otisak(o.TajniKljuc, userAgent, jezik, signali, tls);
        kontekst.Items[BotDetekcijaKljucevi.KljucOtiska] = otisak;

        var korisnikId = int.TryParse(kontekst.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var kid) ? kid : (int?)null;
        var kljuc = korisnikId is not null ? $"k:{korisnikId}" : $"o:{otisak}";
        var zastita = o.Rezim == RezimBotDetekcije.Zastita && !o.PouzdaneIpAdrese.Contains(ipTekst);

        // ---- 1) Trajna blokada uređaja (iz baze) ----
        if (pracenje.JeOtisakBlokiran(otisak))
        {
            Zabelezi(OdlukaBotDetekcije.UredjajBlokiran, 100, ["Uređaj je blokiran"], grupa, kontekst, otisak, ip, korisnikId, kljuc, sada, userAgent);
            if (zastita)
            {
                await OdgovoriAsync(kontekst, problemi, StatusCodes.Status403Forbidden, "uredjaj_blokiran", "Pristup blokiran",
                    "Pristup sa ovog uređaja je blokiran zbog sumnjive aktivnosti.", null);
                return;
            }
        }

        // ---- 2) Privremena blokada (memorija) ----
        var blokiranDo = pracenje.PrivremenoBlokiranDo(kljuc, sada) ?? pracenje.PrivremenoBlokiranDo($"ip:{ipTekst}", sada);
        if (blokiranDo is not null && zastita)
        {
            await OdgovoriAsync(kontekst, problemi, StatusCodes.Status429TooManyRequests, "privremeno_blokirano", "Privremeno blokirano",
                "Previše sumnjivih zahteva. Pokušajte ponovo kasnije.", blokiranDo.Value - sada);
            return;
        }

        // ---- 3) Ponašanje ----
        var limit = o.Limit(grupa);
        var ponasanje = pracenje.Zabelezi(kljuc, grupa, limit, oglasId, sada);
        if (grupa is GrupaRute.Prijava or GrupaRute.Registracija)
        {
            // Dodatni limit po IP adresi (x3 zbog NAT-a), da napadač ne zaobiđe limit menjanjem otiska.
            var poIp = pracenje.Zabelezi($"ip:{ipTekst}", grupa,
                new LimitGrupe { Zahteva = limit.Zahteva * 3, ProzorSekundi = limit.ProzorSekundi }, null, sada);
            if (poIp.PrekoracenLimit)
            {
                ponasanje = ponasanje with { ZahtevaUProzoru = poIp.ZahtevaUProzoru, LimitZahteva = poIp.LimitZahteva };
            }
        }

        if (grupa == GrupaRute.Poruke)
        {
            var (sadrzaj, razgovor) = await ProcitajPorukuAsync(kontekst);
            if (sadrzaj is not null)
            {
                ponasanje = ponasanje with { IstaPorukaURazgovora = pracenje.ZabeleziPoruku(kljuc, sadrzaj, razgovor, sada) };
            }
        }

        // ---- 4) Procena rizika ----
        var validanIzazov = izazovi.ValidanToken(zahtev.Headers[BotDetekcijaKljucevi.ZaglavljeIzazova], otisak, sada);
        var procena = AnalizatorRizika.Proceni(new UlazAnalize
        {
            UserAgent = userAgent,
            AcceptLanguage = jezik,
            Accept = zahtev.Headers.Accept.ToString(),
            Signali = signali,
            ValidanIzazov = validanIzazov,
            TlsOtisakSumnjiv = tls is not null && o.SumnjiviTlsOtisci.Contains(tls, StringComparer.OrdinalIgnoreCase),
            Ponasanje = ponasanje,
            MaksRazlicitihOglasa = o.MaksRazlicitihOglasa5Min,
            IstihPorukaZaSpam = o.IstihPorukaZaSpam
        });

        kontekst.Items[BotDetekcijaKljucevi.KljucRizika] = procena;
        pracenje.AzurirajStatistiku(otisak, procena.Rizik, korisnikId, IdentifikacijaKlijenta.OpisKlijenta(userAgent));
        if (o.PrikaziProcenu)
        {
            kontekst.Response.Headers[BotDetekcijaKljucevi.ZaglavljeRizika] = procena.Rizik.ToString();
        }

        // ---- 5) Odluka ----
        var odluka = Odluci(o, grupa, procena, validanIzazov);
        if (odluka != OdlukaBotDetekcije.Propusti || procena.Rizik >= 30)
        {
            Zabelezi(odluka, procena.Rizik, procena.Razlozi, grupa, kontekst, otisak, ip, korisnikId, kljuc, sada, userAgent, zastita);
        }

        if (zastita && odluka != OdlukaBotDetekcije.Propusti)
        {
            await PrimeniOdlukuAsync(kontekst, problemi, o, odluka, kljuc, limit, sada, otisak, ip, korisnikId, procena, grupa, userAgent);
            return;
        }

        await sledeci(kontekst);

        if (kontekst.Response.StatusCode == StatusCodes.Status404NotFound)
        {
            pracenje.Zabelezi404(kljuc, sada);
        }
    }

    /// <summary>Pravila odlučivanja (javna radi jediničnih testova).</summary>
    public static OdlukaBotDetekcije Odluci(BotDetekcijaOpcije o, GrupaRute grupa, ProcenaRizika procena, bool validanIzazov)
    {
        if (procena.Spam)
        {
            return OdlukaBotDetekcije.Spam;
        }

        if (procena.Rizik >= o.PragBlokade)
        {
            return OdlukaBotDetekcije.PrivremenoBlokiraj;
        }

        if (procena.PrekoracenLimit)
        {
            return OdlukaBotDetekcije.OgraniciBrzinu;
        }

        if (!validanIzazov && grupa == GrupaRute.Registracija && o.IzazovZaRegistraciju)
        {
            return OdlukaBotDetekcije.TraziIzazov;
        }

        if (!validanIzazov && KlasifikacijaRuta.JeOsetljiva(grupa) && procena.Rizik >= o.PragIzazova)
        {
            return OdlukaBotDetekcije.TraziIzazov;
        }

        return OdlukaBotDetekcije.Propusti;
    }

    private async Task PrimeniOdlukuAsync(HttpContext kontekst, IProblemDetailsService problemi, BotDetekcijaOpcije o, OdlukaBotDetekcije odluka,
        string kljuc, LimitGrupe limit, DateTime sada, string otisak, System.Net.IPAddress? ip, int? korisnikId, ProcenaRizika procena,
        GrupaRute grupa, string userAgent)
    {
        switch (odluka)
        {
            case OdlukaBotDetekcije.TraziIzazov:
                await OdgovoriAsync(kontekst, problemi, StatusCodes.Status428PreconditionRequired, "potreban_izazov", "Potrebna provera",
                    "Za ovu akciju potrebno je rešiti bezbednosnu proveru (JavaScript izazov).", null,
                    ("izazov", "/api/v1/bezbednost/izazov"));
                break;

            case OdlukaBotDetekcije.OgraniciBrzinu:
                await OdgovoriAsync(kontekst, problemi, StatusCodes.Status429TooManyRequests, "previse_zahteva", "Previše zahteva",
                    "Poslali ste previše zahteva u kratkom vremenu. Sačekajte malo i pokušajte ponovo.", TimeSpan.FromSeconds(limit.ProzorSekundi));
                break;

            case OdlukaBotDetekcije.Spam:
            case OdlukaBotDetekcije.PrivremenoBlokiraj:
                var trajanje = TimeSpan.FromMinutes(o.PrivremenaBlokadaMinuta);
                var brojBlokada = pracenje.BlokirajPrivremeno(kljuc, sada, trajanje);
                if (brojBlokada >= o.PrivremenihBlokadaZaAutomatsku)
                {
                    // Ponavljanje: uređaj se automatski blokira u bazi (BlokiraniUredjaji), upis radi pozadinski servis.
                    pracenje.DodajDogadjaj(new BezbednosniDogadjaj(otisak, "AutomatskaBlokadaUredjaja", procena.Rizik, procena.Razlozi,
                        $"{kontekst.Request.Method} {kontekst.Request.Path}", IdentifikacijaKlijenta.MaskirajIp(ip), korisnikId, true,
                        IdentifikacijaKlijenta.OpisKlijenta(userAgent), sada));
                }

                var spam = odluka == OdlukaBotDetekcije.Spam;
                await OdgovoriAsync(kontekst, problemi, StatusCodes.Status429TooManyRequests, spam ? "spam" : "privremeno_blokirano",
                    spam ? "Poruka odbijena" : "Privremeno blokirano",
                    spam ? "Ista poruka je poslata u više razgovora. Slanje poruka je privremeno onemogućeno."
                         : "Detektovana je sumnjiva aktivnost. Pristup je privremeno ograničen.",
                    trajanje);
                break;
        }
    }

    private void Zabelezi(OdlukaBotDetekcije odluka, int rizik, IReadOnlyList<string> razlozi, GrupaRute grupa, HttpContext kontekst,
        string otisak, System.Net.IPAddress? ip, int? korisnikId, string kljuc, DateTime sada, string userAgent, bool zastita = true)
    {
        var maskiranaIp = IdentifikacijaKlijenta.MaskirajIp(ip);
        // Strukturisani log (Serilog): svako svojstvo je posebno polje, pa se lako filtrira i pretražuje.
        logger.LogWarning(
            "Bot detekcija: {Odluka}{Rezim} | grupa {Grupa} | rizik {Rizik} | otisak {Otisak} | IP {Ip} | korisnik {KorisnikId} | razlozi: {Razlozi}",
            odluka, zastita ? "" : " (samo praćenje)", grupa, rizik, otisak[..12], maskiranaIp, korisnikId, string.Join("; ", razlozi));

        if (odluka != OdlukaBotDetekcije.Propusti && pracenje.TrebaAudit(kljuc, sada))
        {
            pracenje.DodajDogadjaj(new BezbednosniDogadjaj(otisak, $"BotDetekcija:{odluka}", rizik, razlozi,
                $"{kontekst.Request.Method} {kontekst.Request.Path}", maskiranaIp, korisnikId, false,
                IdentifikacijaKlijenta.OpisKlijenta(userAgent), sada));
        }
    }

    /// <summary>
    /// TLS otisak se prihvata SAMO od pouzdanog proxy-ja. ForwardedHeaders middleware originalnu adresu
    /// neposrednog pošiljaoca (proxy-ja) čuva u zaglavlju X-Original-For.
    /// </summary>
    private string? TlsOtisak(HttpContext kontekst, BotDetekcijaOpcije o)
    {
        var vrednost = kontekst.Request.Headers[BotDetekcijaKljucevi.ZaglavljeTlsOtiska].ToString();
        if (string.IsNullOrWhiteSpace(vrednost))
        {
            return null;
        }

        var proxy = kontekst.Request.Headers["X-Original-For"].ToString().Split(':')[0];
        if (!o.PrihvatiTlsOtisakOdProxyja || !o.PouzdaniProxyji.Contains(proxy))
        {
            logger.LogWarning("Zaglavlje {Zaglavlje} stiglo od nepouzdanog izvora - zanemareno.", BotDetekcijaKljucevi.ZaglavljeTlsOtiska);
            return null;
        }

        return vrednost.Length <= 100 ? vrednost : vrednost[..100];
    }

    /// <summary>
    /// Čita tekst poruke iz tela zahteva (bez trošenja tela za kontroler - EnableBuffering).
    /// Brze poruke se ne računaju u spam (namenjene su za slanje mnogim prodavcima).
    /// </summary>
    private static async Task<(string? Sadrzaj, int Razgovor)> ProcitajPorukuAsync(HttpContext kontekst)
    {
        var zahtev = kontekst.Request;
        if (zahtev.ContentLength is null or > 16_384 || zahtev.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) != true)
        {
            return (null, 0);
        }

        zahtev.EnableBuffering();
        try
        {
            using var dokument = await JsonDocument.ParseAsync(zahtev.Body, new JsonDocumentOptions { MaxDepth = 4 });
            var koren = dokument.RootElement;
            string? Polje(string naziv) => koren.EnumerateObject()
                .FirstOrDefault(p => p.Name.Equals(naziv, StringComparison.OrdinalIgnoreCase)).Value is { ValueKind: JsonValueKind.String } v
                ? v.GetString()
                : null;
            int Broj(string naziv) => koren.EnumerateObject()
                .FirstOrDefault(p => p.Name.Equals(naziv, StringComparison.OrdinalIgnoreCase)).Value is { ValueKind: JsonValueKind.Number } v
                ? v.GetInt32()
                : 0;

            var sadrzaj = Polje("sadrzaj");
            if (string.IsNullOrWhiteSpace(sadrzaj) || sadrzaj.Length < 10)
            {
                return (null, 0);
            }

            // Za /razgovori/{id}/poruke razgovor je u putanji; za POST /razgovori je to oglas iz tela (negativan ključ).
            var delovi = zahtev.Path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var razgovor = delovi.Length >= 5 && int.TryParse(delovi[3], out var id) ? id : -Broj("oglasId");
            return (sadrzaj, razgovor);
        }
        catch (JsonException)
        {
            return (null, 0);
        }
        finally
        {
            zahtev.Body.Position = 0;
        }
    }

    private static async Task OdgovoriAsync(HttpContext kontekst, IProblemDetailsService problemi, int status, string kod, string naslov,
        string detalj, TimeSpan? retryAfter, params (string Kljuc, object Vrednost)[] dodatno)
    {
        kontekst.Response.StatusCode = status;
        if (retryAfter is not null)
        {
            kontekst.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.Value.TotalSeconds)).ToString();
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = naslov,
            Detail = detalj,
            Type = $"https://retrofit.rs/greske/{kod}",
            Instance = kontekst.Request.Path
        };
        problem.Extensions["kod"] = kod;
        foreach (var (k, v) in dodatno)
        {
            problem.Extensions[k] = v;
        }

        await problemi.WriteAsync(new ProblemDetailsContext { HttpContext = kontekst, ProblemDetails = problem });
    }
}
