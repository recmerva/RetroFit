using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Administracija;
using RetroFit.Application.Features.Moderacija;
using RetroFit.Application.Features.Zahtevi;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Infrastructure.Pretraga;

namespace RetroFit.Infrastructure.Persistencija.Repozitorijumi;

internal sealed class ZahtevTrazimRepository(RetroFitDbContext db, IFullTextDetektor fullText) : Repository<ZahtevTrazim>(db), IZahtevTrazimRepository
{
    public Task<ZahtevTrazim?> PronadjiSaOdgovorimaAsync(int id, CancellationToken ct = default) =>
        Skup.Include(z => z.Odgovori).FirstOrDefaultAsync(z => z.Id == id, ct);

    public Task<StranicaRezultata<ZahtevDto>> PretraziAsync(string? upit, IReadOnlyList<int>? kategorije, int? gradId, int? korisnikId,
        StatusZahteva? status, Stranicenje stranicenje, CancellationToken ct = default)
    {
        var q = Skup.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(upit))
        {
            var uslov = FullTextUpit.Napravi(upit);
            if (fullText.Dostupan && uslov is not null)
            {
                q = q.Where(z => EF.Functions.Contains(z.Naslov, uslov) || EF.Functions.Contains(z.Opis, uslov));
            }
            else
            {
                foreach (var rec in FullTextUpit.Reci(upit))
                {
                    q = q.Where(z => z.Naslov.Contains(rec) || z.Opis.Contains(rec));
                }
            }
        }

        if (kategorije is { Count: > 0 })
        {
            q = q.Where(z => kategorije.Contains(z.KategorijaId));
        }

        if (gradId is not null)
        {
            q = q.Where(z => z.GradId == gradId);
        }

        if (korisnikId is not null)
        {
            q = q.Where(z => z.KorisnikId == korisnikId);
        }

        if (status is not null)
        {
            q = q.Where(z => z.Status == status);
        }

        return q.OrderByDescending(z => z.KreiranoUtc).Select(Projekcija()).UStranicuAsync(stranicenje, ct);
    }

    public Task<ZahtevDto?> DtoAsync(int id, CancellationToken ct = default) =>
        Skup.AsNoTracking().Where(z => z.Id == id).Select(Projekcija()).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<OdgovorNaZahtev>> OdgovoriAsync(int zahtevId, CancellationToken ct = default) =>
        await Db.OdgovoriNaZahteve.AsNoTracking()
            .Where(o => o.ZahtevId == zahtevId)
            .OrderByDescending(o => o.KreiranoUtc)
            .ToListAsync(ct);

    private static System.Linq.Expressions.Expression<Func<ZahtevTrazim, ZahtevDto>> Projekcija() => z => new ZahtevDto
    {
        Id = z.Id,
        Naslov = z.Naslov,
        Opis = z.Opis,
        KategorijaId = z.KategorijaId,
        KategorijaNaziv = z.Kategorija.Naziv,
        GradId = z.GradId,
        GradNaziv = z.Grad != null ? z.Grad.Naziv : null,
        MaksimalnaCena = z.MaksimalnaCena,
        Status = z.Status,
        KreiranoUtc = z.KreiranoUtc,
        KorisnikId = z.KorisnikId,
        KorisnikIme = z.Korisnik.Ime + " " + z.Korisnik.Prezime.Substring(0, 1) + ".",
        BrojOdgovora = z.Odgovori.Count
    };
}

internal sealed class PrijavaRepository(RetroFitDbContext db) : Repository<Prijava>(db), IPrijavaRepository
{
    public Task<StranicaRezultata<PrijavaDto>> ListaAsync(StatusPrijave? status, Stranicenje stranicenje, CancellationToken ct = default)
    {
        var q = Skup.AsNoTracking();
        if (status is not null)
        {
            q = q.Where(p => p.Status == status);
        }

        return q
            // Prvo oglasi sa najviše otvorenih prijava, pa najstarije prijave (FIFO).
            .OrderByDescending(p => Db.Prijave.Count(x => x.OglasId == p.OglasId && x.Status == StatusPrijave.Otvorena))
            .ThenBy(p => p.KreiranoUtc)
            .Select(p => new PrijavaDto
            {
                Id = p.Id,
                OglasId = p.OglasId,
                OglasNaslov = p.Oglas.Naslov,
                OglasStatus = p.Oglas.Status,
                ProdavacId = p.Oglas.ProdavacId,
                ProdavacIme = p.Oglas.Prodavac.Ime + " " + p.Oglas.Prodavac.Prezime,
                PrijavioKorisnikId = p.PrijavioKorisnikId,
                PrijavioIme = p.PrijavioKorisnik.Ime + " " + p.PrijavioKorisnik.Prezime,
                Razlog = p.Razlog,
                Opis = p.Opis,
                Status = p.Status,
                KreiranoUtc = p.KreiranoUtc,
                ObradioIme = p.ObradioModerator != null ? p.ObradioModerator.Ime + " " + p.ObradioModerator.Prezime : null,
                ObradjenoUtc = p.ObradjenoUtc,
                NapomenaModeratora = p.NapomenaModeratora,
                BrojOtvorenihPrijavaZaOglas = Db.Prijave.Count(x => x.OglasId == p.OglasId && x.Status == StatusPrijave.Otvorena)
            })
            .UStranicuAsync(stranicenje, ct);
    }

