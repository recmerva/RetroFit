using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Domain.Common;

namespace RetroFit.API.Middleware;

/// <summary>
/// Globalni exception handler (IExceptionHandler, .NET 8). Svaki izuzetak pretvara u ProblemDetails
/// (RFC 7807), pa svi odgovori sa greškom imaju ISTI format:
/// { "type", "title", "status", "detail", "instance", "traceId", "kod", "greske" }.
///
/// Detalji neočekivanih grešaka (stack trace) se NE vraćaju klijentu - samo se loguju.
/// </summary>
internal sealed class GlobalniExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalniExceptionHandler> logger,
    IHostEnvironment okruzenje) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext kontekst, Exception izuzetak, CancellationToken ct)
    {
        var (status, naslov, detalj, kod) = izuzetak switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Neispravni podaci", "Jedno ili više polja nije ispravno.", "validacija"),
            DomenskiIzuzetak d when d.Kod == "validacija" => (StatusCodes.Status400BadRequest, "Neispravni podaci", d.Message, d.Kod),
            DomenskiIzuzetak d when d.Kod == "zabranjeno" => (StatusCodes.Status403Forbidden, "Zabranjeno", d.Message, d.Kod),
            DomenskiIzuzetak d when d.Kod == "previse_zahteva" => (StatusCodes.Status429TooManyRequests, "Previše zahteva", d.Message, d.Kod),
            DomenskiIzuzetak d => (StatusCodes.Status422UnprocessableEntity, "Poslovno pravilo nije ispunjeno", d.Message, d.Kod),
            NijePronadjenoIzuzetak n => (StatusCodes.Status404NotFound, "Nije pronađeno", n.Message, "nije_pronadjeno"),
            NeovlascenIzuzetak n => (StatusCodes.Status401Unauthorized, "Neovlašćen pristup", n.Message, "neovlascen"),
            ZabranjenoIzuzetak z => (StatusCodes.Status403Forbidden, "Zabranjeno", z.Message, "zabranjeno"),
            BlokiranIzuzetak b => (StatusCodes.Status403Forbidden, "Pristup blokiran", b.Message, "blokiran"),
            KonfliktIzuzetak k => (StatusCodes.Status409Conflict, "Konflikt", k.Message, "konflikt"),
            BadHttpRequestException b => (b.StatusCode, "Neispravan zahtev", "Zahtev nije ispravno formiran.", "neispravan_zahtev"),
            OperationCanceledException => (499, "Zahtev otkazan", "Klijent je prekinuo zahtev.", "otkazano"),
            _ => (StatusCodes.Status500InternalServerError, "Greška na serveru",
                "Došlo je do neočekivane greške. Pokušajte ponovo kasnije.", "greska_servera")
        };

        if (status >= 500)
        {
            logger.LogError(izuzetak, "Neobrađena greška pri obradi {Metoda} {Putanja}", kontekst.Request.Method, kontekst.Request.Path);
        }
        else
        {
            logger.LogInformation("Zahtev {Metoda} {Putanja} odbijen: {Status} {Kod}", kontekst.Request.Method, kontekst.Request.Path, status, kod);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = naslov,
            Detail = detalj,
            Type = $"https://retrofit.rs/greske/{kod}",
            Instance = kontekst.Request.Path
        };
        problem.Extensions["kod"] = kod;

        if (izuzetak is ValidationException validacija)
        {
            // Greške grupisane po polju, u istom obliku kao ASP.NET ValidationProblemDetails.
            problem.Extensions["greske"] = validacija.Errors
                .GroupBy(g => string.IsNullOrEmpty(g.PropertyName) ? "zahtev" : char.ToLowerInvariant(g.PropertyName[0]) + g.PropertyName[1..])
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).Distinct().ToArray());
        }

        if (status >= 500 && okruzenje.IsDevelopment())
        {
            problem.Extensions["izuzetak"] = izuzetak.GetType().Name;
        }

        kontekst.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = kontekst,
            ProblemDetails = problem,
            Exception = izuzetak
        });
    }
}
