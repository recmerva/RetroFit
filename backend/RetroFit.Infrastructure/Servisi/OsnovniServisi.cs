using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Infrastructure.Servisi;

internal sealed class DatumVreme : IDatumVreme
{
    public DateTime SadaUtc => DateTime.UtcNow;
}

/// <summary>
/// Heširanje lozinki preko Microsoft PasswordHasher-a (ASP.NET Core Identity, format V3):
/// PBKDF2 + HMAC-SHA512, 100.000 iteracija, nasumična so (salt) od 128 bita za svaku lozinku.
/// Ne pišemo sopstvenu kriptografiju - koristimo proverenu biblioteku.
/// </summary>
internal sealed class HesiranjeLozinke : IHesiranjeLozinke
{
    private static readonly object Korisnik = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hesiraj(string lozinka) => _hasher.HashPassword(Korisnik, lozinka);

    public bool Proveri(string hes, string lozinka)
    {
        if (string.IsNullOrEmpty(hes) || hes == "OBRISAN")
        {
            return false;
        }

        try
        {
            return _hasher.VerifyHashedPassword(Korisnik, hes, lozinka) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

/// <summary>
/// Eksplicitni audit zapisi (bezbednost, moderacija, administracija). Upisuju se zajedno sa
/// ostalim izmenama pri sledećem SacuvajAsync (ista transakcija).
/// </summary>
internal sealed class AuditServis(IUnitOfWork uow, ITrenutniKorisnik korisnik, IDatumVreme vreme) : IAuditServis
{
    // Relaxed encoder: čitljiv JSON u bazi (š, ć, + se ne pretvaraju u \uXXXX). Sadržaj se u UI-ju prikazuje kao tekst.
    private static readonly JsonSerializerOptions Opcije = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public Task ZabeleziAsync(KategorijaAudita kategorija, string akcija, string? nazivEntiteta, string? entitetId,
        object? detalji, CancellationToken ct)
    {
        var zapis = new AuditZapis(kategorija, akcija, nazivEntiteta, entitetId, korisnik.Id, null,
            detalji is null ? null : JsonSerializer.Serialize(detalji, Opcije),
            korisnik.IpAdresa, korisnik.KorelacioniId, vreme.SadaUtc);
        return uow.Audit.DodajAsync(zapis, ct);
    }
}
