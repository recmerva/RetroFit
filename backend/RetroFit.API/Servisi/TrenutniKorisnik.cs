using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.API.Middleware.BotDetekcija;

namespace RetroFit.API.Servisi;

/// <summary>Čita podatke o prijavljenom korisniku iz JWT tokena trenutnog HTTP zahteva.</summary>
internal sealed class TrenutniKorisnik(IHttpContextAccessor pristup) : ITrenutniKorisnik
{
    private HttpContext? Kontekst => pristup.HttpContext;

    public int? Id
    {
        get
        {
            var vrednost = Kontekst?.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                           ?? Kontekst?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(vrednost, out var id) ? id : null;
        }
    }

    public bool JePrijavljen => Id is not null;

    public int ObavezanId => Id ?? throw new NeovlascenIzuzetak("Morate biti prijavljeni.");

    public bool JeUUlozi(string uloga) => Kontekst?.User.IsInRole(uloga) == true;

    /// <summary>IP adresa klijenta (iza reverse proxy-ja je ispravna samo uz ForwardedHeaders middleware).</summary>
    public string? IpAdresa => Kontekst?.Connection.RemoteIpAddress?.ToString();

    public string? KorelacioniId => Kontekst?.TraceIdentifier;

    public string? OtisakUredjaja => Kontekst?.Items[BotDetekcijaKljucevi.KljucOtiska] as string;
}
