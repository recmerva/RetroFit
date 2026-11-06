using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Razgovori;
using RetroFit.Application.Features.Trgovina;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Infrastructure.Persistencija.Repozitorijumi;

internal sealed class RezervacijaRepository(RetroFitDbContext db) : Repository<Rezervacija>(db), IRezervacijaRepository
{
    private Expression<Func<Rezervacija, RezervacijaDto>> Projekcija => r => new RezervacijaDto
    {
        Id = r.Id,
        Oglas = new OglasUkratkoDto
        {
            Id = r.Oglas.Id,
            Naslov = r.Oglas.Naslov,
            Cena = r.Oglas.Cena,
            Tip = r.Oglas.Tip,
            Status = r.Oglas.Status,
            GlavnaSlika = r.Oglas.Slike.OrderBy(s => s.Redosled).Select(s => s.Putanja).FirstOrDefault(),
            ProdavacId = r.Oglas.ProdavacId
        },
        KupacId = r.KupacId,
        KupacIme = r.Kupac.Ime + " " + r.Kupac.Prezime.Substring(0, 1) + ".",
        ProdavacId = r.ProdavacId,
        ProdavacIme = r.Prodavac.Ime + " " + r.Prodavac.Prezime.Substring(0, 1) + ".",
        PonudaId = r.PonudaId,
        DogovorenaCena = r.DogovorenaCena,
        Status = r.Status,
        KreiranoUtc = r.KreiranoUtc,
        IsticeUtc = r.IsticeUtc,
        ZavrsenoUtc = r.ZavrsenoUtc,
        RazlogOtkazivanja = r.RazlogOtkazivanja,
        TransakcijaId = Db.Transakcije.Where(t => t.RezervacijaId == r.Id).Select(t => (int?)t.Id).FirstOrDefault()
    };

    public Task<Rezervacija?> PronadjiSaOglasomAsync(int id, CancellationToken ct = default) =>
        Skup.Include(r => r.Oglas).FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Rezervacija?> AktivnaZaOglasAsync(int oglasId, CancellationToken ct = default) =>
        Skup.Include(r => r.Oglas).FirstOrDefaultAsync(r => r.OglasId == oglasId && r.Status == StatusRezervacije.Aktivna, ct);

    public async Task<IReadOnlyList<Rezervacija>> IstekleAsync(DateTime sada, int maks, CancellationToken ct = default) =>
        await Skup.Include(r => r.Oglas)
            .Where(r => r.Status == StatusRezervacije.Aktivna && r.IsticeUtc <= sada)
            .OrderBy(r => r.IsticeUtc)
            .Take(maks)
            .ToListAsync(ct);

    public Task<StranicaRezultata<RezervacijaDto>> ZaKorisnikaAsync(int korisnikId, StatusRezervacije? status, Stranicenje stranicenje,
        CancellationToken ct = default)
    {
        var q = Skup.AsNoTracking().Where(r => r.KupacId == korisnikId || r.ProdavacId == korisnikId);
        if (status is not null)
        {
            q = q.Where(r => r.Status == status);
        }

        return q.OrderByDescending(r => r.KreiranoUtc).Select(Projekcija).UStranicuAsync(stranicenje, ct);
    }

    public Task<RezervacijaDto?> DtoAsync(int id, CancellationToken ct = default) =>
        Skup.AsNoTracking().Where(r => r.Id == id).Select(Projekcija).FirstOrDefaultAsync(ct);

    public Task<bool> KupacImaPravoNaKontaktAsync(int oglasId, int kupacId, CancellationToken ct = default) =>
        Skup.AnyAsync(r => r.OglasId == oglasId && r.KupacId == kupacId
                           && (r.Status == StatusRezervacije.Aktivna || r.Status == StatusRezervacije.Realizovana), ct);
}

