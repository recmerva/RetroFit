using System.Data;

namespace RetroFit.Application.Common.Interfejsi.Persistencija;

/// <summary>
/// Unit of Work: jedno mesto za sve repozitorijume i za potvrđivanje izmena.
/// Sve izmene napravljene preko repozitorijuma upisuju se u bazu tek pozivom SacuvajAsync,
/// i to atomično (sve ili ništa).
///
/// Greške baze se prevode u aplikacione izuzetke:
///  - DbUpdateConcurrencyException (RowVersion)       -> KonfliktIzuzetak (409)
///  - kršenje UNIQUE indeksa (SQL greške 2601/2627)   -> KonfliktIzuzetak (409)
///  - kršenje CHECK/FK ograničenja (SQL greška 547)   -> KonfliktIzuzetak (409)
/// </summary>
public interface IUnitOfWork
{
    IKorisnikRepository Korisnici { get; }
    IRepository<Domain.Entiteti.Uloga> Uloge { get; }
    IRefreshTokenRepository RefreshTokeni { get; }
    IRepository<Domain.Entiteti.VerifikacijaKorisnika> Verifikacije { get; }
    IBlokadaRepository Blokade { get; }
    IGradRepository Gradovi { get; }
    IKategorijaRepository Kategorije { get; }
    IOglasRepository Oglasi { get; }
    IOmiljeniRepository Omiljeni { get; }
    ISacuvanaPretragaRepository SacuvanePretrage { get; }
    IIstaknutiOglasRepository IstaknutiOglasi { get; }
    IRazgovorRepository Razgovori { get; }
    IPonudaRepository Ponude { get; }
    IRezervacijaRepository Rezervacije { get; }
    ITransakcijaRepository Transakcije { get; }
    IRepository<Domain.Entiteti.Placanje> Placanja { get; }
    IRecenzijaRepository Recenzije { get; }
    IZahtevTrazimRepository Zahtevi { get; }
    IPrijavaRepository Prijave { get; }
    IObavestenjeRepository Obavestenja { get; }
    IOtisakUredjajaRepository OtisciUredjaja { get; }
    IAuditRepository Audit { get; }
    IStatistikaRepository Statistika { get; }

    Task<int> SacuvajAsync(CancellationToken ct = default);

    /// <summary>
    /// Izvršava operaciju u eksplicitnoj transakciji baze (sa execution strategy-jem za ponovne pokušaje).
    /// Koristi se kada se između izmena čita stanje koje ne sme da se promeni (npr. prihvatanje ponude).
    /// </summary>
    Task<T> UTransakcijiAsync<T>(Func<CancellationToken, Task<T>> operacija, IsolationLevel nivo = IsolationLevel.ReadCommitted,
        CancellationToken ct = default);
}
