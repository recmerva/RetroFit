using Microsoft.EntityFrameworkCore;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Korisnici;
using RetroFit.Application.Features.Moderacija;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Infrastructure.Persistencija.Repozitorijumi;

internal sealed class KorisnikRepository(RetroFitDbContext db) : Repository<Korisnik>(db), IKorisnikRepository
{
    public Task<Korisnik?> PronadjiPoEmailuAsync(string normalizovanEmail, CancellationToken ct = default) =>
        Skup.Include(k => k.Uloge).ThenInclude(u => u.Uloga)
            .FirstOrDefaultAsync(k => k.NormalizovanEmail == normalizovanEmail && !k.Obrisan, ct);

    public Task<Korisnik?> PronadjiSaUlogamaAsync(int id, CancellationToken ct = default) =>
        Skup.Include(k => k.Uloge).ThenInclude(u => u.Uloga).FirstOrDefaultAsync(k => k.Id == id, ct);

    public Task<bool> PostojiEmailAsync(string normalizovanEmail, CancellationToken ct = default) =>
        Skup.AnyAsync(k => k.NormalizovanEmail == normalizovanEmail, ct);

    public Task<bool> TelefonZauzetAsync(string telefon, int osimKorisnikaId, CancellationToken ct = default) =>
        Skup.AnyAsync(k => k.Telefon == telefon && k.Id != osimKorisnikaId && !k.Obrisan, ct);

    public async Task<IReadOnlyList<string>> UlogeKorisnikaAsync(int korisnikId, CancellationToken ct = default) =>
        await Db.KorisnikUloge.Where(ku => ku.KorisnikId == korisnikId).Select(ku => ku.Uloga.Naziv).ToListAsync(ct);

    public Task<MojProfilDto?> MojProfilAsync(int id, CancellationToken ct = default) =>
        Skup.AsNoTracking()
            .Where(k => k.Id == id && !k.Obrisan)
            .Select(k => new MojProfilDto
            {
                Id = k.Id,
                Email = k.Email,
                Ime = k.Ime,
                Prezime = k.Prezime,
                PrikaznoIme = k.Ime + " " + k.Prezime.Substring(0, 1) + ".",
                OKorisniku = k.OKorisniku,
                GradId = k.GradId,
                GradNaziv = k.Grad != null ? k.Grad.Naziv : null,
                Telefon = k.Telefon,
                TelefonVerifikovan = k.TelefonVerifikovanUtc != null,
                PrikaziTelefonKupcu = k.PrikaziTelefonKupcu,
                Uloge = k.Uloge.Select(u => u.Uloga.Naziv).ToList(),
                ProsecnaOcena = k.ProsecnaOcena,
                BrojRecenzija = k.BrojRecenzija,
                BrojZavrsenihTransakcija = k.BrojZavrsenihTransakcija,
                KreiranoUtc = k.KreiranoUtc
            })
            .FirstOrDefaultAsync(ct);

    public Task<JavniProfilDto?> JavniProfilAsync(int id, CancellationToken ct = default) =>
        Skup.AsNoTracking()
            .Where(k => k.Id == id && !k.Obrisan)
            .Select(Projekcije.JavniProfil(Db))
            .FirstOrDefaultAsync(ct);

    public Task<StranicaRezultata<KorisnikAdminDto>> PretraziZaAdminaAsync(string? upit, Stranicenje stranicenje, DateTime sada,
        CancellationToken ct = default)
    {
        var q = Skup.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(upit))
        {
            var u = upit.Trim();
            q = q.Where(k => k.Email.Contains(u) || k.Ime.Contains(u) || k.Prezime.Contains(u));
        }

