using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RetroFit.Application.Common.Interfejsi;

namespace RetroFit.Infrastructure.Servisi;

public sealed record PoslatSms(string BrojTelefona, string Poruka, DateTime PoslatoUtc);

/// <summary>
/// MOCK SMS servis: ne šalje pravi SMS, već poruku upisuje u log i čuva poslednjih 50 poruka u memoriji
/// (u razvojnom okruženju dostupne preko /api/v1/razvoj/sms, da bi se verifikacija mogla testirati).
/// U produkciji bi se ovde koristio pravi provajder (npr. Twilio, Infobip) iza istog interfejsa.
/// </summary>
public sealed class MockSmsServis(ILogger<MockSmsServis> logger) : ISmsServis
{
    private const int MaksPoruka = 50;
    private readonly ConcurrentQueue<PoslatSms> _poslate = new();

    public IReadOnlyList<PoslatSms> Poslate => _poslate.Reverse().ToList();

    public Task PosaljiAsync(string brojTelefona, string poruka, CancellationToken ct)
    {
        _poslate.Enqueue(new PoslatSms(brojTelefona, poruka, DateTime.UtcNow));
        while (_poslate.Count > MaksPoruka && _poslate.TryDequeue(out _))
        {
        }

        // Broj se u logu maskira (privatnost); sadržaj se loguje jer je ovo mock za razvoj.
        logger.LogInformation("[MOCK SMS] Za {Telefon}: {Poruka}", Maskiraj(brojTelefona), poruka);
        return Task.CompletedTask;
    }

    private static string Maskiraj(string broj) =>
        broj.Length <= 4 ? "****" : new string('*', broj.Length - 3) + broj[^3..];
}

/// <summary>
/// MOCK platni procesor (simulacija kartičnog plaćanja bez pravog provajdera).
/// Pravila koja važe i kod pravih procesora:
///  - svaka uspešna operacija dobija jedinstvenu referencu procesora;
///  - isti ključ idempotentnosti vraća ISTI rezultat (dupli klik ne naplaćuje dva puta);
///  - postoji limit po transakciji (iznad 1.000.000 RSD naplata se odbija).
/// </summary>
public sealed class MockPlatniProcesor(ILogger<MockPlatniProcesor> logger) : IPlatniProcesor
{
    public const decimal LimitPoTransakciji = 1_000_000m;
    private readonly ConcurrentDictionary<string, RezultatPlacanja> _idempotentni = new();

    public Task<RezultatPlacanja> NaplatiAsync(int korisnikId, decimal iznos, string opis, string? kljucIdempotentnosti, CancellationToken ct)
    {
        if (kljucIdempotentnosti is not null && _idempotentni.TryGetValue(kljucIdempotentnosti, out var prethodni))
        {
            logger.LogInformation("[MOCK PLAĆANJE] Ponovljen zahtev sa ključem {Kljuc}, vraćen prethodni rezultat.", kljucIdempotentnosti);
            return Task.FromResult(prethodni);
        }

        var rezultat = iznos > LimitPoTransakciji
            ? new RezultatPlacanja(false, NovaReferenca("ODB"), $"Iznos prelazi limit od {LimitPoTransakciji:N0} RSD.")
            : new RezultatPlacanja(true, NovaReferenca("NAP"), null);

        if (kljucIdempotentnosti is not null)
        {
            _idempotentni.TryAdd(kljucIdempotentnosti, rezultat);
        }

        logger.LogInformation("[MOCK PLAĆANJE] Naplata {Iznos} RSD od korisnika {KorisnikId}: {Uspesno} ({Referenca})",
            iznos, korisnikId, rezultat.Uspesno ? "uspešno" : "odbijeno", rezultat.ReferencaProcesora);
        return Task.FromResult(rezultat);
    }

    public Task<RezultatPlacanja> IsplatiAsync(int korisnikId, decimal iznos, string opis, CancellationToken ct)
    {
        var rezultat = new RezultatPlacanja(true, NovaReferenca("ISP"), null);
        logger.LogInformation("[MOCK PLAĆANJE] Isplata {Iznos} RSD korisniku {KorisnikId} ({Referenca})", iznos, korisnikId, rezultat.ReferencaProcesora);
        return Task.FromResult(rezultat);
    }

    public Task<RezultatPlacanja> VratiNovacAsync(int korisnikId, decimal iznos, string opis, CancellationToken ct)
    {
        var rezultat = new RezultatPlacanja(true, NovaReferenca("POV"), null);
        logger.LogInformation("[MOCK PLAĆANJE] Povraćaj {Iznos} RSD korisniku {KorisnikId} ({Referenca})", iznos, korisnikId, rezultat.ReferencaProcesora);
        return Task.FromResult(rezultat);
    }

    private static string NovaReferenca(string prefiks) => $"MOCK-{prefiks}-{Guid.NewGuid():N}";
}
