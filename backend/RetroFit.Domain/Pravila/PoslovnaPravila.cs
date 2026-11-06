namespace RetroFit.Domain.Pravila;

/// <summary>
/// Centralno mesto za brojčane poslovne konstante, da ne budu "magični brojevi" po kodu.
/// </summary>
public static class PoslovnaPravila
{
    public const int TrajanjeOglasaDana = 60;
    public const int ObnovaMogucaDanaPreIsteka = 7;
    public const int MaksSlikaPoOglasu = 10;
    public const int MaksVelicinaSlikeBajtova = 5 * 1024 * 1024;

    public const int TrajanjePonudeSati = 48;
    public const int TrajanjeRezervacijeSati = 72;
    public const int RokZaRecenzijuDana = 30;

    public const int MaksSacuvanihPretraga = 20;
    public const int MaksPokusajaVerifikacije = 5;
    public const int TrajanjeVerifikacionogKodaMinuta = 10;

    /// <summary>
    /// Koliko sekundi posle rotacije se stari refresh token prihvata kao istovremeni zahtev
    /// (dva taba, dupli zahtev pri učitavanju), a ne kao krađa.
    /// </summary>
    public const int TolerancijaRotacijeTokenaSekundi = 30;

    public const decimal CenaPodizanjaPoDanu = 99m;
    public const int MaksDanaPodizanja = 30;

    public const decimal MaksCena = 100_000_000m;
}