        return q.OrderByDescending(k => k.KreiranoUtc)
            .Select(k => new KorisnikAdminDto
            {
                Id = k.Id,
                Email = k.Email,
                ImePrezime = k.Ime + " " + k.Prezime,
                Uloge = k.Uloge.Select(x => x.Uloga.Naziv).ToList(),
                KreiranoUtc = k.KreiranoUtc,
                PoslednjaPrijavaUtc = k.PoslednjaPrijavaUtc,
                Blokiran = Db.BlokiraniKorisnici.Any(b => b.KorisnikId == k.Id && b.UkinutoUtc == null && (b.KrajUtc == null || b.KrajUtc > sada)),
                Obrisan = k.Obrisan,
                BrojOglasa = Db.Oglasi.Count(o => o.ProdavacId == k.Id && !o.Obrisan)
            })
            .UStranicuAsync(stranicenje, ct);
    }

    /// <summary>
    /// UPDATE Korisnici SET ProsecnaOcena = ROUND((ProsecnaOcena * BrojRecenzija + @ocena) / (BrojRecenzija + 1), 2),
    ///                      BrojRecenzija = BrojRecenzija + 1 WHERE Id = @id
    /// U SQL-u svi izrazi u SET delu koriste STARE vrednosti kolona, pa je formula ispravna.
    /// </summary>
    public Task DodajOcenuAsync(int korisnikId, int ocena, CancellationToken ct = default) =>
        Skup.Where(k => k.Id == korisnikId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(k => k.ProsecnaOcena, k => Math.Round((k.ProsecnaOcena * k.BrojRecenzija + ocena) / (k.BrojRecenzija + 1), 2))
                .SetProperty(k => k.BrojRecenzija, k => k.BrojRecenzija + 1), ct);

    public Task UvecajZavrseneTransakcijeAsync(IReadOnlyCollection<int> korisnici, CancellationToken ct = default) =>
        Skup.Where(k => korisnici.Contains(k.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.BrojZavrsenihTransakcija, k => k.BrojZavrsenihTransakcija + 1), ct);
}

internal sealed class RefreshTokenRepository(RetroFitDbContext db) : Repository<RefreshToken>(db), IRefreshTokenRepository
{
    public Task<RefreshToken?> PronadjiPoHashuAsync(string hash, CancellationToken ct = default) =>
        Skup.Include(r => r.Korisnik).ThenInclude(k => k.Uloge).ThenInclude(u => u.Uloga)
            .FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

    /// <summary>Set-based UPDATE: opoziva sve aktivne tokene iz porodice jednom SQL naredbom.</summary>
    public Task<int> OpozoviPorodicuAsync(Guid porodicaId, string razlog, DateTime sada, CancellationToken ct = default) =>
        Skup.Where(r => r.PorodicaId == porodicaId && r.OpozvanoUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.OpozvanoUtc, sada).SetProperty(r => r.RazlogOpoziva, razlog), ct);

    public Task<int> OpozoviSveZaKorisnikaAsync(int korisnikId, string razlog, DateTime sada, CancellationToken ct = default) =>
        Skup.Where(r => r.KorisnikId == korisnikId && r.OpozvanoUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.OpozvanoUtc, sada).SetProperty(r => r.RazlogOpoziva, razlog), ct);

    public Task<int> ObrisiZastareleAsync(DateTime starijeOd, CancellationToken ct = default) =>
        Skup.Where(r => r.IsticeUtc < starijeOd).ExecuteDeleteAsync(ct);
}

internal sealed class BlokadaRepository(RetroFitDbContext db) : Repository<BlokadaKorisnika>(db), IBlokadaRepository
{
    public Task<BlokadaKorisnika?> AktivnaZaKorisnikaAsync(int korisnikId, DateTime sada, CancellationToken ct = default) =>
        Skup.Where(b => b.KorisnikId == korisnikId && b.UkinutoUtc == null && (b.KrajUtc == null || b.KrajUtc > sada))
            .OrderByDescending(b => b.PocetakUtc)
            .FirstOrDefaultAsync(ct);

    public Task<StranicaRezultata<BlokadaKorisnikaDto>> ListaAsync(bool samoAktivne, Stranicenje stranicenje, DateTime sada,
        CancellationToken ct = default)
    {
        var q = Skup.AsNoTracking();
        if (samoAktivne)
        {
            q = q.Where(b => b.UkinutoUtc == null && (b.KrajUtc == null || b.KrajUtc > sada));
        }

        return q.OrderByDescending(b => b.PocetakUtc)
            .Select(b => new BlokadaKorisnikaDto
            {
                Id = b.Id,
                KorisnikId = b.KorisnikId,
                KorisnikIme = b.Korisnik.Ime + " " + b.Korisnik.Prezime,
                KorisnikEmail = b.Korisnik.Email,
                BlokiraoIme = b.BlokiraoKorisnik.Ime + " " + b.BlokiraoKorisnik.Prezime,
                Razlog = b.Razlog,
                PocetakUtc = b.PocetakUtc,
                KrajUtc = b.KrajUtc,
                UkinutoUtc = b.UkinutoUtc,
                Aktivna = b.UkinutoUtc == null && (b.KrajUtc == null || b.KrajUtc > sada)
            })
            .UStranicuAsync(stranicenje, ct);
    }
}
