namespace RetroFit.API.Middleware.BotDetekcija;

internal static class BotDetekcijaRegistracija
{
    public static IServiceCollection AddBotDetekcija(this IServiceCollection services, IConfiguration konfiguracija)
    {
        var sekcija = konfiguracija.GetSection(BotDetekcijaOpcije.Sekcija);
        services.Configure<BotDetekcijaOpcije>(sekcija);

        var opcije = sekcija.Get<BotDetekcijaOpcije>() ?? new BotDetekcijaOpcije();
        if (opcije.Rezim != RezimBotDetekcije.Iskljuceno && opcije.TajniKljuc.Length < 32)
        {
            throw new InvalidOperationException("BotDetekcija:TajniKljuc mora imati najmanje 32 znaka.");
        }

        services.AddMemoryCache();
        services.AddSingleton<PracenjePonasanja>();
        services.AddSingleton<JsIzazovServis>();
        services.AddHostedService<BotDetekcijaSinhronizacija>();
        return services;
    }
}
