using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Administracija;
using RetroFit.Application.Features.Korisnici;
using RetroFit.Application.Features.Moderacija;
using RetroFit.Application.Features.Oglasi;
using RetroFit.Domain.Enumi;

namespace RetroFit.API.Kontroleri;

public sealed record UlogeZahtev(IReadOnlyList<string> Uloge);
public sealed record IzdvajanjeZahtev(int BrojDana);

/// <summary>Administracija (samo Admin): statistika, audit log, uloge, izdvajanje oglasa i blokada uređaja.</summary>
[Route(Osnova + "/admin")]
[Authorize(Policy = Politike.Administracija)]
public sealed class AdminKontroler : ApiKontroler
{
    /// <summary>Statistika platforme za admin dashboard.</summary>
    [HttpGet("statistika")]
    [ProducesResponseType(typeof(StatistikaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StatistikaDto>> Statistika(CancellationToken ct) =>
        Ok(await Mediator.Send(new StatistikaQuery(), ct));

    /// <summary>Audit log sa filterima (kategorija, korisnik, entitet, period).</summary>
    [HttpGet("audit")]
    [ProducesResponseType(typeof(StranicaRezultata<AuditZapisDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<AuditZapisDto>>> Audit([FromQuery] KategorijaAudita? kategorija, [FromQuery] int? korisnikId,
        [FromQuery] string? entitet, [FromQuery] DateTime? od, [FromQuery] DateTime? @do, [FromQuery] int stranica = 1,
        [FromQuery] int velicina = 50, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new AuditQuery(new FilterAudita
        {
            Kategorija = kategorija,
            KorisnikId = korisnikId,
            NazivEntiteta = entitet,
            OdUtc = od,
            DoUtc = @do
        }, Stranica(stranica, velicina)), ct));

    /// <summary>Promena uloga korisnika (Korisnik je obavezna; korisnik mora ponovo da se prijavi).</summary>
    [HttpPut("korisnici/{korisnikId:int}/uloge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PromeniUloge(int korisnikId, UlogeZahtev z, CancellationToken ct)
    {
        await Mediator.Send(new PromeniUlogeCommand(korisnikId, z.Uloge), ct);
        return NoContent();
    }

    /// <summary>Lista izdvajanja/podizanja oglasa.</summary>
    [HttpGet("istaknuti")]
    [ProducesResponseType(typeof(StranicaRezultata<IstaknutiOglasDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<IstaknutiOglasDto>>> Istaknuti([FromQuery] bool samoAktivna = true,
        [FromQuery] int stranica = 1, [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new IstaknutiOglasiAdminQuery(samoAktivna, Stranica(stranica, velicina)), ct));

    /// <summary>Izdvajanje oglasa na početnoj strani.</summary>
    [HttpPost("oglasi/{oglasId:int}/izdvajanje")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Izdvoji(int oglasId, IzdvajanjeZahtev z, CancellationToken ct)
    {
        await Mediator.Send(new IzdvojiOglasCommand(oglasId, z.BrojDana), ct);
        return NoContent();
    }

    /// <summary>Otkazivanje izdvajanja ili podizanja.</summary>
    [HttpDelete("istaknuti/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> OtkaziIsticanje(int id, CancellationToken ct)
    {
        await Mediator.Send(new OtkaziIsticanjeCommand(id), ct);
        return NoContent();
    }

    /// <summary>Uređaji sa povišenim rizikom (Bot Detection).</summary>
    [HttpGet("uredjaji")]
    [ProducesResponseType(typeof(StranicaRezultata<OtisakUredjajaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<OtisakUredjajaDto>>> Uredjaji([FromQuery] int minRizik = 30, [FromQuery] int stranica = 1,
        [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new SumnjiviUredjajiQuery(minRizik, Stranica(stranica, velicina)), ct));

    /// <summary>Blokade uređaja.</summary>
    [HttpGet("uredjaji/blokade")]
    [ProducesResponseType(typeof(StranicaRezultata<BlokadaUredjajaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<BlokadaUredjajaDto>>> BlokadeUredjaja([FromQuery] bool samoAktivne = true,
        [FromQuery] int stranica = 1, [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new BlokadeUredjajaQuery(samoAktivne, Stranica(stranica, velicina)), ct));

    /// <summary>Ručna blokada uređaja.</summary>
    [HttpPost("uredjaji/{otisakId:int}/blokada")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BlokirajUredjaj(int otisakId, BlokadaZahtev z, CancellationToken ct)
    {
        await Mediator.Send(new BlokirajUredjajCommand(otisakId, z.Razlog, z.TrajanjeDana), ct);
        return NoContent();
    }

    /// <summary>Ukidanje blokade uređaja.</summary>
    [HttpDelete("uredjaji/blokade/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UkiniBlokaduUredjaja(int id, CancellationToken ct)
    {
        await Mediator.Send(new UkiniBlokaduUredjajaCommand(id), ct);
        return NoContent();
    }
}
