using RetroFit.Domain.Common;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Refresh token. U bazi se čuva SAMO SHA-256 heš tokena, nikada sam token,
/// pa ni krađa baze ne daje upotrebljive tokene.
///
/// Rotacija: svako osvežavanje opoziva stari token i izdaje novi iz iste "porodice" (PorodicaId).
/// Detekcija ponovne upotrebe: ako stigne već opozvan token, opoziva se cela porodica (znak krađe).
/// </summary>
public class RefreshToken : Entitet
{
    private RefreshToken() { }

    public RefreshToken(int korisnikId, string tokenHash, Guid porodicaId, DateTime sada, DateTime isticeUtc, string? kreiranSaIp)
    {
        KorisnikId = korisnikId;
        TokenHash = tokenHash;
        PorodicaId = porodicaId;
        KreiranoUtc = sada;
        IsticeUtc = isticeUtc;
        KreiranSaIp = kreiranSaIp;
    }

    public int KorisnikId { get; private set; }
    public Korisnik Korisnik { get; private set; } = null!;
    public string TokenHash { get; private set; } = string.Empty;
    public Guid PorodicaId { get; private set; }
    public DateTime KreiranoUtc { get; private set; }
    public DateTime IsticeUtc { get; private set; }
    public DateTime? OpozvanoUtc { get; private set; }
    public string? RazlogOpoziva { get; private set; }
    public string? ZamenjenHashom { get; private set; }
    public string? KreiranSaIp { get; private set; }

    public bool JeOpozvan => OpozvanoUtc.HasValue;
    public bool JeIstekao(DateTime sada) => sada >= IsticeUtc;
    public bool JeAktivan(DateTime sada) => !JeOpozvan && !JeIstekao(sada);

    /// <summary>
    /// Token je opozvan redovnom rotacijom pre manje od zadatog vremena. Takav zahtev je skoro sigurno
    /// istovremeno osvežavanje istog klijenta (dva taba, dupli zahtev pri učitavanju), a ne krađa.
    /// </summary>
    public bool JeNedavnoRotiran(DateTime sada, TimeSpan tolerancija) =>
        RazlogOpoziva == RazlogRotacije && OpozvanoUtc is { } opozvano && sada - opozvano <= tolerancija;

    public const string RazlogRotacije = "Rotacija";

    public void Opozovi(DateTime sada, string razlog, string? zamenjenHashom = null)
    {
        if (JeOpozvan)
        {
            return;
        }

        OpozvanoUtc = sada;
        RazlogOpoziva = razlog;
        ZamenjenHashom = zamenjenHashom;
    }
}
