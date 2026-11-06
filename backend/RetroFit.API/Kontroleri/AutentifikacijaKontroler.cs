using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RetroFit.API.Konfiguracija;
using RetroFit.Application.Features.Autentifikacija;
using RetroFit.Application.Features.Korisnici;

namespace RetroFit.API.Kontroleri;

/// <summary>Odgovor na uspešnu prijavu. Refresh token NIJE u telu - nalazi se u httpOnly cookie-ju.</summary>
public sealed record AutentifikacijaOdgovor(string AccessToken, DateTime AccessTokenIsticeUtc, MojProfilDto Korisnik);

public sealed record OsvezavanjeZahtev(string? RefreshToken);

/// <summary>
/// Registracija, prijava, osvežavanje tokena (rotacija) i odjava.
/// </summary>
[Route(Osnova + "/autentifikacija")]
public sealed class AutentifikacijaKontroler(IOptions<KolacicOpcije> kolacici) : ApiKontroler
{
    /// <summary>Registracija novog korisnika. Odmah prijavljuje korisnika (vraća access token i postavlja refresh cookie).</summary>
    /// <response code="201">Nalog je kreiran.</response>
    /// <response code="400">Neispravni podaci (npr. slaba lozinka).</response>
    /// <response code="409">Email je već registrovan.</response>
    [HttpPost("registracija")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AutentifikacijaOdgovor), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AutentifikacijaOdgovor>> Registracija(RegistracijaCommand komanda, CancellationToken ct)
    {
        var rezultat = await Mediator.Send(komanda, ct);
        return StatusCode(StatusCodes.Status201Created, Odgovor(rezultat));
    }

    /// <summary>Prijava email adresom i lozinkom.</summary>
    /// <response code="200">Uspešna prijava.</response>
    /// <response code="401">Pogrešan email ili lozinka.</response>
    /// <response code="403">Nalog je blokiran.</response>
    [HttpPost("prijava")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AutentifikacijaOdgovor), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AutentifikacijaOdgovor>> Prijava(PrijavaCommand komanda, CancellationToken ct) =>
        Ok(Odgovor(await Mediator.Send(komanda, ct)));

    /// <summary>
    /// Osvežavanje access tokena pomoću refresh tokena (iz httpOnly cookie-ja, ili iz tela zahteva za ne-browser klijente).
    /// Svaki poziv ROTIRA refresh token: stari prestaje da važi, a novi se postavlja u cookie.
    /// </summary>
    /// <response code="200">Izdat je novi par tokena.</response>
    /// <response code="401">Refresh token je nevažeći, istekao ili je već iskorišćen.</response>
    [HttpPost("osvezi")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AutentifikacijaOdgovor), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AutentifikacijaOdgovor>> Osvezi([FromBody] OsvezavanjeZahtev? zahtev, CancellationToken ct)
    {
        var token = Request.Cookies[kolacici.Value.Naziv] ?? zahtev?.RefreshToken ?? string.Empty;
        try
        {
            return Ok(Odgovor(await Mediator.Send(new OsveziTokenCommand(token), ct)));
        }
        catch
        {
            ObrisiKolacic();
            throw;
        }
    }

    /// <summary>Odjava: opoziva trenutni refresh token i briše cookie.</summary>
    [HttpPost("odjava")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Odjava([FromBody] OsvezavanjeZahtev? zahtev, CancellationToken ct)
    {
        var token = Request.Cookies[kolacici.Value.Naziv] ?? zahtev?.RefreshToken;
        await Mediator.Send(new OdjavaCommand(token, false), ct);
        ObrisiKolacic();
        return NoContent();
    }

    /// <summary>Odjava sa svih uređaja: opoziva SVE refresh tokene korisnika.</summary>
    [HttpPost("odjava-svuda")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> OdjavaSvuda(CancellationToken ct)
    {
        await Mediator.Send(new OdjavaCommand(null, true), ct);
        ObrisiKolacic();
        return NoContent();
    }

    private AutentifikacijaOdgovor Odgovor(AutentifikacijaRezultat rezultat)
    {
        // httpOnly: JavaScript ne može da pročita cookie (zaštita od XSS).
        // SameSite=Strict: browser ga ne šalje sa tuđih sajtova (zaštita od CSRF).
        // Path: cookie se šalje SAMO na /autentifikacija rute, ne uz svaki API zahtev.
        Response.Cookies.Append(kolacici.Value.Naziv, rezultat.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = kolacici.Value.Secure,
            SameSite = SameSiteMode.Strict,
            Path = kolacici.Value.Putanja,
            Expires = rezultat.RefreshTokenIsticeUtc
        });

        return new AutentifikacijaOdgovor(rezultat.AccessToken, rezultat.AccessTokenIsticeUtc, rezultat.Korisnik);
    }

    private void ObrisiKolacic() =>
        Response.Cookies.Delete(kolacici.Value.Naziv, new CookieOptions
        {
            HttpOnly = true,
            Secure = kolacici.Value.Secure,
            SameSite = SameSiteMode.Strict,
            Path = kolacici.Value.Putanja
        });
}
