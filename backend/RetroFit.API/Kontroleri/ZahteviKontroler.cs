using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Zahtevi;

namespace RetroFit.API.Kontroleri;

public sealed record ZahtevZahtev(string Naslov, string Opis, int KategorijaId, int? GradId, decimal? MaksimalnaCena);
public sealed record ZatvaranjeZahtev(bool Ispunjen);
public sealed record OdgovorZahtev(int OglasId, string? Poruka);

/// <summary>Zahtevi "Tražim..." - korisnici objavljuju šta traže, prodavci odgovaraju svojim oglasima.</summary>
[Route(Osnova + "/zahtevi")]
[Authorize]
public sealed class ZahteviKontroler : ApiKontroler
{
    /// <summary>Pretraga zahteva (Full-Text nad naslovom i opisom).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StranicaRezultata<ZahtevDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<ZahtevDto>>> Pretraga([FromQuery] string? upit, [FromQuery] int? kategorijaId,
        [FromQuery] int? gradId, [FromQuery] bool samoOtvoreni = true, [FromQuery] int stranica = 1, [FromQuery] int velicina = 20,
        CancellationToken ct = default) =>
        Ok(await Mediator.Send(new ZahteviQuery(upit, kategorijaId, gradId, false, samoOtvoreni, Stranica(stranica, velicina)), ct));

    /// <summary>Moji zahtevi (svi statusi).</summary>
    [HttpGet("moji")]
    [ProducesResponseType(typeof(StranicaRezultata<ZahtevDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<ZahtevDto>>> Moji([FromQuery] int stranica = 1, [FromQuery] int velicina = 20,
        CancellationToken ct = default) =>
        Ok(await Mediator.Send(new ZahteviQuery(null, null, null, true, false, Stranica(stranica, velicina)), ct));

    /// <summary>Detalji zahteva sa ponuđenim oglasima.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ZahtevDetaljiDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ZahtevDetaljiDto>> Detalji(int id, CancellationToken ct) =>
        Ok(await Mediator.Send(new ZahtevDetaljiQuery(id), ct));

    [HttpPost]
    [ProducesResponseType(typeof(ZahtevDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ZahtevDto>> Kreiraj(ZahtevZahtev z, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created,
            await Mediator.Send(new KreirajZahtevCommand(z.Naslov, z.Opis, z.KategorijaId, z.GradId, z.MaksimalnaCena), ct));

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ZahtevDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ZahtevDto>> Izmeni(int id, ZahtevZahtev z, CancellationToken ct) =>
        Ok(await Mediator.Send(new IzmeniZahtevCommand(id, z.Naslov, z.Opis, z.KategorijaId, z.GradId, z.MaksimalnaCena), ct));

    /// <summary>Zatvaranje zahteva (ispunjen = pronašao sam šta sam tražio).</summary>
    [HttpPost("{id:int}/zatvori")]
    [ProducesResponseType(typeof(ZahtevDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ZahtevDto>> Zatvori(int id, ZatvaranjeZahtev z, CancellationToken ct) =>
        Ok(await Mediator.Send(new ZatvoriZahtevCommand(id, z.Ispunjen), ct));

    /// <summary>Odgovor na zahtev: nudim svoj aktivan oglas.</summary>
    [HttpPost("{id:int}/odgovori")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Odgovori(int id, OdgovorZahtev z, CancellationToken ct)
    {
        await Mediator.Send(new OdgovoriNaZahtevCommand(id, z.OglasId, z.Poruka), ct);
        return NoContent();
    }
}
