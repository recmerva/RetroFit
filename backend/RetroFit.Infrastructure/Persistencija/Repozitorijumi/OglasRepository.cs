using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Korisnici;
using RetroFit.Application.Features.Oglasi;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Infrastructure.Persistencija.Konfiguracije;
using RetroFit.Infrastructure.Pretraga;

namespace RetroFit.Infrastructure.Persistencija.Repozitorijumi;

internal sealed class OglasRepository(RetroFitDbContext db, IFullTextDetektor fullText) : Repository<Oglas>(db), IOglasRepository
{
    public bool FullTextDostupan => fullText.Dostupan;

    public Task<Oglas?> PronadjiZaIzmenuAsync(int id, CancellationToken ct = default) =>
        Skup.Include(o => o.Slike).Include(o => o.Atributi)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id && !o.Obrisan, ct);

    public async Task<OglasDetaljiDto?> DetaljiAsync(int id, DateTime sada, CancellationToken ct = default)
    {
        var detalji = await Skup.AsNoTracking()
            .Where(o => o.Id == id && !o.Obrisan)
            .Select(o => new OglasDetaljiDto
            {
                Id = o.Id,
                Naslov = o.Naslov,
                Opis = o.Opis,
                Cena = o.Cena,
                Tip = o.Tip,
                Stanje = o.Stanje,
                Status = o.Status,
                ZeljenaZamena = o.ZeljenaZamena,
                DostavaMoguca = o.DostavaMoguca,
                LicnoPreuzimanjeMoguce = o.LicnoPreuzimanjeMoguce,
                GradId = o.GradId,
                GradNaziv = o.Grad.Naziv,
                KategorijaId = o.KategorijaId,
                KategorijaNaziv = o.Kategorija.Naziv,
                NadredjenaKategorijaId = o.Kategorija.RoditeljId,
                NadredjenaKategorijaNaziv = o.Kategorija.Roditelj != null ? o.Kategorija.Roditelj.Naziv : null,
                KreiranoUtc = o.KreiranoUtc,
                ObjavljenoUtc = o.ObjavljenoUtc,
                IsticeUtc = o.IsticeUtc,
                BrojPregleda = o.BrojPregleda,
                BrojOmiljenih = o.BrojOmiljenih,
                RazlogUklanjanja = o.RazlogUklanjanja,
                Slike = o.Slike.OrderBy(s => s.Redosled)
                    .Select(s => new SlikaDto { Id = s.Id, Putanja = s.Putanja, Redosled = s.Redosled }).ToList(),
                Atributi = o.Atributi.OrderBy(a => a.DefinicijaAtributa.Redosled)
                    .Select(a => new AtributVrednostDto
                    {
                        DefinicijaAtributaId = a.DefinicijaAtributaId,
                        Naziv = a.DefinicijaAtributa.Naziv,
                        Kljuc = a.DefinicijaAtributa.Kljuc,
                        Tip = a.DefinicijaAtributa.Tip,
                        Vrednost = a.Vrednost,
                        JedinicaMere = a.DefinicijaAtributa.JedinicaMere
                    }).ToList(),
                Prodavac = new JavniProfilDto
                {
                    Id = o.Prodavac.Id,
                    PrikaznoIme = o.Prodavac.Ime + " " + o.Prodavac.Prezime.Substring(0, 1) + ".",
                    GradNaziv = o.Prodavac.Grad != null ? o.Prodavac.Grad.Naziv : null,
                    OKorisniku = o.Prodavac.OKorisniku,
                    ProsecnaOcena = o.Prodavac.ProsecnaOcena,
                    BrojRecenzija = o.Prodavac.BrojRecenzija,
                    BrojZavrsenihTransakcija = o.Prodavac.BrojZavrsenihTransakcija,
                    TelefonVerifikovan = o.Prodavac.TelefonVerifikovanUtc != null,
                    ClanOdUtc = o.Prodavac.KreiranoUtc,
                    BrojAktivnihOglasa = Db.Oglasi.Count(x => x.ProdavacId == o.ProdavacId && !x.Obrisan && x.Status == StatusOglasa.Aktivan)
                },
                JeIstaknut = Db.IstaknutiOglasi.Any(i => i.OglasId == o.Id && i.OtkazanoUtc == null && i.PocetakUtc <= sada && i.KrajUtc > sada)
            })
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);

        return detalji;
    }

    public async Task<StranicaRezultata<OglasKarticaDto>> PretraziAsync(KriterijumiPretrage k, IReadOnlyList<int>? kategorije,
        DateTime sada, CancellationToken ct = default)
    {
        var centar = await OdrediCentarAsync(k, ct);
        var q = Filtriraj(Skup.AsNoTracking().Where(o => !o.Obrisan && o.Status == StatusOglasa.Aktivan), k, kategorije, centar);

        var kartice = q.Select(Projekcije.Kartica(Db, sada, centar));
        kartice = k.Sortiranje switch
        {
            SortiranjeOglasa.CenaRastuce => kartice.OrderBy(x => x.Cena == null).ThenBy(x => x.Cena).ThenByDescending(x => x.Id),
            SortiranjeOglasa.CenaOpadajuce => kartice.OrderByDescending(x => x.Cena).ThenByDescending(x => x.Id),
            // Popularnost: omiljeni vrede više od pregleda (jači signal interesovanja).
            SortiranjeOglasa.Popularnost => kartice.OrderByDescending(x => x.BrojOmiljenih * 5 + x.BrojPregleda).ThenByDescending(x => x.Id),
            SortiranjeOglasa.Udaljenost when centar is not null => kartice.OrderBy(x => x.UdaljenostKm).ThenByDescending(x => x.Id),
            // Najnovije (podrazumevano): istaknuti (plaćeni/izdvojeni) oglasi idu na vrh.
            _ => kartice.OrderByDescending(x => x.JeIstaknut).ThenByDescending(x => x.ObjavljenoUtc).ThenByDescending(x => x.Id)
        };

        return await kartice.UStranicuAsync(k.Stranicenje, ct);
    }

    public Task<StranicaRezultata<OglasKarticaDto>> OglasiProdavcaAsync(int prodavacId, bool samoJavni, StatusOglasa? status,
        Stranicenje stranicenje, DateTime sada, CancellationToken ct = default)
    {
        var q = Skup.AsNoTracking().Where(o => o.ProdavacId == prodavacId && !o.Obrisan);
        if (samoJavni)
        {
            q = q.Where(o => o.Status == StatusOglasa.Aktivan);
        }
        else if (status is not null)
        {
            q = q.Where(o => o.Status == status);
        }

        return q.OrderByDescending(o => o.KreiranoUtc)
            .Select(Projekcije.Kartica(Db, sada, null))
            .UStranicuAsync(stranicenje, ct);
    }

    public async Task<IReadOnlyList<OglasKarticaDto>> KarticeAsync(IReadOnlyCollection<int> idOglasa, DateTime sada, CancellationToken ct = default)
    {
        if (idOglasa.Count == 0)
        {
            return [];
        }

        var lista = await Skup.AsNoTracking()
            .Where(o => idOglasa.Contains(o.Id) && !o.Obrisan)
            .Select(Projekcije.Kartica(Db, sada, null))
            .ToListAsync(ct);

        // Zadržavamo redosled iz ulazne liste.
        var redosled = idOglasa.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        return lista.OrderBy(x => redosled[x.Id]).ToList();
    }

    public async Task<IReadOnlyList<Oglas>> ZaIstekAsync(DateTime sada, int maks, CancellationToken ct = default) =>
        await Skup.Where(o => o.Status == StatusOglasa.Aktivan && !o.Obrisan && o.IsticeUtc <= sada)
            .OrderBy(o => o.IsticeUtc)
            .Take(maks)
            .ToListAsync(ct);

    /// <summary>
    /// Jedna UPDATE naredba sa CASE izrazom:
    ///   UPDATE SlikeOglasa SET Redosled = CASE Id WHEN 5 THEN 0 WHEN 3 THEN 1 ... END WHERE OglasId = @id
    /// SQL Server proverava UNIQUE (OglasId, Redosled) na kraju naredbe, pa zamena mesta ne krši ograničenje.
    /// Sve vrednosti idu kao parametri (zaštita od SQL injection-a).
    /// </summary>
    public async Task PromeniRedosledSlikaAsync(int oglasId, IReadOnlyDictionary<int, int> noviRedosled, CancellationToken ct = default)
    {
        if (noviRedosled.Count == 0)
        {
            return;
        }

        var sql = new StringBuilder("UPDATE dbo.SlikeOglasa SET Redosled = CASE Id ");
        var parametri = new List<SqlParameter> { new("@oglasId", oglasId) };
        var i = 0;
        foreach (var (slikaId, redosled) in noviRedosled)
        {
            sql.Append($"WHEN @s{i} THEN @r{i} ");
            parametri.Add(new SqlParameter($"@s{i}", slikaId));
            parametri.Add(new SqlParameter($"@r{i}", redosled));
            i++;
        }

        sql.Append("END WHERE OglasId = @oglasId;");
        await Db.Database.ExecuteSqlRawAsync(sql.ToString(), parametri, ct);
    }

    public Task UvecajBrojPregledaAsync(int oglasId, CancellationToken ct = default) =>
        Skup.Where(o => o.Id == oglasId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.BrojPregleda, o => o.BrojPregleda + 1), ct);

    public async Task<IReadOnlyList<int>> NoviOglasiZaPretraguAsync(SacuvanaPretraga p, IReadOnlyList<int>? kategorije,
        DateTime odUtc, DateTime doUtc, int maks, CancellationToken ct = default)
    {
        var kriterijumi = new KriterijumiPretrage
        {
            Upit = p.Upit,
            GradId = p.GradId,
            CenaOd = p.CenaOd,
            CenaDo = p.CenaDo,
            Stanja = p.Stanje is null ? null : [p.Stanje.Value],
            Tip = p.Tip
        };

        var q = Filtriraj(
            Skup.AsNoTracking().Where(o => !o.Obrisan && o.Status == StatusOglasa.Aktivan && o.ProdavacId != p.KorisnikId
                                           && o.ObjavljenoUtc > odUtc && o.ObjavljenoUtc <= doUtc),
            kriterijumi, kategorije, null);

        return await q.OrderByDescending(o => o.ObjavljenoUtc).Select(o => o.Id).Take(maks).ToListAsync(ct);
    }

    private IQueryable<Oglas> Filtriraj(IQueryable<Oglas> q, KriterijumiPretrage k, IReadOnlyList<int>? kategorije, Point? centar)
    {
        if (!string.IsNullOrWhiteSpace(k.Upit))
        {
            if (fullText.Dostupan)
            {
                // Full-Text Search: CONTAINS(Naslov, '"bicik*" AND "crven*"') - koristi FT indeks,
                // prepoznaje reči (srpski word breaker) i ne zavisi od kvačica.
                var uslov = FullTextUpit.Napravi(k.Upit);
                if (uslov is not null)
                {
                    q = q.Where(o => EF.Functions.Contains(o.Naslov, uslov) || EF.Functions.Contains(o.Opis, uslov));
                }
            }
            else
            {
                // Fallback bez Full-Text komponente: LIKE '%rec%' za svaku reč (sporije, bez indeksa).
                foreach (var rec in FullTextUpit.Reci(k.Upit))
                {
                    q = q.Where(o => o.Naslov.Contains(rec) || o.Opis.Contains(rec));
                }
            }
        }

        if (kategorije is { Count: > 0 })
        {
            q = q.Where(o => kategorije.Contains(o.KategorijaId));
        }

        if (centar is not null && k.RadijusKm is > 0)
        {
            var metara = k.RadijusKm.Value * 1000;
            q = q.Where(o => EF.Property<Point>(o.Grad, GradKonfiguracija.Lokacija).IsWithinDistance(centar, metara));
        }
        else if (k.GradId is not null)
        {
            q = q.Where(o => o.GradId == k.GradId);
        }

        if (k.CenaOd is not null)
        {
            q = q.Where(o => o.Cena != null && o.Cena >= k.CenaOd);
        }

        if (k.CenaDo is not null)
        {
            q = q.Where(o => o.Cena != null && o.Cena <= k.CenaDo);
        }

        if (k.Stanja is { Count: > 0 })
        {
            var stanja = k.Stanja.ToList();
            q = q.Where(o => stanja.Contains(o.Stanje));
        }

        if (k.Tip is not null)
        {
            q = q.Where(o => o.Tip == k.Tip);
        }

        if (k.SamoSaDostavom == true)
        {
            q = q.Where(o => o.DostavaMoguca);
        }

        return q;
    }

    /// <summary>
    /// Centar pretrage po udaljenosti: GPS koordinate korisnika (ako ih je dao) ili koordinate izabranog grada.
    /// </summary>
    private async Task<Point?> OdrediCentarAsync(KriterijumiPretrage k, CancellationToken ct)
    {
        if (k.GeografskaSirina is not null && k.GeografskaDuzina is not null)
        {
            return new Point(k.GeografskaDuzina.Value, k.GeografskaSirina.Value) { SRID = 4326 };
        }

        if (k.GradId is not null && (k.RadijusKm is > 0 || k.Sortiranje == SortiranjeOglasa.Udaljenost))
        {
            var grad = await Db.Gradovi.AsNoTracking().FirstOrDefaultAsync(g => g.Id == k.GradId, ct);
            if (grad is not null)
            {
                return new Point(grad.GeografskaDuzina, grad.GeografskaSirina) { SRID = 4326 };
            }
        }

        return null;
    }
}
