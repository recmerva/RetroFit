using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RetroFit.Infrastructure.Persistencija;

namespace RetroFit.Infrastructure.Pretraga;

/// <summary>Da li je SQL Server Full-Text Search dostupan (komponenta instalirana + FT indeks postoji).</summary>
public interface IFullTextDetektor
{
    bool Dostupan { get; }
    Task ProveriAsync(RetroFitDbContext db, CancellationToken ct);
}

/// <summary>
/// Proverava jednom pri pokretanju aplikacije. Ako FTS nije dostupan (npr. SQL Server bez
/// Full-Text komponente), pretraga automatski koristi LIKE fallback.
/// Podešavanje "Pretraga:FullText" = "auto" | "uvek" | "nikad".
/// </summary>
public sealed class FullTextDetektor(ILogger<FullTextDetektor> logger, string rezim = "auto") : IFullTextDetektor
{
    public bool Dostupan { get; private set; }

    public async Task ProveriAsync(RetroFitDbContext db, CancellationToken ct)
    {
        if (rezim.Equals("nikad", StringComparison.OrdinalIgnoreCase))
        {
            Dostupan = false;
            logger.LogInformation("Full-Text pretraga je isključena podešavanjem; koristi se LIKE.");
            return;
        }

        try
        {
            var rezultat = await db.Database.SqlQueryRaw<int>(
                    """
                    SELECT CASE WHEN FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
                                 AND EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.Oglasi'))
                            THEN 1 ELSE 0 END AS Value
                    """)
                .ToListAsync(ct);
            Dostupan = rezultat.FirstOrDefault() == 1;
        }
        catch (Exception ex)
        {
            Dostupan = false;
            logger.LogWarning(ex, "Provera Full-Text podrške nije uspela; koristi se LIKE.");
        }

        if (!Dostupan && rezim.Equals("uvek", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Full-Text Search je obavezan (Pretraga:FullText = uvek), ali nije dostupan.");
        }

        logger.LogInformation("Pretraga oglasa koristi {Nacin}.", Dostupan ? "SQL Server Full-Text Search" : "LIKE fallback");
    }
}

/// <summary>
/// Pravljenje Full-Text uslova iz korisničkog unosa. Korisnički tekst se NIKAD ne ubacuje direktno:
/// zadržavaju se samo slova i cifre, pa unos poput  a" OR "b  ne može da promeni smisao upita.
/// </summary>
public static class FullTextUpit
{
    private const int MaksReci = 6;

    public static IReadOnlyList<string> Reci(string? upit)
    {
        if (string.IsNullOrWhiteSpace(upit))
        {
            return [];
        }

        var reci = new List<string>();
        var trenutna = new StringBuilder();
        foreach (var c in upit.Trim().ToLowerInvariant().Append(' '))
        {
            if (char.IsLetterOrDigit(c))
            {
                trenutna.Append(c);
                continue;
            }

            if (trenutna.Length >= 2 && !reci.Contains(trenutna.ToString()))
            {
                reci.Add(trenutna.ToString());
            }

            trenutna.Clear();
            if (reci.Count == MaksReci)
            {
                break;
            }
        }

        return reci;
    }

    /// <summary>"crveni bicikl" -> "crveni*" AND "bicikl*"  (prefiksna pretraga svake reči).</summary>
    public static string? Napravi(string? upit)
    {
        var reci = Reci(upit);
        return reci.Count == 0 ? null : string.Join(" AND ", reci.Select(r => $"\"{r}*\""));
    }
}
