using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Obavestenja;
using RetroFit.Application.Features.Razgovori;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Infrastructure.Persistencija.Repozitorijumi;

internal sealed class RazgovorRepository(RetroFitDbContext db, IConfigurationProvider mapiranje) : Repository<Razgovor>(db), IRazgovorRepository
{
    public Task<Razgovor?> PronadjiZaOglasIKupcaAsync(int oglasId, int kupacId, CancellationToken ct = default) =>
        Skup.FirstOrDefaultAsync(r => r.OglasId == oglasId && r.KupacId == kupacId, ct);

    public Task<Razgovor?> PronadjiSaOglasomAsync(int id, CancellationToken ct = default) =>
        Skup.Include(r => r.Oglas).FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<StranicaRezultata<RazgovorPregledDto>> ZaKorisnikaAsync(int korisnikId, Stranicenje stranicenje, CancellationToken ct = default) =>
        Skup.AsNoTracking()
            .Where(r => r.KupacId == korisnikId || r.ProdavacId == korisnikId)
            .OrderByDescending(r => r.PoslednjaPorukaUtc)
            .Select(r => new RazgovorPregledDto
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
                SagovornikId = r.KupacId == korisnikId ? r.ProdavacId : r.KupacId,
                SagovornikIme = r.KupacId == korisnikId
                    ? r.Prodavac.Ime + " " + r.Prodavac.Prezime.Substring(0, 1) + "."
                    : r.Kupac.Ime + " " + r.Kupac.Prezime.Substring(0, 1) + ".",
                JaSamKupac = r.KupacId == korisnikId,
                PoslednjaPoruka = r.Poruke.OrderByDescending(p => p.PoslatoUtc).Select(p => p.Sadrzaj).FirstOrDefault(),
                PoslednjaPorukaUtc = r.PoslednjaPorukaUtc,
                BrojNeprocitanih = r.Poruke.Count(p => p.PosiljalacId != korisnikId && p.ProcitanoUtc == null)
            })
            .UStranicuAsync(stranicenje, ct);

    public async Task<RazgovorDetaljiDto?> DetaljiAsync(int id, int korisnikId, CancellationToken ct = default)
    {
        var detalji = await Skup.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RazgovorDetaljiDto
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
                JaSamKupac = r.KupacId == korisnikId,
                KreiranoUtc = r.KreiranoUtc,
                AktivnaRezervacijaId = Db.Rezervacije
                    .Where(x => x.OglasId == r.OglasId && x.KupacId == r.KupacId && x.Status == StatusRezervacije.Aktivna)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        if (detalji is null)
        {
            return null;
        }

        var ponude = await Db.Ponude.AsNoTracking()
            .Where(p => p.RazgovorId == id)
            .OrderByDescending(p => p.KreiranoUtc)
            .Select(PonudaRepository.Projekcija)
            .ToListAsync(ct);

        return detalji with { Ponude = ponude };
    }

    /// <summary>Najnovije poruke prve (straničenje unazad kroz istoriju razgovora).</summary>
    public Task<StranicaRezultata<PorukaDto>> PorukeAsync(int razgovorId, Stranicenje stranicenje, CancellationToken ct = default) =>
        Db.Poruke.AsNoTracking()
            .Where(p => p.RazgovorId == razgovorId)
            .OrderByDescending(p => p.PoslatoUtc).ThenByDescending(p => p.Id)
            .ProjectTo<PorukaDto>(mapiranje)
            .UStranicuAsync(stranicenje, ct);

    /// <summary>Set-based UPDATE: sve tuđe nepročitane poruke u razgovoru označava pročitanim.</summary>
    public Task<int> OznaciProcitaneAsync(int razgovorId, int korisnikId, DateTime sada, CancellationToken ct = default) =>
        Db.Poruke.Where(p => p.RazgovorId == razgovorId && p.PosiljalacId != korisnikId && p.ProcitanoUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ProcitanoUtc, sada), ct);

    public Task<int> BrojNeprocitanihAsync(int korisnikId, CancellationToken ct = default) =>
        Db.Poruke.CountAsync(p => p.ProcitanoUtc == null && p.PosiljalacId != korisnikId
                                  && (p.Razgovor.KupacId == korisnikId || p.Razgovor.ProdavacId == korisnikId), ct);
}

