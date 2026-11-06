using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroFit.Application.Features.Korisnici;

namespace RetroFit.API.Kontroleri;

public sealed record ObrisiNalogZahtev(string Lozinka);

/// <summary>Profil prijavljenog korisnika i javni profili drugih korisnika.</summary>
[Route(Osnova + "/korisnici")]
[Authorize]
public sealed class KorisniciKontroler : ApiKontroler
{
    /// <summary>Moj profil (sa privatnim podacima: email, telefon).</summary>
    [HttpGet("ja")]
    [ProducesResponseType(typeof(MojProfilDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MojProfilDto>> MojProfil(CancellationToken ct) =>
        Ok(await Mediator.Send(new MojProfilQuery(), ct));

    /// <summary>Izmena profila (ime, prezime, opis, grad, privatnost telefona).</summary>
    [HttpPut("ja")]
    [ProducesResponseType(typeof(MojProfilDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MojProfilDto>> AzurirajProfil(AzurirajProfilCommand komanda, CancellationToken ct) =>
        Ok(await Mediator.Send(komanda, ct));

    /// <summary>Promena lozinke. Posle promene, sve sesije na drugim uređajima se poništavaju.</summary>
    [HttpPost("ja/lozinka")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PromeniLozinku(PromeniLozinkuCommand komanda, CancellationToken ct)
    {
        await Mediator.Send(komanda, ct);
        return NoContent();
    }

    /// <summary>Postavljanje broja telefona i slanje verifikacionog SMS koda (mock SMS servis).</summary>
    [HttpPost("ja/telefon")]
    [ProducesResponseType(typeof(VerifikacijaPoslataDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VerifikacijaPoslataDto>> ZatraziVerifikaciju(ZatraziVerifikacijuTelefonaCommand komanda, CancellationToken ct) =>
        Ok(await Mediator.Send(komanda, ct));

    /// <summary>Potvrda broja telefona šestocifrenim kodom iz SMS-a.</summary>
    [HttpPost("ja/telefon/potvrda")]
    [ProducesResponseType(typeof(MojProfilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MojProfilDto>> PotvrdiTelefon(PotvrdiTelefonCommand komanda, CancellationToken ct) =>
        Ok(await Mediator.Send(komanda, ct));

    /// <summary>Brisanje naloga (anonimizacija ličnih podataka). Zahteva potvrdu lozinkom.</summary>
    [HttpDelete("ja")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ObrisiNalog(ObrisiNalogZahtev zahtev, CancellationToken ct)
    {
        await Mediator.Send(new ObrisiNalogCommand(zahtev.Lozinka), ct);
        return NoContent();
    }

    /// <summary>Javni profil korisnika (bez emaila i telefona).</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JavniProfilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JavniProfilDto>> JavniProfil(int id, CancellationToken ct) =>
        Ok(await Mediator.Send(new JavniProfilQuery(id), ct));
}
