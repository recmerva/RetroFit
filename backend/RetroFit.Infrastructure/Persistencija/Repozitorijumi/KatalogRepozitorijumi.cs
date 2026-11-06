using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Katalog;
using RetroFit.Application.Features.Oglasi;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Infrastructure.Persistencija.Repozitorijumi;

internal sealed class GradRepository(RetroFitDbContext db, IConfigurationProvider mapiranje) : Repository<Grad>(db), IGradRepository
{
    public async Task<IReadOnlyList<GradDto>> SviAsync(CancellationToken ct = default) =>
        await Skup.AsNoTracking().OrderBy(g => g.Naziv).ProjectTo<GradDto>(mapiranje).ToListAsync(ct);
}

internal sealed class KategorijaRepository(RetroFitDbContext db) : Repository<Kategorija>(db), IKategorijaRepository
{
    public async Task<IReadOnlyList<KategorijaDto>> SveRavnoAsync(bool samoAktivne, CancellationToken ct = default)
    {
        var q = Skup.AsNoTracking();
        if (samoAktivne)
        {
            q = q.Where(k => k.Aktivna);
        }

        return await q.OrderBy(k => k.Redosled).ThenBy(k => k.Naziv)
            .Select(k => new KategorijaDto
            {
                Id = k.Id,
                Naziv = k.Naziv,
                Slug = k.Slug,
                Ikonica = k.Ikonica,
                Redosled = k.Redosled,
                Aktivna = k.Aktivna,
                RoditeljId = k.RoditeljId,
                BrojAktivnihOglasa = Db.Oglasi.Count(o => o.KategorijaId == k.Id && !o.Obrisan && o.Status == StatusOglasa.Aktivan)
            })
            .ToListAsync(ct);
    }

    public Task<Kategorija?> PronadjiSaAtributimaAsync(int id, CancellationToken ct = default) =>
        Skup.Include(k => k.DefinicijeAtributa).FirstOrDefaultAsync(k => k.Id == id, ct);

    /// <summary>
    /// Rekurzivni CTE "naviše": kategorija + svi njeni preci (podkategorija nasleđuje atribute nadređene).
    /// </summary>
    public async Task<IReadOnlyList<DefinicijaAtributa>> DefinicijeZaKategorijuAsync(int kategorijaId, CancellationToken ct = default)
    {
        var preci = await Db.Database.SqlQuery<int>(
                $"""
                 WITH Preci AS (
                     SELECT Id, RoditeljId FROM dbo.Kategorije WHERE Id = {kategorijaId}
                     UNION ALL
                     SELECT k.Id, k.RoditeljId FROM dbo.Kategorije k INNER JOIN Preci p ON k.Id = p.RoditeljId
                 )
                 SELECT Id AS Value FROM Preci
                 """)
            .ToListAsync(ct);

        return await Db.DefinicijeAtributa.Where(d => preci.Contains(d.KategorijaId))
            .OrderBy(d => d.Redosled).ThenBy(d => d.Id)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Rekurzivni CTE "naniže": kategorija + sve podkategorije na svim nivoima
    /// (pretraga po "Elektronika" vraća i oglase iz "Mobilni telefoni").
    /// </summary>
    public async Task<IReadOnlyList<int>> IdSaPodkategorijamaAsync(int kategorijaId, CancellationToken ct = default) =>
        await Db.Database.SqlQuery<int>(
                $"""
                 WITH Stablo AS (
                     SELECT Id FROM dbo.Kategorije WHERE Id = {kategorijaId}
                     UNION ALL
                     SELECT k.Id FROM dbo.Kategorije k INNER JOIN Stablo s ON k.RoditeljId = s.Id
                 )
                 SELECT Id AS Value FROM Stablo
                 """)
            .ToListAsync(ct);

    public Task<bool> ImaPodkategorijeAsync(int id, CancellationToken ct = default) =>
        Skup.AnyAsync(k => k.RoditeljId == id, ct);
}

internal sealed class OmiljeniRepository(RetroFitDbContext db) : IOmiljeniRepository
{
    public Task<OmiljeniOglas?> PronadjiAsync(int korisnikId, int oglasId, CancellationToken ct = default) =>
        db.OmiljeniOglasi.FirstOrDefaultAsync(o => o.KorisnikId == korisnikId && o.OglasId == oglasId, ct);

    public async Task DodajAsync(OmiljeniOglas omiljeni, CancellationToken ct = default) =>
        await db.OmiljeniOglasi.AddAsync(omiljeni, ct);

    public void Ukloni(OmiljeniOglas omiljeni) => db.OmiljeniOglasi.Remove(omiljeni);

