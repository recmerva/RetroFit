using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace RetroFit.API.Middleware.BotDetekcija;

/// <summary>Stanje jednog klijenta u memoriji (klizni prozori). Pristup je zaključan po klijentu.</summary>
internal sealed class StanjeKlijenta
{
    public readonly object Brava = new();
    public readonly Dictionary<GrupaRute, Queue<DateTime>> PoGrupi = [];
    public readonly Queue<DateTime> SviZahtevi = new();
    public readonly Queue<(DateTime Vreme, int OglasId)> Oglasi = new();
    public readonly Queue<(DateTime Vreme, string Hash, int Razgovor)> Poruke = new();
    public readonly Queue<DateTime> Greske404 = new();
    public readonly Queue<DateTime> Blokade = new();
    public DateTime? BlokiranDo;
    public DateTime PoslednjiAudit = DateTime.MinValue;
}

/// <summary>Statistika za periodičan upis u tabelu OtisciUredjaja.</summary>
internal sealed record StatistikaOtiska(long NoviZahtevi, int NajveciRizik, int? KorisnikId, string? OpisKlijenta);

/// <summary>Događaj koji treba upisati u bazu (audit ili automatska blokada).</summary>
internal sealed record BezbednosniDogadjaj(string Otisak, string Akcija, int Rizik, IReadOnlyList<string> Razlozi, string Ruta,
    string Ip, int? KorisnikId, bool AutomatskaBlokada, string? OpisKlijenta, DateTime Vreme);

/// <summary>
/// Praćenje ponašanja klijenata u memoriji: brzina zahteva po grupi ruta, otvoreni oglasi (scraping),
/// ritam zahteva, ponovljene poruke (spam), 404 greške i privremene blokade.
///
/// Stanje je u memoriji jedne instance API-ja (MemoryCache sa ograničenjem veličine i kliznim istekom).
/// Kod više instanci iza load balancer-a ovo bi se premestilo u Redis - interfejs ostaje isti.
/// U bazu se NE piše na svakom zahtevu: statistika se skuplja ovde, a BotDetekcijaSinhronizacija je
/// upisuje periodično (batch), da zaštita ne bi postala usko grlo.
/// </summary>
public sealed class PracenjePonasanja : IDisposable
{
    private readonly MemoryCache _stanja = new(new MemoryCacheOptions { SizeLimit = 100_000 });
    private readonly ConcurrentDictionary<string, StatistikaOtiska> _statistika = new();
    private readonly ConcurrentQueue<BezbednosniDogadjaj> _dogadjaji = new();
    private volatile HashSet<string> _blokiraniOtisci = [];

    private StanjeKlijenta Stanje(string kljuc) =>
        _stanja.GetOrCreate(kljuc, e =>
        {
            e.SetSize(1).SetSlidingExpiration(TimeSpan.FromMinutes(30));
            return new StanjeKlijenta();
        })!;

    // ---------------- Blokade ----------------

    public bool JeOtisakBlokiran(string otisak) => _blokiraniOtisci.Contains(otisak);

    internal void PostaviBlokiraneOtiske(IEnumerable<string> otisci) => _blokiraniOtisci = otisci.ToHashSet();

    public DateTime? PrivremenoBlokiranDo(string kljuc, DateTime sada)
    {
        var s = Stanje(kljuc);
        lock (s.Brava)
        {
            return s.BlokiranDo > sada ? s.BlokiranDo : null;
        }
    }

    /// <summary>Privremena blokada; vraća broj blokada u poslednja 24h (za odluku o automatskoj trajnijoj blokadi).</summary>
    public int BlokirajPrivremeno(string kljuc, DateTime sada, TimeSpan trajanje)
    {
        var s = Stanje(kljuc);
        lock (s.Brava)
        {
            s.BlokiranDo = sada + trajanje;
            s.Blokade.Enqueue(sada);
            Ocisti(s.Blokade, sada - TimeSpan.FromHours(24));
            return s.Blokade.Count;
        }
    }

    // ---------------- Beleženje zahteva ----------------

    /// <summary>Beleži zahtev i vraća sažetak ponašanja za analizator rizika.</summary>
    public SazetakPonasanja Zabelezi(string kljuc, GrupaRute grupa, LimitGrupe limit, int? oglasId, DateTime sada)
    {
        var s = Stanje(kljuc);
        lock (s.Brava)
        {
            if (!s.PoGrupi.TryGetValue(grupa, out var red))
            {
                s.PoGrupi[grupa] = red = new Queue<DateTime>();
            }

            red.Enqueue(sada);
            Ocisti(red, sada - TimeSpan.FromSeconds(limit.ProzorSekundi));

            s.SviZahtevi.Enqueue(sada);
            while (s.SviZahtevi.Count > 30)
            {
                s.SviZahtevi.Dequeue();
            }

            if (oglasId is not null)
            {
                s.Oglasi.Enqueue((sada, oglasId.Value));
            }

            while (s.Oglasi.Count > 0 && s.Oglasi.Peek().Vreme < sada.AddMinutes(-5))
            {
                s.Oglasi.Dequeue();
            }

            Ocisti(s.Greske404, sada.AddMinutes(-5));
            while (s.Poruke.Count > 0 && s.Poruke.Peek().Vreme < sada.AddMinutes(-10))
            {
                s.Poruke.Dequeue();
            }

            return new SazetakPonasanja
            {
                ZahtevaUProzoru = red.Count,
                LimitZahteva = limit.Zahteva,
                RazlicitihOglasa5Min = s.Oglasi.Select(o => o.OglasId).Distinct().Count(),
                SekvencijalnihPregleda = Sekvencijalnih(s.Oglasi.Select(o => o.OglasId).TakeLast(15).ToList()),
                RobotskiRitam = RobotskiRitam(s.SviZahtevi.ToList()),
                IstaPorukaURazgovora = 0,
                Gresaka404U5Min = s.Greske404.Count
            };
        }
    }

