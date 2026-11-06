namespace RetroFit.Domain.Pravila;

/// <summary>
/// Pravila reputacije na jednom mestu. Koriste ih i entitet Korisnik i DTO-i koji se
/// popunjavaju SQL projekcijom (bez učitavanja celog entiteta).
/// </summary>
public static class ReputacijaPravila
{
    private const decimal VirtuelnihOcena = 5m;
    private const decimal PriorProsek = 3m;

    /// <summary>
    /// Reputacija 0-100 = Bajesov prosek ocena skaliran na 0-80 (uz 5 "virtuelnih" ocena 3.0,
    /// da jedna petica ne napravi savršenu reputaciju) + do 15 poena za završene transakcije
    /// + 5 poena za verifikovan telefon.
    /// </summary>
    public static int Izracunaj(decimal prosecnaOcena, int brojRecenzija, int brojZavrsenih, bool telefonVerifikovan)
    {
        var bajes = (VirtuelnihOcena * PriorProsek + prosecnaOcena * brojRecenzija) / (VirtuelnihOcena + brojRecenzija);
        var izOcena = (bajes - 1m) / 4m * 80m;
        var izTransakcija = Math.Min(brojZavrsenih, 15);
        var izVerifikacije = telefonVerifikovan ? 5 : 0;
        return (int)Math.Round(izOcena + izTransakcija + izVerifikacije);
    }

    /// <summary>Bedž "Pouzdan prodavac": bar 5 završenih transakcija, bar 3 recenzije i prosek bar 4.5.</summary>
    public static bool JePouzdan(decimal prosecnaOcena, int brojRecenzija, int brojZavrsenih) =>
        brojZavrsenih >= 5 && brojRecenzija >= 3 && prosecnaOcena >= 4.5m;

    public static IReadOnlyList<string> Bedzevi(bool telefonVerifikovan, bool pouzdan, DateTime clanOdUtc, DateTime sadaUtc)
    {
        var lista = new List<string>();
        if (telefonVerifikovan)
        {
            lista.Add("VerifikovanTelefon");
        }

        if (pouzdan)
        {
            lista.Add("PouzdanProdavac");
        }

        if (sadaUtc - clanOdUtc > TimeSpan.FromDays(365))
        {
            lista.Add("VisegodisnjiClan");
        }

        return lista;
    }
}
