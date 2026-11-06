using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RetroFit.API.Middleware.BotDetekcija;

/// <summary>
/// Grubi signali koje frontend šalje u zaglavlju X-Client-Signals (JSON). Namerno su GRUBI
/// (rezolucija, vremenska zona, jezik, platforma) - dovoljni da razlikuju klasu uređaja, a nedovoljni
/// za precizno praćenje osobe. Ne koriste se canvas/audio/font otisci (agresivno praćenje).
/// </summary>
public sealed record KlijentskiSignali
{
    /// <summary>Rezolucija ekrana, npr. "1920x1080".</summary>
    public string? Ekran { get; init; }
    /// <summary>IANA vremenska zona, npr. "Europe/Belgrade".</summary>
    public string? Zona { get; init; }
    public string? Jezik { get; init; }
    public string? Platforma { get; init; }
    public double? GustinaPiksela { get; init; }
    public bool? Dodir { get; init; }
    /// <summary>navigator.webdriver - true kod Selenium/Puppeteer/Playwright automatizacije.</summary>
    public bool? Webdriver { get; init; }

    private static readonly JsonSerializerOptions Opcije = new() { PropertyNameCaseInsensitive = true, MaxDepth = 2 };

    public static KlijentskiSignali? Parsiraj(string? zaglavlje)
    {
        if (string.IsNullOrWhiteSpace(zaglavlje) || zaglavlje.Length > 512)
        {
            return null;
        }

        try
        {
            var s = JsonSerializer.Deserialize<KlijentskiSignali>(zaglavlje, Opcije);
            return s is null ? null : s with
            {
                Ekran = Skrati(s.Ekran, 20),
                Zona = Skrati(s.Zona, 60),
                Jezik = Skrati(s.Jezik, 20),
                Platforma = Skrati(s.Platforma, 40)
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public int? SirinaEkrana =>
        Ekran is not null && Ekran.Split('x') is [var w, _] && int.TryParse(w, out var sirina) ? sirina : null;

    private static string? Skrati(string? s, int n) => s is null ? null : s.Length <= n ? s : s[..n];
}

/// <summary>Pomoćne funkcije za identifikaciju klijenta - uz poštovanje privatnosti.</summary>
public static class IdentifikacijaKlijenta
{
    /// <summary>
    /// Otisak = HMAC-SHA256(tajni ključ, porodica pregledača | jezik | ekran | zona | platforma | gustina | TLS).
    /// HMAC (a ne običan SHA-256) znači da ni neko ko ima bazu ne može da "pogađa" otiske bez tajnog ključa.
    /// IP adresa NAMERNO nije deo otiska: menja se na mobilnoj mreži, a iza NAT-a je dele mnogi korisnici.
    /// </summary>
    public static string Otisak(string tajniKljuc, string? userAgent, string? jezik, KlijentskiSignali? s, string? tlsOtisak)
    {
        var ulaz = string.Join('|',
            OpisKlijenta(userAgent), (jezik ?? "-").Split(',')[0].Trim().ToLowerInvariant(),
            s?.Ekran ?? "-", s?.Zona ?? "-", s?.Platforma ?? "-", s?.GustinaPiksela?.ToString("0.##") ?? "-", tlsOtisak ?? "-");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(tajniKljuc));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(ulaz))).ToLowerInvariant();
    }

    /// <summary>Gruba porodica klijenta ("Chrome / Windows") umesto punog User-Agent-a.</summary>
    public static string OpisKlijenta(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua))
        {
            return "Nepoznat";
        }

        var pregledac = ua switch
        {
            _ when ua.Contains("Edg/", StringComparison.Ordinal) => "Edge",
            _ when ua.Contains("OPR/", StringComparison.Ordinal) => "Opera",
            _ when ua.Contains("Firefox/", StringComparison.Ordinal) => "Firefox",
            _ when ua.Contains("Chrome/", StringComparison.Ordinal) => "Chrome",
            _ when ua.Contains("Safari/", StringComparison.Ordinal) => "Safari",
            _ => ua.Split('/')[0].Trim() is { Length: > 0 and <= 30 } ime ? ime : "Ostalo"
        };
        var sistem = ua switch
        {
            _ when ua.Contains("Android", StringComparison.OrdinalIgnoreCase) => "Android",
            _ when ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || ua.Contains("iPad", StringComparison.OrdinalIgnoreCase) => "iOS",
            _ when ua.Contains("Windows", StringComparison.OrdinalIgnoreCase) => "Windows",
            _ when ua.Contains("Mac OS", StringComparison.OrdinalIgnoreCase) => "macOS",
            _ when ua.Contains("Linux", StringComparison.OrdinalIgnoreCase) => "Linux",
            _ => "?"
        };
        return $"{pregledac} / {sistem}";
    }

    /// <summary>Maskirana IP adresa za logove (IPv4: poslednji oktet = 0; IPv6: zadržava se samo /48 prefiks).</summary>
    public static string MaskirajIp(IPAddress? ip)
    {
        if (ip is null)
        {
            return "-";
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        var b = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            b[3] = 0;
        }
        else
        {
            for (var i = 6; i < b.Length; i++)
            {
                b[i] = 0;
            }
        }

        return new IPAddress(b).ToString();
    }
}
