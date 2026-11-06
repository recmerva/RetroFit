using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RetroFit.Application.Features.Korisnici;
using RetroFit.Application.Features.Oglasi;
using RetroFit.Application.Features.Razgovori;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;
using RetroFit.Infrastructure.Persistencija.Konfiguracije;

namespace RetroFit.Infrastructure.Persistencija.Repozitorijumi;

/// <summary>
/// Zajedničke LINQ projekcije entitet -> DTO. EF Core ih prevodi u SQL SELECT sa tačno potrebnim
/// kolonama (i podupitima), pa se celi entiteti nikad ne učitavaju u memoriju za prikaz liste.
/// </summary>
internal static class Projekcije
{
    public static Expression<Func<Oglas, OglasKarticaDto>> Kartica(RetroFitDbContext db, DateTime sada, Point? centar) =>
        o => new OglasKarticaDto
        {
            Id = o.Id,
            Naslov = o.Naslov,
            Cena = o.Cena,
            Tip = o.Tip,
            Stanje = o.Stanje,
            Status = o.Status,
            GradId = o.GradId,
            GradNaziv = o.Grad.Naziv,
            KategorijaId = o.KategorijaId,
            KategorijaNaziv = o.Kategorija.Naziv,
            ObjavljenoUtc = o.ObjavljenoUtc,
            IsticeUtc = o.IsticeUtc,
            GlavnaSlika = o.Slike.OrderBy(s => s.Redosled).Select(s => s.Putanja).FirstOrDefault(),
            Prodavac = new KorisnikKratkoDto
            {
                Id = o.Prodavac.Id,
                PrikaznoIme = o.Prodavac.Ime + " " + o.Prodavac.Prezime.Substring(0, 1) + ".",
                ProsecnaOcena = o.Prodavac.ProsecnaOcena,
                BrojRecenzija = o.Prodavac.BrojRecenzija,
                TelefonVerifikovan = o.Prodavac.TelefonVerifikovanUtc != null
            },
            JeIstaknut = db.IstaknutiOglasi.Any(i =>
                i.OglasId == o.Id && i.OtkazanoUtc == null && i.PocetakUtc <= sada && i.KrajUtc > sada),
            BrojOmiljenih = o.BrojOmiljenih,
            BrojPregleda = o.BrojPregleda,
            // geography.STDistance vraća metre; ako centar nije zadat (NULL), rezultat je NULL.
            UdaljenostKm = EF.Property<Point>(o.Grad, GradKonfiguracija.Lokacija).Distance(centar!) / 1000.0
        };

    public static Expression<Func<Korisnik, JavniProfilDto>> JavniProfil(RetroFitDbContext db) =>
        k => new JavniProfilDto
        {
            Id = k.Id,
            PrikaznoIme = k.Ime + " " + k.Prezime.Substring(0, 1) + ".",
            GradNaziv = k.Grad != null ? k.Grad.Naziv : null,
            OKorisniku = k.OKorisniku,
            ProsecnaOcena = k.ProsecnaOcena,
            BrojRecenzija = k.BrojRecenzija,
            BrojZavrsenihTransakcija = k.BrojZavrsenihTransakcija,
            TelefonVerifikovan = k.TelefonVerifikovanUtc != null,
            ClanOdUtc = k.KreiranoUtc,
            BrojAktivnihOglasa = db.Oglasi.Count(o => o.ProdavacId == k.Id && !o.Obrisan && o.Status == StatusOglasa.Aktivan)
        };

    public static Expression<Func<Oglas, OglasUkratkoDto>> OglasUkratko =>
        o => new OglasUkratkoDto
        {
            Id = o.Id,
            Naslov = o.Naslov,
            Cena = o.Cena,
            Tip = o.Tip,
            Status = o.Status,
            GlavnaSlika = o.Slike.OrderBy(s => s.Redosled).Select(s => s.Putanja).FirstOrDefault(),
            ProdavacId = o.ProdavacId
        };

    public static string ImeKratko(Korisnik k) => k.Obrisan ? "Obrisan korisnik" : $"{k.Ime} {k.Prezime[..1]}.";
}
