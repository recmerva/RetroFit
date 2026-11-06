using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RetroFit.API.Middleware.BotDetekcija;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Infrastructure.Servisi;

namespace RetroFit.API.Kontroleri;

/// <summary>JavaScript proof-of-work izazov (Bot Detection).</summary>
[Route(Osnova + "/bezbednost")]
[AllowAnonymous]
public sealed class BezbednostKontroler(JsIzazovServis izazovi, IDatumVreme vreme, IOptionsMonitor<BotDetekcijaOpcije> opcije) : ApiKontroler
{
    /// <summary>
    /// Novi izazov: pronađi broj X tako da SHA-256("nonce:X") počinje sa "tezina" nultih bitova.
    /// </summary>
    [HttpGet("izazov")]
    [ProducesResponseType(typeof(JsIzazovDto), StatusCodes.Status200OK)]
    public ActionResult<JsIzazovDto> Izazov() => Ok(izazovi.Izdaj(vreme.SadaUtc));

    /// <summary>Provera rešenja; vraća token koji se šalje u zaglavlju X-Challenge-Token (vezan za otisak uređaja).</summary>
    [HttpPost("izazov")]
    [ProducesResponseType(typeof(TokenIzazovaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<TokenIzazovaDto> Resi(ResenjeIzazovaDto resenje)
    {
        if (HttpContext.Items[BotDetekcijaKljucevi.KljucOtiska] is not string otisak)
        {
            return Problem(statusCode: 400, title: "Neispravan zahtev", detail: "Bot detekcija nije aktivna.");
        }

        var token = izazovi.Proveri(resenje, otisak, vreme.SadaUtc);
        return token is null
            ? Problem(statusCode: 400, title: "Neispravno rešenje", detail: "Rešenje izazova nije ispravno ili je izazov istekao.")
            : Ok(token);
    }

    /// <summary>[Razvoj/demonstracija] Procena rizika za trenutni zahtev sa razlozima. Isključeno u produkciji.</summary>
    [HttpGet("procena")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Procena()
    {
        if (!opcije.CurrentValue.PrikaziProcenu)
        {
            return NotFound();
        }

        var procena = HttpContext.Items[BotDetekcijaKljucevi.KljucRizika] as ProcenaRizika;
        var otisak = HttpContext.Items[BotDetekcijaKljucevi.KljucOtiska] as string;
        return Ok(new
        {
            Rezim = opcije.CurrentValue.Rezim.ToString(),
            OtisakSkraceno = otisak?[..12],
            Rizik = procena?.Rizik,
            Razlozi = procena?.Razlozi ?? []
        });
    }
}

/// <summary>Pomoćni endpointi dostupni SAMO u razvojnom okruženju.</summary>
[Route(Osnova + "/razvoj")]
[ApiExplorerSettings(IgnoreApi = false)]
public sealed class RazvojKontroler(IHostEnvironment okruzenje, MockSmsServis sms) : ApiKontroler
{
    /// <summary>[Razvoj] Poslednje poruke koje je "poslao" mock SMS servis (za testiranje verifikacije telefona).</summary>
    [HttpGet("sms")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<PoslatSms>), StatusCodes.Status200OK)]
    public IActionResult Sms() => okruzenje.IsDevelopment() ? Ok(sms.Poslate.Take(10)) : NotFound();
}
