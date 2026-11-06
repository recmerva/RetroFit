using Serilog.Context;

namespace RetroFit.API.Middleware;

/// <summary>
/// Korelacioni ID: svaki zahtev dobija ID (ili koristi X-Correlation-Id koji je poslao klijent),
/// koji se vraća u odgovoru i dodaje u SVE logove tog zahteva - lako se prati jedan zahtev kroz logove.
/// </summary>
internal sealed class KorelacioniIdMiddleware(RequestDelegate sledeci)
{
    public const string Zaglavlje = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext kontekst)
    {
        var id = kontekst.Request.Headers[Zaglavlje].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64 || !id.All(c => char.IsLetterOrDigit(c) || c is '-' or '_'))
        {
            id = Guid.NewGuid().ToString("N");
        }

        kontekst.TraceIdentifier = id;
        kontekst.Response.Headers[Zaglavlje] = id;

        using (LogContext.PushProperty("KorelacioniId", id))
        {
            await sledeci(kontekst);
        }
    }
}

/// <summary>Osnovna bezbednosna HTTP zaglavlja.</summary>
internal sealed class BezbednosnaZaglavljaMiddleware(RequestDelegate sledeci)
{
    public Task InvokeAsync(HttpContext kontekst)
    {
        var h = kontekst.Response.Headers;
        h["X-Content-Type-Options"] = "nosniff";
        h["X-Frame-Options"] = "DENY";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)";
        return sledeci(kontekst);
    }
}
