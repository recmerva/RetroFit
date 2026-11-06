using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Prijava sumnjivog oglasa. Moderator je prihvata (oglas se uklanja) ili odbija.
/// U bazi: filtrirani UNIQUE (OglasId, PrijavioKorisnikId) WHERE Status = 'Otvorena',
/// pa jedan korisnik ne može da "spamuje" prijavama isti oglas.
/// </summary>
public class Prijava : Entitet
{
    private Prijava() { }

    public Prijava(Oglas oglas, int prijavioKorisnikId, RazlogPrijave razlog, string? opis, DateTime sada)
    {
        Provera.Uslov(!oglas.JeVlasnik(prijavioKorisnikId), "sopstveni_oglas", "Ne možete prijaviti sopstveni oglas.");
        Provera.Uslov(razlog != RazlogPrijave.Ostalo || !string.IsNullOrWhiteSpace(opis), "validacija",
            "Za razlog 'Ostalo' opis je obavezan.");
        OglasId = oglas.Id;
        PrijavioKorisnikId = prijavioKorisnikId;
        Razlog = razlog;
        Opis = Provera.OpcioniTekst(opis, "Opis", 1000);
        Status = StatusPrijave.Otvorena;
        KreiranoUtc = sada;
    }

    public int OglasId { get; private set; }
    public Oglas Oglas { get; private set; } = null!;
    public int PrijavioKorisnikId { get; private set; }
    public Korisnik PrijavioKorisnik { get; private set; } = null!;
    public RazlogPrijave Razlog { get; private set; }
    public string? Opis { get; private set; }
    public StatusPrijave Status { get; private set; }
    public DateTime KreiranoUtc { get; private set; }
    public int? ObradioModeratorId { get; private set; }
    public Korisnik? ObradioModerator { get; private set; }
    public DateTime? ObradjenoUtc { get; private set; }
    public string? NapomenaModeratora { get; private set; }

    public void Prihvati(int moderatorId, string? napomena, DateTime sada) =>
        Obradi(StatusPrijave.Prihvacena, moderatorId, napomena, sada);

    public void Odbij(int moderatorId, string? napomena, DateTime sada) =>
        Obradi(StatusPrijave.Odbijena, moderatorId, napomena, sada);

    private void Obradi(StatusPrijave status, int moderatorId, string? napomena, DateTime sada)
    {
        Provera.Uslov(Status == StatusPrijave.Otvorena, "prijava_obradjena", "Prijava je već obrađena.");
        Status = status;
        ObradioModeratorId = moderatorId;
        ObradjenoUtc = sada;
        NapomenaModeratora = Provera.OpcioniTekst(napomena, "Napomena", 500);
    }
}
