using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Common.Interfejsi;

/// <summary>Apstrakcija trenutnog vremena, da bi testovi mogli da "zamrznu" vreme.</summary>
public interface IDatumVreme
{
    DateTime SadaUtc { get; }
}

/// <summary>Podaci o korisniku koji šalje trenutni HTTP zahtev (iz JWT tokena).</summary>
public interface ITrenutniKorisnik
{
    int? Id { get; }
    bool JePrijavljen { get; }
    bool JeUUlozi(string uloga);
    string? IpAdresa { get; }
    string? KorelacioniId { get; }
    /// <summary>Heš otiska uređaja koji je izračunao Bot Detection middleware (ako postoji).</summary>
    string? OtisakUredjaja { get; }

    /// <summary>Id prijavljenog korisnika ili izuzetak 401 ako korisnik nije prijavljen.</summary>
    int ObavezanId { get; }
}

public interface IHesiranjeLozinke
{
    string Hesiraj(string lozinka);
    bool Proveri(string hes, string lozinka);
}

public interface ITokenServis
{
    (string Token, DateTime IsticeUtc) KreirajAccessToken(Korisnik korisnik, IEnumerable<string> uloge);

    /// <summary>Generiše kriptografski slučajan refresh token (vraća se klijentu) i njegov heš (čuva se u bazi).</summary>
    (string Token, string Hash, DateTime IsticeUtc) KreirajRefreshToken();

    string Hesiraj(string vrednost);
}

/// <summary>Slanje SMS poruka. U ovom projektu implementacija je mock (upisuje u log).</summary>
public interface ISmsServis
{
    Task PosaljiAsync(string brojTelefona, string poruka, CancellationToken ct);
}

public sealed record SacuvanFajl(string JavnaPutanja, string TipSadrzaja, long VelicinaBajtova);

/// <summary>Skladište za otpremljene slike (lokalni disk / Docker volume).</summary>
public interface ISkladisteFajlova
{
    Task<SacuvanFajl> SacuvajSlikuAsync(Stream sadrzaj, string originalniNaziv, string tipSadrzaja, string folder, CancellationToken ct);
    Task ObrisiAsync(string javnaPutanja, CancellationToken ct);
}

public sealed record RezultatPlacanja(bool Uspesno, string ReferencaProcesora, string? Poruka);

/// <summary>
/// Platni procesor. Implementacija je MOCK (bez pravog provajdera), ali ima realna pravila:
/// idempotentnost, referencu transakcije i mogućnost neuspeha.
/// </summary>
public interface IPlatniProcesor
{
    Task<RezultatPlacanja> NaplatiAsync(int korisnikId, decimal iznos, string opis, string? kljucIdempotentnosti, CancellationToken ct);
    Task<RezultatPlacanja> IsplatiAsync(int korisnikId, decimal iznos, string opis, CancellationToken ct);
    Task<RezultatPlacanja> VratiNovacAsync(int korisnikId, decimal iznos, string opis, CancellationToken ct);
}

/// <summary>Kreiranje obaveštenja za korisnike (upisuje se u bazu u okviru iste Unit of Work transakcije).</summary>
public interface IObavestenjaServis
{
    Task ObavestiAsync(int korisnikId, TipObavestenja tip, string naslov, string sadrzaj, string? link, CancellationToken ct);
}

/// <summary>Eksplicitni audit zapisi za bezbednosne i moderatorske događaje.</summary>
public interface IAuditServis
{
    Task ZabeleziAsync(KategorijaAudita kategorija, string akcija, string? nazivEntiteta, string? entitetId,
        object? detalji, CancellationToken ct);
}
