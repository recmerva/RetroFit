using System.Diagnostics;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace RetroFit.Application.Common.Ponasanja;

/// <summary>
/// MediatR pipeline behavior: pre SVAKOG handlera pokreće sve FluentValidation validatore za taj zahtev.
/// Ako validacija ne prođe, handler se uopšte ne poziva, a API vraća 400 ProblemDetails sa listom grešaka.
/// </summary>
public sealed class ValidacijaPonasanje<TZahtev, TOdgovor>(IEnumerable<IValidator<TZahtev>> validatori)
    : IPipelineBehavior<TZahtev, TOdgovor> where TZahtev : notnull
{
    public async Task<TOdgovor> Handle(TZahtev zahtev, RequestHandlerDelegate<TOdgovor> sledeci, CancellationToken ct)
    {
        if (!validatori.Any())
        {
            return await sledeci();
        }

        var kontekst = new ValidationContext<TZahtev>(zahtev);
        var rezultati = await Task.WhenAll(validatori.Select(v => v.ValidateAsync(kontekst, ct)));
        var greske = rezultati.SelectMany(r => r.Errors).Where(g => g is not null).ToList();

        if (greske.Count > 0)
        {
            throw new ValidationException(greske);
        }

        return await sledeci();
    }
}

/// <summary>
/// MediatR pipeline behavior: strukturisano logovanje svakog zahteva (naziv, trajanje, ishod).
/// Sadržaj zahteva se NE loguje (može sadržati lozinke ili lične podatke).
/// </summary>
public sealed class LogovanjePonasanje<TZahtev, TOdgovor>(ILogger<LogovanjePonasanje<TZahtev, TOdgovor>> logger)
    : IPipelineBehavior<TZahtev, TOdgovor> where TZahtev : notnull
{
    private const int SporZahtevMs = 500;

    public async Task<TOdgovor> Handle(TZahtev zahtev, RequestHandlerDelegate<TOdgovor> sledeci, CancellationToken ct)
    {
        var naziv = typeof(TZahtev).Name;
        var stoperica = Stopwatch.StartNew();
        try
        {
            var odgovor = await sledeci();
            stoperica.Stop();
            if (stoperica.ElapsedMilliseconds > SporZahtevMs)
            {
                logger.LogWarning("Spor zahtev {Zahtev} trajao {TrajanjeMs} ms", naziv, stoperica.ElapsedMilliseconds);
            }
            else
            {
                logger.LogDebug("Zahtev {Zahtev} obrađen za {TrajanjeMs} ms", naziv, stoperica.ElapsedMilliseconds);
            }

            return odgovor;
        }
        catch (Exception ex)
        {
            logger.LogInformation("Zahtev {Zahtev} završen greškom {TipGreske} posle {TrajanjeMs} ms",
                naziv, ex.GetType().Name, stoperica.ElapsedMilliseconds);
            throw;
        }
    }
}