    /// <summary>Beleži sadržaj poruke (samo heš normalizovanog teksta) i vraća u koliko RAZLIČITIH razgovora je poslat isti tekst.</summary>
    public int ZabeleziPoruku(string kljuc, string sadrzaj, int razgovorId, DateTime sada)
    {
        var hash = HesPoruke(sadrzaj);
        var s = Stanje(kljuc);
        lock (s.Brava)
        {
            s.Poruke.Enqueue((sada, hash, razgovorId));
            return s.Poruke.Where(p => p.Hash == hash).Select(p => p.Razgovor).Distinct().Count();
        }
    }

    public void Zabelezi404(string kljuc, DateTime sada)
    {
        var s = Stanje(kljuc);
        lock (s.Brava)
        {
            s.Greske404.Enqueue(sada);
        }
    }

    /// <summary>Da li za ovog klijenta treba upisati audit (najviše jednom u 5 minuta - zaštita baze od preplavljivanja).</summary>
    public bool TrebaAudit(string kljuc, DateTime sada)
    {
        var s = Stanje(kljuc);
        lock (s.Brava)
        {
            if (sada - s.PoslednjiAudit < TimeSpan.FromMinutes(5))
            {
                return false;
            }

            s.PoslednjiAudit = sada;
            return true;
        }
    }

    // ---------------- Statistika i događaji za bazu ----------------

    public void AzurirajStatistiku(string otisak, int rizik, int? korisnikId, string opisKlijenta) =>
        _statistika.AddOrUpdate(otisak,
            _ => new StatistikaOtiska(1, rizik, korisnikId, opisKlijenta),
            (_, st) => new StatistikaOtiska(st.NoviZahtevi + 1, Math.Max(st.NajveciRizik, rizik), korisnikId ?? st.KorisnikId, opisKlijenta));

    internal IReadOnlyList<(string Otisak, StatistikaOtiska Statistika)> PreuzmiStatistiku()
    {
        var rezultat = new List<(string, StatistikaOtiska)>();
        foreach (var kljuc in _statistika.Keys)
        {
            if (_statistika.TryRemove(kljuc, out var st))
            {
                rezultat.Add((kljuc, st));
            }
        }

        return rezultat;
    }

    internal void DodajDogadjaj(BezbednosniDogadjaj d)
    {
        if (_dogadjaji.Count < 10_000)
        {
            _dogadjaji.Enqueue(d);
        }
    }

    internal IReadOnlyList<BezbednosniDogadjaj> PreuzmiDogadjaje()
    {
        var lista = new List<BezbednosniDogadjaj>();
        while (lista.Count < 500 && _dogadjaji.TryDequeue(out var d))
        {
            lista.Add(d);
        }

        return lista;
    }

    // ---------------- Pomoćne (internal radi testova) ----------------

    /// <summary>Broj uzastopnih parova ID, ID+1 (otvaranje oglasa "redom", tipično za skriptu koja enumeriše).</summary>
    internal static int Sekvencijalnih(IReadOnlyList<int> id)
    {
        var najduze = 0;
        var trenutno = 0;
        for (var i = 1; i < id.Count; i++)
        {
            trenutno = id[i] == id[i - 1] + 1 ? trenutno + 1 : 0;
            najduze = Math.Max(najduze, trenutno);
        }

        return najduze;
    }

    /// <summary>
    /// Robotski ritam: bar 15 zahteva, prosečan razmak ispod 5 s i koeficijent varijacije (std/prosek) ispod 0.15.
    /// Ljudi klikću neravnomerno; skripta sa "sleep(1)" pravi skoro savršeno jednake razmake.
    /// </summary>
    internal static bool RobotskiRitam(IReadOnlyList<DateTime> vremena)
    {
        if (vremena.Count < 15)
        {
            return false;
        }

        var razmaci = vremena.Zip(vremena.Skip(1), (a, b) => (b - a).TotalMilliseconds).ToList();
        var prosek = razmaci.Average();
        if (prosek is <= 50 or >= 5000)
        {
            return false;
        }

        var std = Math.Sqrt(razmaci.Sum(r => (r - prosek) * (r - prosek)) / razmaci.Count);
        return std / prosek < 0.15;
    }

    internal static string HesPoruke(string sadrzaj)
    {
        var normalizovano = new string(sadrzaj.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizovano)))[..16];
    }

    private static void Ocisti(Queue<DateTime> red, DateTime granica)
    {
        while (red.Count > 0 && red.Peek() < granica)
        {
            red.Dequeue();
        }
    }

    public void Dispose() => _stanja.Dispose();
}