internal sealed class PonudaRepository(RetroFitDbContext db) : Repository<Ponuda>(db), IPonudaRepository
{
    internal static readonly Expression<Func<Ponuda, PonudaDto>> Projekcija = p => new PonudaDto
    {
        Id = p.Id,
        OglasId = p.OglasId,
        OglasNaslov = p.Oglas.Naslov,
        CenaOglasa = p.Oglas.Cena,
        OglasSlika = p.Oglas.Slike.OrderBy(s => s.Redosled).Select(s => s.Putanja).FirstOrDefault(),
        RazgovorId = p.RazgovorId,
        KupacId = p.KupacId,
        KupacIme = p.Kupac.Ime + " " + p.Kupac.Prezime.Substring(0, 1) + ".",
        ProdavacId = p.Razgovor.ProdavacId,
        ProdavacIme = p.Razgovor.Prodavac.Ime + " " + p.Razgovor.Prodavac.Prezime.Substring(0, 1) + ".",
        PonudioKorisnikId = p.PonudioKorisnikId,
        Iznos = p.Iznos,
        Napomena = p.Napomena,
        Status = p.Status,
        KreiranoUtc = p.KreiranoUtc,
        IsticeUtc = p.IsticeUtc,
        OdgovorenoUtc = p.OdgovorenoUtc,
        PrethodnaPonudaId = p.PrethodnaPonudaId
    };

    public Task<Ponuda?> PronadjiSaRazgovoromAsync(int id, CancellationToken ct = default) =>
        Skup.Include(p => p.Razgovor).Include(p => p.Oglas).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Ponuda>> NaCekanjuZaOglasAsync(int oglasId, CancellationToken ct = default) =>
        await Skup.Include(p => p.Razgovor)
            .Where(p => p.OglasId == oglasId && p.Status == StatusPonude.NaCekanju)
            .ToListAsync(ct);

    public Task<Ponuda?> NaCekanjuURazgovoruAsync(int razgovorId, CancellationToken ct = default) =>
        Skup.FirstOrDefaultAsync(p => p.RazgovorId == razgovorId && p.Status == StatusPonude.NaCekanju, ct);

    public Task<StranicaRezultata<PonudaDto>> ZaKorisnikaAsync(int korisnikId, bool? kaoKupac, StatusPonude? status,
        Stranicenje stranicenje, CancellationToken ct = default)
    {
        var q = Skup.AsNoTracking();
        q = kaoKupac switch
        {
            true => q.Where(p => p.KupacId == korisnikId),
            false => q.Where(p => p.Razgovor.ProdavacId == korisnikId),
            null => q.Where(p => p.KupacId == korisnikId || p.Razgovor.ProdavacId == korisnikId)
        };

        if (status is not null)
        {
            q = q.Where(p => p.Status == status);
        }

        return q.OrderByDescending(p => p.KreiranoUtc).Select(Projekcija).UStranicuAsync(stranicenje, ct);
    }

    public Task<PonudaDto?> DtoAsync(int id, CancellationToken ct = default) =>
        Skup.AsNoTracking().Where(p => p.Id == id).Select(Projekcija).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Ponuda>> IstekleAsync(DateTime sada, int maks, CancellationToken ct = default) =>
        await Skup.Include(p => p.Razgovor)
            .Where(p => p.Status == StatusPonude.NaCekanju && p.IsticeUtc <= sada)
            .OrderBy(p => p.IsticeUtc)
            .Take(maks)
            .ToListAsync(ct);

    /// <summary>Broj ponuda na čekanju na koje JA treba da odgovorim (ponudio ih je neko drugi).</summary>
    public Task<int> BrojZaMojOdgovorAsync(int korisnikId, CancellationToken ct = default) =>
        Skup.CountAsync(p => p.Status == StatusPonude.NaCekanju && p.PonudioKorisnikId != korisnikId
                             && (p.KupacId == korisnikId || p.Razgovor.ProdavacId == korisnikId), ct);
}

internal sealed class ObavestenjeRepository(RetroFitDbContext db, IConfigurationProvider mapiranje)
    : Repository<Obavestenje>(db), IObavestenjeRepository
{
    public Task<StranicaRezultata<ObavestenjeDto>> ZaKorisnikaAsync(int korisnikId, bool samoNeprocitana, Stranicenje stranicenje,
        CancellationToken ct = default)
    {
        var q = Skup.AsNoTracking().Where(o => o.KorisnikId == korisnikId);
        if (samoNeprocitana)
        {
            q = q.Where(o => o.ProcitanoUtc == null);
        }

        return q.OrderByDescending(o => o.KreiranoUtc).ThenByDescending(o => o.Id)
            .ProjectTo<ObavestenjeDto>(mapiranje)
            .UStranicuAsync(stranicenje, ct);
    }

    public Task<int> BrojNeprocitanihAsync(int korisnikId, CancellationToken ct = default) =>
        Skup.CountAsync(o => o.KorisnikId == korisnikId && o.ProcitanoUtc == null, ct);

    public Task<int> OznaciSvaProcitanimAsync(int korisnikId, DateTime sada, CancellationToken ct = default) =>
        Skup.Where(o => o.KorisnikId == korisnikId && o.ProcitanoUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.ProcitanoUtc, sada), ct);
}
