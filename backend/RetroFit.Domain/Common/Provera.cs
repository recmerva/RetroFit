namespace RetroFit.Domain.Common;

/// <summary>
/// Pomoćne metode za proveru invarijanti unutar domenskih entiteta.
/// </summary>
internal static class Provera
{
    public static void Uslov(bool uslov, string kod, string poruka)
    {
        if (!uslov)
        {
            throw new DomenskiIzuzetak(kod, poruka);
        }
    }

    public static string Tekst(string? vrednost, string naziv, int maksDuzina)
    {
        if (string.IsNullOrWhiteSpace(vrednost))
        {
            throw new DomenskiIzuzetak("validacija", $"Polje '{naziv}' je obavezno.");
        }

        var ociscen = vrednost.Trim();
        if (ociscen.Length > maksDuzina)
        {
            throw new DomenskiIzuzetak("validacija", $"Polje '{naziv}' može imati najviše {maksDuzina} karaktera.");
        }

        return ociscen;
    }

    public static string? OpcioniTekst(string? vrednost, string naziv, int maksDuzina)
    {
        if (string.IsNullOrWhiteSpace(vrednost))
        {
            return null;
        }

        return Tekst(vrednost, naziv, maksDuzina);
    }
}