    public async Task<StranicaRezultata<OglasKarticaDto>> OmiljeniKorisnikaAsync(int korisnikId, Stranicenje stranicenje, DateTime sada,
        CancellationToken ct = default)
    {
        // Omiljeni prikazuju i oglase koji više nisu aktivni (npr. prodat), da korisnik vidi šta se desilo.
        var q = db.OmiljeniOglasi.AsNoTracking()
            .Where(o => o.KorisnikId == korisnikId && !o.Oglas.Obrisan)
            .OrderByDescending(o => o.DodatoUtc)
            .Select(o => o.Oglas)
            .Select(Projekcije.Kartica(db, sada, null));
        return await q.UStranicuAsync(stranicenje, ct);
    }

    public async Task<IReadOnlyList<int>> IdOmiljenihAsync(int korisnikId, IReadOnlyCollection<int> idOglasa, CancellationToken ct = default) =>
        idOglasa.Count == 0
            ? []
            : await db.OmiljeniOglasi.Where(o => o.KorisnikId == korisnikId && idOglasa.Contains(o.OglasId))
                .Select(o => o.OglasId).ToListAsync(ct);
}

internal sealed class SacuvanaPretragaRepository(RetroFitDbContext db, IConfigurationProvider mapiranje)
    : Repository<SacuvanaPretraga>(db), ISacuvanaPretragaRepository
{
    public async Task<IReadOnlyList<SacuvanaPretragaDto>> ZaKorisnikaAsync(int korisnikId, CancellationToken ct = default) =>
        await Skup.AsNoTracking().Where(p => p.KorisnikId == korisnikId)
            .OrderByDescending(p => p.KreiranoUtc)
            .ProjectTo<SacuvanaPretragaDto>(mapiranje)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SacuvanaPretraga>> ZaObavestavanjeAsync(DateTime proveraPre, int maks, CancellationToken ct = default) =>
        await Skup.Where(p => p.Obavestavaj && p.PoslednjaProveraUtc < proveraPre)
            .OrderBy(p => p.PoslednjaProveraUtc)
            .Take(maks)
            .ToListAsync(ct);
}

internal sealed class IstaknutiOglasRepository(RetroFitDbContext db) : Repository<IstaknutiOglas>(db), IIstaknutiOglasRepository
{
    public async Task<IReadOnlyList<int>> IzdvojeniIdAsync(int maks, DateTime sada, CancellationToken ct = default)
    {
        var redovi = await Skup.AsNoTracking()
            .Where(i => i.OtkazanoUtc == null && i.PocetakUtc <= sada && i.KrajUtc > sada
                        && i.Oglas.Status == StatusOglasa.Aktivan && !i.Oglas.Obrisan)
            // Admin izdvojeni prvo, pa plaćena podizanja; unutar grupe najnovije prvo.
            .OrderBy(i => i.Tip == TipIsticanja.Izdvojen ? 0 : 1).ThenByDescending(i => i.PocetakUtc)
            .Select(i => i.OglasId)
            .Take(maks * 3)
            .ToListAsync(ct);

        // Isti oglas može imati i izdvajanje i podizanje - zadržavamo prvo pojavljivanje (redosled je očuvan).
        return redovi.Distinct().Take(maks).ToList();
    }

    public Task<bool> ImaAktivnoAsync(int oglasId, TipIsticanja tip, DateTime sada, CancellationToken ct = default) =>
        Skup.AnyAsync(i => i.OglasId == oglasId && i.Tip == tip && i.OtkazanoUtc == null && i.PocetakUtc <= sada && i.KrajUtc > sada, ct);

    public Task<StranicaRezultata<IstaknutiOglasDto>> ListaAsync(bool samoAktivna, Stranicenje stranicenje, DateTime sada,
        CancellationToken ct = default)
    {
        var q = Skup.AsNoTracking();
        if (samoAktivna)
        {
            q = q.Where(i => i.OtkazanoUtc == null && i.KrajUtc > sada);
        }

        return q.OrderByDescending(i => i.PocetakUtc)
            .Select(i => new IstaknutiOglasDto
            {
                Id = i.Id,
                OglasId = i.OglasId,
                OglasNaslov = i.Oglas.Naslov,
                Tip = i.Tip,
                PocetakUtc = i.PocetakUtc,
                KrajUtc = i.KrajUtc,
                OtkazanoUtc = i.OtkazanoUtc,
                KreiraoIme = i.KreiraoKorisnik.Ime + " " + i.KreiraoKorisnik.Prezime,
                PlaceniIznos = i.Placanje != null ? i.Placanje.Iznos : null
            })
            .UStranicuAsync(stranicenje, ct);
    }
}
