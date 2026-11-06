using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RetroFit.Application.Common.Modeli;

namespace RetroFit.API.Kontroleri;

/// <summary>
/// Bazni kontroler. Kontroleri su "tanki": samo primaju HTTP zahtev, šalju Command/Query kroz MediatR
/// i vraćaju rezultat. Nema poslovne logike niti pristupa bazi u kontrolerima.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class ApiKontroler : ControllerBase
{
    public const string Osnova = "api/v{version:apiVersion}";

    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected static Stranicenje Stranica(int stranica, int velicina) => new() { Stranica = stranica, VelicinaStranice = velicina };
}

/// <summary>Nazivi autorizacionih politika.</summary>
public static class Politike
{
    /// <summary>Moderator ili Admin: prijave, uklanjanje oglasa, blokiranje korisnika.</summary>
    public const string Moderacija = "Moderacija";
    /// <summary>Samo Admin: kategorije, uloge, izdvajanje, blokada uređaja, statistika, audit.</summary>
    public const string Administracija = "Administracija";
}