    public async Task<IReadOnlyList<Prijava>> OtvoreneZaOglasAsync(int oglasId, CancellationToken ct = default) =>
        await Skup.Where(p => p.OglasId == oglasId && p.Status == StatusPrijave.Otvorena).ToListAsync(ct);
}

internal sealed class OtisakUredjajaRepository(RetroFitDbContext db, IConfigurationProvider mapiranje)
    : Repository<OtisakUredjaja>(db), IOtisakUredjajaRepository
{
    public Task<OtisakUredjaja?> PoHashuAsync(string hash, CancellationToken ct = default) =>
        Skup.FirstOrDefaultAsync(o => o.OtisakHash == hash, ct);

    public async Task<IReadOnlyList<string>> AktivnoBlokiraniHesheviAsync(DateTime sada, CancellationToken ct = default) =>
        await Db.BlokiraniUredjaji.AsNoTracking()
            .Where(b => b.UkinutoUtc == null && (b.KrajUtc == null || b.KrajUtc > sada))
            .Select(b => b.OtisakUredjaja.OtisakHash)
            .Distinct()
            .ToListAsync(ct);

    public async Task DodajBlokaduAsync(BlokadaUredjaja blokada, CancellationToken ct = default) =>
        await Db.BlokiraniUredjaji.AddAsync(blokada, ct);

    public Task<BlokadaUredjaja?> BlokadaPoIdAsync(int id, CancellationToken ct = default) =>
        Db.BlokiraniUredjaji.Include(b => b.OtisakUredjaja).FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<bool> ImaAktivnuBlokaduAsync(int otisakId, DateTime sada, CancellationToken ct = default) =>
        Db.BlokiraniUredjaji.AnyAsync(b => b.OtisakUredjajaId == otisakId && b.UkinutoUtc == null && (b.KrajUtc == null || b.KrajUtc > sada), ct);

    public Task<StranicaRezultata<OtisakUredjajaDto>> SumnjiviAsync(int minRizik, Stranicenje stranicenje, DateTime sada,
        CancellationToken ct = default) =>
        Skup.AsNoTracking()
            .Where(o => o.NajveciRizik >= minRizik)
            .OrderByDescending(o => o.NajveciRizik).ThenByDescending(o => o.PoslednjiPutVidjenUtc)
            .Select(o => new OtisakUredjajaDto
            {
                Id = o.Id,
                OtisakSkraceno = o.OtisakHash.Substring(0, 12),
                OpisKlijenta = o.OpisKlijenta,
                PrviPutVidjenUtc = o.PrviPutVidjenUtc,
                PoslednjiPutVidjenUtc = o.PoslednjiPutVidjenUtc,
                BrojZahteva = o.BrojZahteva,
                BrojRegistracija = o.BrojRegistracija,
                NajveciRizik = o.NajveciRizik,
                Blokiran = Db.BlokiraniUredjaji.Any(b => b.OtisakUredjajaId == o.Id && b.UkinutoUtc == null && (b.KrajUtc == null || b.KrajUtc > sada))
            })
            .UStranicuAsync(stranicenje, ct);

    public Task<StranicaRezultata<BlokadaUredjajaDto>> BlokadeAsync(bool samoAktivne, Stranicenje stranicenje, DateTime sada,
        CancellationToken ct = default)
    {
        var q = Db.BlokiraniUredjaji.AsNoTracking();
        if (samoAktivne)
        {
            q = q.Where(b => b.UkinutoUtc == null && (b.KrajUtc == null || b.KrajUtc > sada));
        }

        return q.OrderByDescending(b => b.PocetakUtc).ProjectTo<BlokadaUredjajaDto>(mapiranje).UStranicuAsync(stranicenje, ct);
    }
}

internal sealed class AuditRepository(RetroFitDbContext db, IConfigurationProvider mapiranje) : IAuditRepository
{
    public async Task DodajAsync(AuditZapis zapis, CancellationToken ct = default) => await db.AuditZapisi.AddAsync(zapis, ct);

    public Task<StranicaRezultata<AuditZapisDto>> ListaAsync(FilterAudita f, Stranicenje stranicenje, CancellationToken ct = default)
    {
        var q = db.AuditZapisi.AsNoTracking();
        if (f.Kategorija is not null)
        {
            q = q.Where(a => a.Kategorija == f.Kategorija);
        }

        if (f.KorisnikId is not null)
        {
            q = q.Where(a => a.KorisnikId == f.KorisnikId);
        }

        if (!string.IsNullOrWhiteSpace(f.NazivEntiteta))
        {
            q = q.Where(a => a.NazivEntiteta == f.NazivEntiteta);
        }

        if (f.OdUtc is not null)
        {
            q = q.Where(a => a.KreiranoUtc >= f.OdUtc);
        }

        if (f.DoUtc is not null)
        {
            q = q.Where(a => a.KreiranoUtc <= f.DoUtc);
        }

        return q.OrderByDescending(a => a.KreiranoUtc).ThenByDescending(a => a.Id)
            .ProjectTo<AuditZapisDto>(mapiranje)
            .UStranicuAsync(stranicenje, ct);
    }
}

