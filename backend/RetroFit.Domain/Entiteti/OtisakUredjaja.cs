using RetroFit.Domain.Common;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// "Otisak" (fingerprint) klijenta koji koristi Bot Detection middleware.
///
/// PRIVATNOST: ne čuvaju se sirovi podaci (rezolucija, jezik, IP...), već samo HMAC-SHA256 heš
/// kombinacije grubih signala sa tajnim ključem servera. Iz heša se ne mogu rekonstruisati podaci.
///
/// VAŽNO: otisak NIJE pouzdan jedinstveni identifikator uređaja. Više uređaja može imati isti otisak
/// (isti model telefona, isti browser), a jedan uređaj može promeniti otisak (novi browser, VPN).
/// Zato se koristi samo kao jedan od signala u proceni rizika, nikad kao jedini dokaz.
/// </summary>
public class OtisakUredjaja : Entitet
{
    private OtisakUredjaja() { }

    public OtisakUredjaja(string otisakHash, string? opisKlijenta, DateTime sada)
    {
        OtisakHash = otisakHash;
        OpisKlijenta = opisKlijenta is { Length: > 200 } ? opisKlijenta[..200] : opisKlijenta;
        PrviPutVidjenUtc = sada;
        PoslednjiPutVidjenUtc = sada;
    }

    public string OtisakHash { get; private set; } = string.Empty;
    /// <summary>Gruba porodica klijenta (npr. "Chrome / Windows"), ne pun User-Agent.</summary>
    public string? OpisKlijenta { get; private set; }
    public DateTime PrviPutVidjenUtc { get; private set; }
    public DateTime PoslednjiPutVidjenUtc { get; private set; }
    public long BrojZahteva { get; private set; }
    public int BrojRegistracija { get; private set; }
    public int NajveciRizik { get; private set; }
    public int? PoslednjiKorisnikId { get; private set; }

    public void Zabelezi(int brojNovihZahteva, int rizik, int? korisnikId, DateTime sada, string? opisKlijenta = null)
    {
        OpisKlijenta ??= opisKlijenta is { Length: > 200 } ? opisKlijenta[..200] : opisKlijenta;
        BrojZahteva += brojNovihZahteva;
        NajveciRizik = Math.Max(NajveciRizik, Math.Clamp(rizik, 0, 100));
        PoslednjiKorisnikId = korisnikId ?? PoslednjiKorisnikId;
        PoslednjiPutVidjenUtc = sada;
    }

    public void ZabeleziRegistraciju() => BrojRegistracija++;
}

/// <summary>
/// Blokada otiska uređaja: ručna (moderator) ili automatska (middleware posle ponovljenog sumnjivog ponašanja).
/// </summary>
public class BlokadaUredjaja : Entitet
{
    private BlokadaUredjaja() { }

    public BlokadaUredjaja(int otisakUredjajaId, string razlog, DateTime sada, DateTime? krajUtc, int? blokiraoKorisnikId)
    {
        Provera.Uslov(krajUtc is null || krajUtc > sada, "validacija", "Kraj blokade mora biti u budućnosti.");
        OtisakUredjajaId = otisakUredjajaId;
        Razlog = Provera.Tekst(razlog, "Razlog", 500);
        PocetakUtc = sada;
        KrajUtc = krajUtc;
        BlokiraoKorisnikId = blokiraoKorisnikId;
        Automatska = blokiraoKorisnikId is null;
    }

    public int OtisakUredjajaId { get; private set; }
    public OtisakUredjaja OtisakUredjaja { get; private set; } = null!;
    public string Razlog { get; private set; } = string.Empty;
    public DateTime PocetakUtc { get; private set; }
    public DateTime? KrajUtc { get; private set; }
    public bool Automatska { get; private set; }
    public int? BlokiraoKorisnikId { get; private set; }
    public Korisnik? BlokiraoKorisnik { get; private set; }
    public DateTime? UkinutoUtc { get; private set; }
    public int? UkinuoKorisnikId { get; private set; }

    public bool JeAktivna(DateTime sada) => UkinutoUtc is null && (KrajUtc is null || KrajUtc > sada);

    public void Ukini(int moderatorId, DateTime sada)
    {
        Provera.Uslov(JeAktivna(sada), "blokada_neaktivna", "Blokada nije aktivna.");
        UkinutoUtc = sada;
        UkinuoKorisnikId = moderatorId;
    }
}
