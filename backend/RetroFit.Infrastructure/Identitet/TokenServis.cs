using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Domain.Entiteti;

namespace RetroFit.Infrastructure.Identitet;

/// <summary>Podešavanja JWT-a (sekcija "Jwt" u appsettings / promenljive okruženja).</summary>
public sealed class JwtOpcije
{
    public const string Sekcija = "Jwt";

    public string Izdavac { get; set; } = "RetroFit";
    public string Publika { get; set; } = "RetroFit.Klijent";
    /// <summary>Tajni ključ za HMAC-SHA256 potpis; mora imati najmanje 32 bajta. U produkciji iz tajni, ne iz koda.</summary>
    public string Kljuc { get; set; } = string.Empty;
    public int AccessTokenMinuta { get; set; } = 15;
    public int RefreshTokenDana { get; set; } = 14;
}

/// <summary>
/// Izdavanje tokena:
///  - Access token: kratkotrajan JWT (15 min), potpisan HMAC-SHA256, sadrži Id korisnika i uloge.
///  - Refresh token: 64 nasumična bajta (kriptografski generator), vraća se klijentu u httpOnly cookie-ju;
///    u bazi se čuva samo njegov SHA-256 heš.
/// </summary>
internal sealed class TokenServis(IOptions<JwtOpcije> opcije, IDatumVreme vreme) : ITokenServis
{
    private readonly JwtOpcije _opcije = opcije.Value;

    public (string Token, DateTime IsticeUtc) KreirajAccessToken(Korisnik korisnik, IEnumerable<string> uloge)
    {
        var sada = vreme.SadaUtc;
        var istice = sada.AddMinutes(_opcije.AccessTokenMinuta);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, korisnik.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(sada).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("ime", korisnik.PrikaznoIme)
        };
        claims.AddRange(uloge.Select(u => new Claim(ClaimTypes.Role, u)));

        var kredencijali = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcije.Kljuc)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(_opcije.Izdavac, _opcije.Publika, claims, sada, istice, kredencijali);
        return (new JwtSecurityTokenHandler().WriteToken(token), istice);
    }

    public (string Token, string Hash, DateTime IsticeUtc) KreirajRefreshToken()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return (token, Hesiraj(token), vreme.SadaUtc.AddDays(_opcije.RefreshTokenDana));
    }

    public string Hesiraj(string vrednost) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(vrednost))).ToLowerInvariant();
}
