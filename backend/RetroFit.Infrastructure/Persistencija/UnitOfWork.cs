using System.Data;
using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Domain.Entiteti;
using RetroFit.Infrastructure.Persistencija.Repozitorijumi;
using RetroFit.Infrastructure.Pretraga;

namespace RetroFit.Infrastructure.Persistencija;

/// <summary>
/// Unit of Work nad jednim DbContext-om. Svi repozitorijumi dele ISTI kontekst, pa jedan poziv
/// SacuvajAsync upisuje sve izmene iz jedne poslovne operacije atomično (jedna SQL transakcija).
/// </summary>
internal sealed class UnitOfWork(
    RetroFitDbContext db,
    IConfigurationProvider mapiranje,
    IFullTextDetektor fullText,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    // Poruke za poznata ograničenja baze: korisnik dobija razumljivo objašnjenje umesto SQL greške.
    private static readonly Dictionary<string, string> PorukeOgranicenja = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UX_Korisnici_NormalizovanEmail"] = "Nalog sa ovom email adresom već postoji.",
        ["UX_Korisnici_Telefon"] = "Ovaj broj telefona već koristi drugi nalog.",
        ["UX_Rezervacije_JednaAktivnaPoOglasu"] = "Oglas je upravo rezervisan za drugog kupca.",
        ["UX_Rezervacije_PonudaId"] = "Za ovu ponudu rezervacija već postoji.",
        ["UX_Transakcije_OglasNeotkazana"] = "Oglas je već prodat ili je kupovina u toku.",
        ["UX_Transakcije_RezervacijaId"] = "Kupovina za ovu rezervaciju je već pokrenuta.",
        ["UX_Recenzije_Transakcija_Ocenjivac"] = "Već ste ostavili recenziju za ovu transakciju.",
        ["UX_Ponude_JednaNaCekanju"] = "U ovom razgovoru već postoji ponuda koja čeka odgovor.",
        ["UX_Prijave_JednaOtvorenaPoKorisniku"] = "Već ste prijavili ovaj oglas; prijava čeka obradu.",
        ["UX_Razgovori_Oglas_Kupac"] = "Razgovor za ovaj oglas već postoji.",
        ["UX_OdgovoriNaZahteve_Zahtev_Oglas"] = "Ovaj oglas je već ponuđen za ovaj zahtev.",
        ["UX_Placanja_KljucIdempotentnosti"] = "Ovo plaćanje je već obrađeno.",
        ["UX_Kategorije_Slug"] = "Kategorija sa ovim slug-om već postoji.",
        ["UX_SlikeOglasa_Oglas_Redosled"] = "Redosled slika je u međuvremenu promenjen. Pokušajte ponovo.",
        ["PK_OmiljeniOglasi"] = "Oglas je već u omiljenima.",
        ["UX_DefinicijeAtributa_Kategorija_Kljuc"] = "Atribut sa ovim ključem već postoji u kategoriji."
    };

    private IKorisnikRepository? _korisnici;
    private IRepository<Uloga>? _uloge;
    private IRefreshTokenRepository? _refreshTokeni;
    private IRepository<VerifikacijaKorisnika>? _verifikacije;
    private IBlokadaRepository? _blokade;
    private IGradRepository? _gradovi;
    private IKategorijaRepository? _kategorije;
    private IOglasRepository? _oglasi;
    private IOmiljeniRepository? _omiljeni;
    private ISacuvanaPretragaRepository? _sacuvanePretrage;
    private IIstaknutiOglasRepository? _istaknuti;
    private IRazgovorRepository? _razgovori;
    private IPonudaRepository? _ponude;
    private IRezervacijaRepository? _rezervacije;
    private ITransakcijaRepository? _transakcije;
    private IRepository<Placanje>? _placanja;
    private IRecenzijaRepository? _recenzije;
    private IZahtevTrazimRepository? _zahtevi;
    private IPrijavaRepository? _prijave;
    private IObavestenjeRepository? _obavestenja;
    private IOtisakUredjajaRepository? _otisci;
    private IAuditRepository? _audit;
    private IStatistikaRepository? _statistika;

    public IKorisnikRepository Korisnici => _korisnici ??= new KorisnikRepository(db);
    public IRepository<Uloga> Uloge => _uloge ??= new Repository<Uloga>(db);
    public IRefreshTokenRepository RefreshTokeni => _refreshTokeni ??= new RefreshTokenRepository(db);
    public IRepository<VerifikacijaKorisnika> Verifikacije => _verifikacije ??= new Repository<VerifikacijaKorisnika>(db);
    public IBlokadaRepository Blokade => _blokade ??= new BlokadaRepository(db);
    public IGradRepository Gradovi => _gradovi ??= new GradRepository(db, mapiranje);
    public IKategorijaRepository Kategorije => _kategorije ??= new KategorijaRepository(db);
    public IOglasRepository Oglasi => _oglasi ??= new OglasRepository(db, fullText);
    public IOmiljeniRepository Omiljeni => _omiljeni ??= new OmiljeniRepository(db);
    public ISacuvanaPretragaRepository SacuvanePretrage => _sacuvanePretrage ??= new SacuvanaPretragaRepository(db, mapiranje);
    public IIstaknutiOglasRepository IstaknutiOglasi => _istaknuti ??= new IstaknutiOglasRepository(db);
    public IRazgovorRepository Razgovori => _razgovori ??= new RazgovorRepository(db, mapiranje);
    public IPonudaRepository Ponude => _ponude ??= new PonudaRepository(db);
    public IRezervacijaRepository Rezervacije => _rezervacije ??= new RezervacijaRepository(db);
    public ITransakcijaRepository Transakcije => _transakcije ??= new TransakcijaRepository(db);
    public IRepository<Placanje> Placanja => _placanja ??= new Repository<Placanje>(db);
    public IRecenzijaRepository Recenzije => _recenzije ??= new RecenzijaRepository(db, mapiranje);
    public IZahtevTrazimRepository Zahtevi => _zahtevi ??= new ZahtevTrazimRepository(db, fullText);
    public IPrijavaRepository Prijave => _prijave ??= new PrijavaRepository(db);
    public IObavestenjeRepository Obavestenja => _obavestenja ??= new ObavestenjeRepository(db, mapiranje);
    public IOtisakUredjajaRepository OtisciUredjaja => _otisci ??= new OtisakUredjajaRepository(db, mapiranje);
    public IAuditRepository Audit => _audit ??= new AuditRepository(db, mapiranje);
    public IStatistikaRepository Statistika => _statistika ??= new StatistikaRepository(db);

    public async Task<int> SacuvajAsync(CancellationToken ct = default)
    {
        try
        {
            return await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // RowVersion se razlikuje: neko je izmenio isti red između našeg čitanja i upisa.
            logger.LogWarning(ex, "Konflikt konkurentnosti pri čuvanju ({Entiteti})",
                string.Join(", ", ex.Entries.Select(e => e.Metadata.ClrType.Name)));
            throw new KonfliktIzuzetak("Podatak je u međuvremenu izmenjen. Osvežite stranicu i pokušajte ponovo.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
        {
            throw PrevediSqlGresku(sql, ex);
        }
    }

    public async Task<T> UTransakcijiAsync<T>(Func<CancellationToken, Task<T>> operacija, IsolationLevel nivo = IsolationLevel.ReadCommitted,
        CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            return await operacija(ct);
        }

        // Execution strategy (ponovni pokušaj kod prolaznih grešaka) mora da obuhvati celu transakciju.
        var strategija = db.Database.CreateExecutionStrategy();
        return await strategija.ExecuteAsync(async () =>
        {
            await using var transakcija = await db.Database.BeginTransactionAsync(nivo, ct);
            try
            {
                var rezultat = await operacija(ct);
                await transakcija.CommitAsync(ct);
                return rezultat;
            }
            catch
            {
                await transakcija.RollbackAsync(CancellationToken.None);
                db.ChangeTracker.Clear();
                throw;
            }
        });
    }

    private Exception PrevediSqlGresku(SqlException sql, DbUpdateException original)
    {
        switch (sql.Number)
        {
            // 2601 = duplikat u UNIQUE indeksu, 2627 = kršenje UNIQUE/PRIMARY KEY ograničenja.
            case 2601 or 2627:
            {
                var poruka = PorukeOgranicenja.FirstOrDefault(p => sql.Message.Contains(p.Key, StringComparison.OrdinalIgnoreCase)).Value
                             ?? "Podatak sa istim vrednostima već postoji.";
                logger.LogInformation("UNIQUE ograničenje sprečilo upis: {Poruka}", sql.Message);
                return new KonfliktIzuzetak(poruka);
            }
            // 547 = kršenje CHECK ili FOREIGN KEY ograničenja.
            case 547:
                logger.LogWarning("CHECK/FK ograničenje sprečilo upis: {Poruka}", sql.Message);
                return new KonfliktIzuzetak("Izmena krši pravilo integriteta podataka i nije sačuvana.");
            default:
                return original;
        }
    }
}
