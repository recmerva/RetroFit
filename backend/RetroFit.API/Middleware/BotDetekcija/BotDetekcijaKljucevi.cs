namespace RetroFit.API.Middleware.BotDetekcija;

/// <summary>Nazivi HTTP zaglavlja i ključeva koje koristi Bot Detection middleware.</summary>
public static class BotDetekcijaKljucevi
{
    /// <summary>HttpContext.Items ključ pod kojim middleware ostavlja heš otiska uređaja za ostatak pipeline-a.</summary>
    public const string KljucOtiska = "RetroFit.OtisakUredjaja";
    public const string KljucRizika = "RetroFit.Rizik";

    /// <summary>Grubi signali sa frontenda (rezolucija, vremenska zona, jezik, platforma...) kao JSON.</summary>
    public const string ZaglavljeSignala = "X-Client-Signals";
    /// <summary>Token dobijen rešavanjem JavaScript proof-of-work izazova.</summary>
    public const string ZaglavljeIzazova = "X-Challenge-Token";
    /// <summary>JA3/JA4 TLS otisak - prihvata se SAMO od pouzdanog reverse proxy-ja (vidi dokumentaciju).</summary>
    public const string ZaglavljeTlsOtiska = "X-TLS-Fingerprint";
    public const string ZaglavljeRizika = "X-Risk-Score";
}
