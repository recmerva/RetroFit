using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Obavestenja;

namespace RetroFit.API.Kontroleri;

/// <summary>Obaveštenja prijavljenog korisnika.</summary>
[Route(Osnova + "/obavestenja")]
[Authorize]
public sealed class ObavestenjaKontroler : ApiKontroler
{
    [HttpGet]
    [ProducesResponseType(typeof(StranicaRezultata<ObavestenjeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<ObavestenjeDto>>> Lista([FromQuery] bool samoNeprocitana = false,
        [FromQuery] int stranica = 1, [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new ObavestenjaQuery(samoNeprocitana, Stranica(stranica, velicina)), ct));

    /// <summary>Brojači za navigaciju: nepročitana obaveštenja, nepročitane poruke, ponude koje čekaju moj odgovor.</summary>
    [HttpGet("brojaci")]
    [ProducesResponseType(typeof(BrojaciDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BrojaciDto>> Brojaci(CancellationToken ct) => Ok(await Mediator.Send(new BrojaciQuery(), ct));

    [HttpPost("{id:int}/procitano")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Procitano(int id, CancellationToken ct)
    {
        await Mediator.Send(new OznaciObavestenjeProcitanimCommand(id), ct);
        return NoContent();
    }

    [HttpPost("procitano")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SvaProcitana(CancellationToken ct)
    {
        await Mediator.Send(new OznaciSvaObavestenjaProcitanimCommand(), ct);
        return NoContent();
    }
}