internal sealed class TransakcijaRepository(RetroFitDbContext db) : Repository<Transakcija>(db), ITransakcijaRepository
{
    private Expression<Func<Transakcija, TransakcijaDto>> Projekcija => t => new TransakcijaDto
    {
        Id = t.Id,
        Oglas = new OglasUkratkoDto
        {
            Id = t.Oglas.Id,
            Naslov = t.Oglas.Naslov,
            Cena = t.Oglas.Cena,
            Tip = t.Oglas.Tip,
            Status = t.Oglas.Status,
            GlavnaSlika = t.Oglas.Slike.OrderBy(s => s.Redosled).Select(s => s.Putanja).FirstOrDefault(),
            ProdavacId = t.Oglas.ProdavacId
        },
        RezervacijaId = t.RezervacijaId,
        KupacId = t.KupacId,
        KupacIme = t.Kupac.Ime + " " + t.Kupac.Prezime.Substring(0, 1) + ".",
        ProdavacId = t.ProdavacId,
        ProdavacIme = t.Prodavac.Ime + " " + t.Prodavac.Prezime.Substring(0, 1) + ".",
        Cena = t.Cena,
        NacinPreuzimanja = t.NacinPreuzimanja,
        AdresaDostave = t.AdresaDostave,
        Status = t.Status,
        StatusEscrowa = t.StatusEscrowa,
        KreiranoUtc = t.KreiranoUtc,
        KupacPotvrdioUtc = t.KupacPotvrdioUtc,
        ProdavacPotvrdioUtc = t.ProdavacPotvrdioUtc,
        ZavrsenoUtc = t.ZavrsenoUtc,
        OtkazanoUtc = t.OtkazanoUtc,
        RazlogOtkazivanja = t.RazlogOtkazivanja,
        KupacOstavioRecenziju = Db.Recenzije.Any(r => r.TransakcijaId == t.Id && r.OcenjivacId == t.KupacId),
        ProdavacOstavioRecenziju = Db.Recenzije.Any(r => r.TransakcijaId == t.Id && r.OcenjivacId == t.ProdavacId)
    };

    public Task<Transakcija?> PronadjiSaOglasomAsync(int id, CancellationToken ct = default) =>
        Skup.Include(t => t.Oglas).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<TransakcijaDto?> DetaljiAsync(int id, CancellationToken ct = default)
    {
        var dto = await Skup.AsNoTracking().Where(t => t.Id == id).Select(Projekcija).FirstOrDefaultAsync(ct);
        if (dto is null)
        {
            return null;
        }

        var placanja = await Db.Placanja.AsNoTracking()
            .Where(p => p.TransakcijaId == id)
            .OrderBy(p => p.KreiranoUtc)
            .Select(p => new PlacanjeDto
            {
                Id = p.Id,
                Tip = p.Tip,
                Status = p.Status,
                Iznos = p.Iznos,
                Valuta = p.Valuta,
                ReferencaProcesora = p.ReferencaProcesora,
                Opis = p.Opis,
                KreiranoUtc = p.KreiranoUtc
            })
            .ToListAsync(ct);

        return dto with { Placanja = placanja };
    }

    public Task<StranicaRezultata<TransakcijaDto>> ZaKorisnikaAsync(int korisnikId, StatusTransakcije? status, Stranicenje stranicenje,
        CancellationToken ct = default)
    {
        var q = Skup.AsNoTracking().Where(t => t.KupacId == korisnikId || t.ProdavacId == korisnikId);
        if (status is not null)
        {
            q = q.Where(t => t.Status == status);
        }

        return q.OrderByDescending(t => t.KreiranoUtc).Select(Projekcija).UStranicuAsync(stranicenje, ct);
    }
}

internal sealed class RecenzijaRepository(RetroFitDbContext db, IConfigurationProvider mapiranje) : Repository<Recenzija>(db), IRecenzijaRepository
{
    public Task<StranicaRezultata<RecenzijaDto>> ZaKorisnikaAsync(int korisnikId, Stranicenje stranicenje, CancellationToken ct = default) =>
        Skup.AsNoTracking()
            .Where(r => r.OcenjeniKorisnikId == korisnikId)
            .OrderByDescending(r => r.KreiranoUtc)
            .ProjectTo<RecenzijaDto>(mapiranje)
            .UStranicuAsync(stranicenje, ct);

    /// <summary>GROUP BY Ocena - raspodela ocena (koliko petica, četvorki...).</summary>
    public async Task<SazetakOcenaDto> SazetakAsync(int korisnikId, CancellationToken ct = default)
    {
        var raspodela = await Skup.AsNoTracking()
            .Where(r => r.OcenjeniKorisnikId == korisnikId)
            .GroupBy(r => r.Ocena)
            .Select(g => new { Ocena = g.Key, Broj = g.Count() })
            .ToListAsync(ct);

        var ukupno = raspodela.Sum(x => x.Broj);
        var prosek = ukupno == 0 ? 0m : Math.Round((decimal)raspodela.Sum(x => x.Ocena * x.Broj) / ukupno, 2);

        return new SazetakOcenaDto
        {
            ProsecnaOcena = prosek,
            BrojRecenzija = ukupno,
            RaspodelaOcena = Enumerable.Range(1, 5).ToDictionary(o => o, o => raspodela.FirstOrDefault(x => x.Ocena == o)?.Broj ?? 0)
        };
    }
}