internal sealed class StatistikaRepository(RetroFitDbContext db) : IStatistikaRepository
{
    public async Task<StatistikaDto> IzracunajAsync(DateTime sada, CancellationToken ct = default)
    {
        var pre7 = sada.AddDays(-7);
        var pre24h = sada.AddHours(-24);
        var pre14Dana = sada.Date.AddDays(-13);

        var oglasiPoStatusu = await db.Oglasi.AsNoTracking().Where(o => !o.Obrisan)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Broj = g.Count() })
            .ToListAsync(ct);
        int BrojOglasa(StatusOglasa s) => oglasiPoStatusu.FirstOrDefault(x => x.Status == s)?.Broj ?? 0;

        // Aktivni oglasi grupisani po GLAVNOJ kategoriji (podkategorija se pripisuje roditelju).
        var poKategorijama = await db.Oglasi.AsNoTracking()
            .Where(o => !o.Obrisan && o.Status == StatusOglasa.Aktivan)
            .GroupBy(o => o.Kategorija.Roditelj != null ? o.Kategorija.Roditelj.Naziv : o.Kategorija.Naziv)
            .Select(g => new { Naziv = g.Key, Broj = g.Count() })
            .OrderByDescending(x => x.Broj)
            .ToListAsync(ct);

        var poDanima = await db.Transakcije.AsNoTracking()
            .Where(t => t.Status == StatusTransakcije.Zavrsena && t.ZavrsenoUtc >= pre14Dana)
            .GroupBy(t => t.ZavrsenoUtc!.Value.Date)
            .Select(g => new { Dan = g.Key, Broj = g.Count() })
            .ToListAsync(ct);

        var uspesnaPlacanja = await db.Placanja.AsNoTracking()
            .Where(p => p.Status == StatusPlacanja.Uspesno)
            .GroupBy(p => p.Tip)
            .Select(g => new { Tip = g.Key, Iznos = g.Sum(p => p.Iznos) })
            .ToListAsync(ct);

        return new StatistikaDto
        {
            UkupnoKorisnika = await db.Korisnici.CountAsync(k => !k.Obrisan, ct),
            NoviKorisnici7Dana = await db.Korisnici.CountAsync(k => !k.Obrisan && k.KreiranoUtc >= pre7, ct),
            AktivniOglasi = BrojOglasa(StatusOglasa.Aktivan),
            RezervisaniOglasi = BrojOglasa(StatusOglasa.Rezervisan),
            ProdatiOglasi = BrojOglasa(StatusOglasa.Prodat),
            NoviOglasi7Dana = await db.Oglasi.CountAsync(o => !o.Obrisan && o.ObjavljenoUtc >= pre7, ct),
            ZavrseneTransakcije = await db.Transakcije.CountAsync(t => t.Status == StatusTransakcije.Zavrsena, ct),
            UkupanPromet = await db.Transakcije.Where(t => t.Status == StatusTransakcije.Zavrsena).SumAsync(t => t.Cena ?? 0, ct),
            PrometEscrow = uspesnaPlacanja.FirstOrDefault(x => x.Tip == TipPlacanja.EscrowUplata)?.Iznos ?? 0,
            PrihodOdIsticanja = uspesnaPlacanja.FirstOrDefault(x => x.Tip == TipPlacanja.Isticanje)?.Iznos ?? 0,
            OtvorenePrijave = await db.Prijave.CountAsync(p => p.Status == StatusPrijave.Otvorena, ct),
            AktivneBlokadeKorisnika = await db.BlokiraniKorisnici.CountAsync(b => b.UkinutoUtc == null && (b.KrajUtc == null || b.KrajUtc > sada), ct),
            AktivneBlokadeUredjaja = await db.BlokiraniUredjaji.CountAsync(b => b.UkinutoUtc == null && (b.KrajUtc == null || b.KrajUtc > sada), ct),
            BezbednosniDogadjaji24h = await db.AuditZapisi.CountAsync(a => a.Kategorija == KategorijaAudita.Bezbednost && a.KreiranoUtc >= pre24h, ct),
            ProsecnaOcenaPlatforme = await db.Recenzije.AnyAsync(ct)
                ? Math.Round((decimal)await db.Recenzije.AverageAsync(r => (double)r.Ocena, ct), 2)
                : 0,
            OglasiPoKategorijama = poKategorijama.Select(x => new StavkaStatistikeDto(x.Naziv, x.Broj)).ToList(),
            TransakcijePoDanima = Enumerable.Range(0, 14)
                .Select(i => pre14Dana.AddDays(i))
                .Select(dan => new StavkaStatistikeDto(dan.ToString("dd.MM."), poDanima.FirstOrDefault(x => x.Dan == dan)?.Broj ?? 0))
                .ToList()
        };
    }
}
