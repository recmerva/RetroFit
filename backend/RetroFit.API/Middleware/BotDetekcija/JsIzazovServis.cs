using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace RetroFit.API.Middleware.BotDetekcija;

public sealed record JsIzazovDto(string Nonce, int Tezina, long IsticeUnix, string Potpis);

public sealed record ResenjeIzazovaDto(string Nonce, int Tezina, long IsticeUnix, string Potpis, long Resenje);

public sealed record TokenIzazovaDto(string Token, DateTime IsticeUtc);

/// <summary>
/// JavaScript proof-of-work izazov (alternativa CAPTCHA-i, bez spoljnog servisa i bez praćenja korisnika):
///  1) server izdaje nonce i težinu N, potpisane HMAC-om (server ne mora da pamti izdate izazove);
///  2) browser traži broj X takav da SHA-256("nonce:X") počinje sa N nultih bitova (~2^N pokušaja);
///  3) server proverava rešenje jednim heširanjem i izdaje kratkotrajan token vezan za otisak uređaja.
///
/// Šta ovo postiže: svaki "skupi" zahtev (registracija, poruka sa rizikom) košta CPU vreme, pa masovna
/// automatizacija postaje skupa. Šta NE postiže: ne dokazuje da je klijent čovek - odlučan napadač
/// može da reši izazov. Zato je ovo jedan sloj, uz rate limit i analizu ponašanja.
/// </summary>
public sealed class JsIzazovServis(IOptions<BotDetekcijaOpcije> opcije, IMemoryCache kes)
{
    private readonly BotDetekcijaOpcije _o = opcije.Value;

    public JsIzazovDto Izdaj(DateTime sada)
    {
        var nonce = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(16));
        var istice = new DateTimeOffset(sada.AddMinutes(2)).ToUnixTimeSeconds();
        return new JsIzazovDto(nonce, _o.TezinaIzazova, istice, Potpisi($"izazov|{nonce}|{_o.TezinaIzazova}|{istice}"));
    }

    public TokenIzazovaDto? Proveri(ResenjeIzazovaDto r, string otisak, DateTime sada)
    {
        if (!JednakiPotpisi(r.Potpis, Potpisi($"izazov|{r.Nonce}|{r.Tezina}|{r.IsticeUnix}"))
            || r.Tezina < _o.TezinaIzazova
            || DateTimeOffset.FromUnixTimeSeconds(r.IsticeUnix).UtcDateTime < sada
            || !ImaVodeceNule(SHA256.HashData(Encoding.UTF8.GetBytes($"{r.Nonce}:{r.Resenje}")), r.Tezina))
        {
            return null;
        }

        // Zaštita od ponovnog korišćenja (replay): isti nonce se prihvata samo jednom.
        var kljuc = $"izazov-iskoriscen:{r.Nonce}";
        if (kes.TryGetValue(kljuc, out _))
        {
            return null;
        }

        kes.Set(kljuc, true, TimeSpan.FromMinutes(5));

        var istice = sada.AddMinutes(_o.TrajanjeTokenaIzazovaMinuta);
        var sadrzaj = $"{otisak}|{new DateTimeOffset(istice).ToUnixTimeSeconds()}";
        var token = $"{Base64UrlEncoder.Encode(sadrzaj)}.{Potpisi($"token|{sadrzaj}")}";
        return new TokenIzazovaDto(token, istice);
    }

    /// <summary>Token je važeći samo za ISTI otisak uređaja za koji je izdat (ne može se "pozajmiti" drugom klijentu).</summary>
    public bool ValidanToken(string? token, string otisak, DateTime sada)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 300 || token.Split('.') is not [var deo, var potpis])
        {
            return false;
        }

        string sadrzaj;
        try
        {
            sadrzaj = Base64UrlEncoder.Decode(deo);
        }
        catch (FormatException)
        {
            return false;
        }

        return JednakiPotpisi(potpis, Potpisi($"token|{sadrzaj}"))
               && sadrzaj.Split('|') is [var o, var istice]
               && o == otisak
               && long.TryParse(istice, out var unix)
               && DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime > sada;
    }

    internal static bool ImaVodeceNule(byte[] hash, int bitova)
    {
        var celihBajtova = bitova / 8;
        for (var i = 0; i < celihBajtova; i++)
        {
            if (hash[i] != 0)
            {
                return false;
            }
        }

        var ostatak = bitova % 8;
        return ostatak == 0 || hash[celihBajtova] >> (8 - ostatak) == 0;
    }

    private string Potpisi(string tekst)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_o.TajniKljuc));
        return Base64UrlEncoder.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(tekst)));
    }

    /// <summary>Poređenje u konstantnom vremenu (zaštita od timing napada na potpis).</summary>
    private static bool JednakiPotpisi(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));
}
