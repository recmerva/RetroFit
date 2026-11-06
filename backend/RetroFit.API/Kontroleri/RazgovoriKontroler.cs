using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Razgovori;

namespace RetroFit.API.Kontroleri;

public sealed record PorukaZahtev(string? Sadrzaj, string? BrzaPorukaKljuc);

/// <summary>Razgovori između kupca i prodavca (uvek vezani za jedan oglas) i poruke.</summary>
[Route(Osnova + "/razgovori")]
[Authorize]
public sealed class RazgovoriKontroler : ApiKontroler
{
    /// <summary>Moji razgovori, sortirani po poslednjoj poruci, sa brojem nepročitanih.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(StranicaRezultata<RazgovorPregledDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<RazgovorPregledDto>>> Lista([FromQuery] int stranica = 1, [FromQuery] int velicina = 20,
        CancellationToken ct = default) =>
        Ok(await Mediator.Send(new RazgovoriQuery(Stranica(stranica, velicina)), ct));

    /// <summary>
    /// Započinjanje razgovora o oglasu (ili nastavak postojećeg - jedan kupac ima jedan razgovor po oglasu).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RazgovorDetaljiDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RazgovorDetaljiDto>> Zapocni(ZapocniRazgovorCommand komanda, CancellationToken ct) =>
        Ok(await Mediator.Send(komanda, ct));

    /// <summary>Detalji razgovora sa ponudama. Kupac sa rezervacijom vidi i telefon prodavca (ako je dozvoljen).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RazgovorDetaljiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RazgovorDetaljiDto>> Detalji(int id, CancellationToken ct) =>
        Ok(await Mediator.Send(new RazgovorDetaljiQuery(id), ct));

    /// <summary>Istorija poruka (najnovije prve, sa straničenjem).</summary>
    [HttpGet("{id:int}/poruke")]
    [ProducesResponseType(typeof(StranicaRezultata<PorukaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<PorukaDto>>> Poruke(int id, [FromQuery] int stranica = 1, [FromQuery] int velicina = 50,
        CancellationToken ct = default) =>
        Ok(await Mediator.Send(new PorukeQuery(id, Stranica(stranica, velicina)), ct));

    /// <summary>Slanje poruke (tekst ili brza poruka po ključu).</summary>
    [HttpPost("{id:int}/poruke")]
    [ProducesResponseType(typeof(PorukaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PorukaDto>> Posalji(int id, PorukaZahtev z, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Mediator.Send(new PosaljiPorukuCommand(id, z.Sadrzaj, z.BrzaPorukaKljuc), ct));

    /// <summary>Označavanje svih poruka druge strane u razgovoru kao pročitanih.</summary>
    [HttpPost("{id:int}/procitano")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Procitano(int id, CancellationToken ct)
    {
        await Mediator.Send(new OznaciRazgovorProcitanimCommand(id), ct);
        return NoContent();
    }

    /// <summary>Lista brzih poruka.</summary>
    [HttpGet("brze-poruke")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<BrzaPorukaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BrzaPorukaDto>>> BrzePoruke(CancellationToken ct) =>
        Ok(await Mediator.Send(new BrzePorukeQuery(), ct));
}
