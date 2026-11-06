using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroFit.Application.Features.Katalog;
using RetroFit.Domain.Enumi;

namespace RetroFit.API.Kontroleri;

public sealed record KategorijaZahtev(string Naziv, string? Slug, int? RoditeljId, string? Ikonica, int Redosled);

public sealed record DefinicijaAtributaZahtev(string Naziv, string? Kljuc, TipAtributa Tip, bool Obavezan,
    IReadOnlyList<string>? Opcije, string? JedinicaMere, int Redosled);

public sealed record AktivnostZahtev(bool Aktivna);

/// <summary>Šifarnici: gradovi i kategorije sa atributima.</summary>
[Route(Osnova)]
public sealed class KatalogKontroler : ApiKontroler
{
    /// <summary>Lista gradova (sa koordinatama za pretragu po udaljenosti).</summary>
    [HttpGet("gradovi")]
    [AllowAnonymous]
    // Kratak keš (5 min): šifarnik se retko menja, a posle izmene (nova migracija) korisnici brzo vide novi spisak.
    [ResponseCache(Duration = 300)]
    [ProducesResponseType(typeof(IReadOnlyList<GradDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GradDto>>> Gradovi(CancellationToken ct) =>
        Ok(await Mediator.Send(new GradoviQuery(), ct));

    /// <summary>Stablo kategorija (glavne kategorije sa podkategorijama i brojem aktivnih oglasa).</summary>
    [HttpGet("kategorije")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<KategorijaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<KategorijaDto>>> Kategorije([FromQuery] bool ukljuciNeaktivne, CancellationToken ct)
    {
        // Neaktivne kategorije vidi samo administrator.
        var sveKategorije = ukljuciNeaktivne && User.IsInRole("Admin");
        return Ok(await Mediator.Send(new KategorijeQuery(sveKategorije), ct));
    }

    /// <summary>Atributi kategorije (uključujući nasleđene iz nadređene kategorije).</summary>
    [HttpGet("kategorije/{id:int}/atributi")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<DefinicijaAtributaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DefinicijaAtributaDto>>> Atributi(int id, CancellationToken ct) =>
        Ok(await Mediator.Send(new DefinicijeAtributaQuery(id), ct));

    /// <summary>[Admin] Nova kategorija.</summary>
    [HttpPost("kategorije")]
    [Authorize(Policy = Politike.Administracija)]
    [ProducesResponseType(typeof(KategorijaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<KategorijaDto>> KreirajKategoriju(KategorijaZahtev z, CancellationToken ct)
    {
        var rezultat = await Mediator.Send(new SacuvajKategorijuCommand(null, z.Naziv, z.Slug, z.RoditeljId, z.Ikonica, z.Redosled), ct);
        return StatusCode(StatusCodes.Status201Created, rezultat);
    }

    /// <summary>[Admin] Izmena kategorije.</summary>
    [HttpPut("kategorije/{id:int}")]
    [Authorize(Policy = Politike.Administracija)]
    [ProducesResponseType(typeof(KategorijaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<KategorijaDto>> IzmeniKategoriju(int id, KategorijaZahtev z, CancellationToken ct) =>
        Ok(await Mediator.Send(new SacuvajKategorijuCommand(id, z.Naziv, z.Slug, z.RoditeljId, z.Ikonica, z.Redosled), ct));

    /// <summary>[Admin] Aktiviranje/deaktiviranje kategorije (deaktivirana se ne nudi pri kreiranju oglasa).</summary>
    [HttpPatch("kategorije/{id:int}/aktivnost")]
    [Authorize(Policy = Politike.Administracija)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PromeniAktivnost(int id, AktivnostZahtev z, CancellationToken ct)
    {
        await Mediator.Send(new PromeniAktivnostKategorijeCommand(id, z.Aktivna), ct);
        return NoContent();
    }

    /// <summary>[Admin] Brisanje kategorije (samo ako se ne koristi).</summary>
    [HttpDelete("kategorije/{id:int}")]
    [Authorize(Policy = Politike.Administracija)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ObrisiKategoriju(int id, CancellationToken ct)
    {
        await Mediator.Send(new ObrisiKategorijuCommand(id), ct);
        return NoContent();
    }

    /// <summary>[Admin] Dodavanje atributa kategoriji.</summary>
    [HttpPost("kategorije/{id:int}/atributi")]
    [Authorize(Policy = Politike.Administracija)]
    [ProducesResponseType(typeof(DefinicijaAtributaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DefinicijaAtributaDto>> DodajAtribut(int id, DefinicijaAtributaZahtev z, CancellationToken ct)
    {
        var rezultat = await Mediator.Send(new DodajDefinicijuAtributaCommand(id, z.Naziv, z.Kljuc, z.Tip, z.Obavezan, z.Opcije,
            z.JedinicaMere, z.Redosled), ct);
        return StatusCode(StatusCodes.Status201Created, rezultat);
    }
}
