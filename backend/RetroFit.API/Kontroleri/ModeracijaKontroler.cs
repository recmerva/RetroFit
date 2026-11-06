using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Korisnici;
using RetroFit.Application.Features.Moderacija;
using RetroFit.Domain.Enumi;

namespace RetroFit.API.Kontroleri;

public sealed record PrijavaZahtev(RazlogPrijave Razlog, string? Opis);
public sealed record ObradaPrijaveZahtev(bool Prihvati, bool UkloniOglas, string? Napomena);
public sealed record UklanjanjeZahtev(string Razlog);
public sealed record BlokadaZahtev(string Razlog, int? TrajanjeDana);

/// <summary>Prijave oglasa i moderacija (Moderator i Admin).</summary>
[Route(Osnova)]
[Authorize]
public sealed class ModeracijaKontroler : ApiKontroler
{
    /// <summary>Prijava sumnjivog oglasa (svaki prijavljeni korisnik).</summary>
    [HttpPost("oglasi/{oglasId:int}/prijava")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Prijavi(int oglasId, PrijavaZahtev z, CancellationToken ct)
    {
        await Mediator.Send(new PrijaviOglasCommand(oglasId, z.Razlog, z.Opis), ct);
        return NoContent();
    }

    /// <summary>[Moderator] Prijave (podrazumevano otvorene), sortirane po broju prijava za oglas.</summary>
    [HttpGet("moderacija/prijave")]
    [Authorize(Policy = Politike.Moderacija)]
    [ProducesResponseType(typeof(StranicaRezultata<PrijavaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<PrijavaDto>>> Prijave([FromQuery] StatusPrijave? status = StatusPrijave.Otvorena,
        [FromQuery] int stranica = 1, [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new PrijaveQuery(status, Stranica(stranica, velicina)), ct));

    /// <summary>[Moderator] Obrada prijave: prihvatanje (uz opciono uklanjanje oglasa) ili odbijanje.</summary>
    [HttpPost("moderacija/prijave/{id:int}/obrada")]
    [Authorize(Policy = Politike.Moderacija)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ObradiPrijavu(int id, ObradaPrijaveZahtev z, CancellationToken ct)
    {
        await Mediator.Send(new ObradiPrijavuCommand(id, z.Prihvati, z.UkloniOglas, z.Napomena), ct);
        return NoContent();
    }

    /// <summary>[Moderator] Direktno uklanjanje oglasa.</summary>
    [HttpPost("moderacija/oglasi/{oglasId:int}/uklanjanje")]
    [Authorize(Policy = Politike.Moderacija)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UkloniOglas(int oglasId, UklanjanjeZahtev z, CancellationToken ct)
    {
        await Mediator.Send(new UkloniOglasCommand(oglasId, z.Razlog), ct);
        return NoContent();
    }

    /// <summary>[Moderator] Pretraga korisnika.</summary>
    [HttpGet("moderacija/korisnici")]
    [Authorize(Policy = Politike.Moderacija)]
    [ProducesResponseType(typeof(StranicaRezultata<KorisnikAdminDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<KorisnikAdminDto>>> Korisnici([FromQuery] string? upit, [FromQuery] int stranica = 1,
        [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new KorisniciAdminQuery(upit, Stranica(stranica, velicina)), ct));

    /// <summary>[Moderator] Blokiranje korisnika (trajanjeDana = null znači trajno). Moderatora može blokirati samo admin.</summary>
    [HttpPost("moderacija/korisnici/{korisnikId:int}/blokada")]
    [Authorize(Policy = Politike.Moderacija)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Blokiraj(int korisnikId, BlokadaZahtev z, CancellationToken ct)
    {
        await Mediator.Send(new BlokirajKorisnikaCommand(korisnikId, z.Razlog, z.TrajanjeDana), ct);
        return NoContent();
    }

    /// <summary>[Moderator] Lista blokada korisnika.</summary>
    [HttpGet("moderacija/blokade")]
    [Authorize(Policy = Politike.Moderacija)]
    [ProducesResponseType(typeof(StranicaRezultata<BlokadaKorisnikaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<BlokadaKorisnikaDto>>> Blokade([FromQuery] bool samoAktivne = true,
        [FromQuery] int stranica = 1, [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new BlokadeKorisnikaQuery(samoAktivne, Stranica(stranica, velicina)), ct));

    /// <summary>[Moderator] Ukidanje blokade.</summary>
    [HttpDelete("moderacija/blokade/{id:int}")]
    [Authorize(Policy = Politike.Moderacija)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UkiniBlokadu(int id, CancellationToken ct)
    {
        await Mediator.Send(new UkiniBlokaduKorisnikaCommand(id), ct);
        return NoContent();
    }
}
