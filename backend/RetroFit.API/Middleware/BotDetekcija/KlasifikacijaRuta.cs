namespace RetroFit.API.Middleware.BotDetekcija;

/// <summary>Grupe ruta sa različitim limitima i različitom osetljivošću.</summary>
public enum GrupaRute
{
    Ignorisi,
    Registracija,
    Prijava,
    Izazov,
    Poruke,
    Pisanje,
    Pretraga,
    PregledOglasa,
    Opste
}

public static class KlasifikacijaRuta
{
    /// <summary>Osetljive grupe: ovde visok rizik traži rešen JS izazov.</summary>
    public static bool JeOsetljiva(GrupaRute g) => g is GrupaRute.Registracija or GrupaRute.Prijava or GrupaRute.Poruke or GrupaRute.Pisanje;

    public static (GrupaRute Grupa, int? OglasId) Klasifikuj(string metoda, PathString putanja)
    {
        var p = (putanja.Value ?? string.Empty).ToLowerInvariant().TrimEnd('/');

        if (p.StartsWith("/uploads") || p.StartsWith("/swagger") || p.StartsWith("/health") || !p.StartsWith("/api/"))
        {
            return (GrupaRute.Ignorisi, null);
        }

        // "/api/v1/oglasi/15" -> ["oglasi", "15"]
        var delovi = p.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(2).ToArray();
        var jeGet = HttpMethods.IsGet(metoda) || HttpMethods.IsHead(metoda);

        return delovi switch
        {
            ["bezbednost", "izazov", ..] => (GrupaRute.Izazov, null),
            ["autentifikacija", "registracija"] when !jeGet => (GrupaRute.Registracija, null),
            ["autentifikacija", "prijava"] when !jeGet => (GrupaRute.Prijava, null),
            ["razgovori"] when !jeGet => (GrupaRute.Poruke, null),
            ["razgovori", _, "poruke"] when !jeGet => (GrupaRute.Poruke, null),
            ["oglasi"] when jeGet => (GrupaRute.Pretraga, null),
            ["zahtevi"] when jeGet => (GrupaRute.Pretraga, null),
            ["oglasi", var id] when jeGet && int.TryParse(id, out var oglasId) => (GrupaRute.PregledOglasa, oglasId),
            _ when !jeGet && !HttpMethods.IsOptions(metoda) => (GrupaRute.Pisanje, null),
            _ => (GrupaRute.Opste, null)
        };
    }
}
