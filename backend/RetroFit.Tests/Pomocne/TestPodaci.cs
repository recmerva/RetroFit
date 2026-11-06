using System.Reflection;
using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Tests.Pomocne;

/// <summary>Fabrika test podataka. Entiteti se prave kroz domenske metode, kao u aplikaciji.</summary>
internal static class TestPodaci
{
    public static readonly DateTime Sada = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    public const int ProdavacId = 10;
    public const int KupacId = 20;
    public const int DrugiKupacId = 30;

    /// <summary>Postavlja Id (setter je zaštićen jer ga u aplikaciji dodeljuje baza).</summary>
    public static T SaId<T>(this T entitet, int id) where T : Entitet
    {
        typeof(Entitet).GetProperty(nameof(Entitet.Id))!.SetValue(entitet, id);
        return entitet;
    }

    public static Oglas AktivanOglas(int id = 1, TipOglasa tip = TipOglasa.Prodaja, decimal? cena = 10_000, int prodavacId = ProdavacId) =>
        Oglas.Kreiraj(prodavacId, 11, 1, "Test oglas za prodaju", "Opis test oglasa koji je dovoljno dugačak.", tip,
            tip == TipOglasa.Prodaja ? cena : null, StanjeProizvoda.Polovno, true, true, null, objaviOdmah: true, Sada).SaId(id);

    public static Razgovor NoviRazgovor(Oglas oglas, int kupacId = KupacId, int id = 100) => new Razgovor(oglas, kupacId, Sada).SaId(id);

    public static Ponuda PonudaNaCekanju(Oglas oglas, Razgovor razgovor, decimal iznos = 9_000, int id = 200) =>
        Ponuda.Kreiraj(razgovor, oglas, razgovor.KupacId, iznos, null, Sada).SaId(id);

    public static (Oglas Oglas, Rezervacija Rezervacija) Rezervisano(decimal? cena = 10_000)
    {
        var oglas = AktivanOglas(cena: cena);
        var rezervacija = Rezervacija.Kreiraj(oglas, KupacId, cena, null, Sada).SaId(300);
        return (oglas, rezervacija);
    }

    public static Transakcija TransakcijaUToku(bool escrow = true, decimal cena = 10_000)
    {
        var (oglas, rezervacija) = Rezervisano(cena);
        return Transakcija.IzRezervacije(rezervacija, oglas, KupacId, NacinPreuzimanja.LicnoPreuzimanje, null, escrow, Sada.AddHours(1)).SaId(400);
    }

    public static Transakcija ZavrsenaTransakcija()
    {
        var t = TransakcijaUToku(escrow: false);
        t.Potvrdi(ProdavacId, Sada.AddHours(2));
        t.Potvrdi(KupacId, Sada.AddHours(3));
        return t;
    }

    public static Korisnik NoviKorisnik(int id, string email = "test@primer.rs")
    {
        var k = new Korisnik(email, "Petar", "Petrović", 1).SaId(id);
        k.PostaviLozinkuHash("HES");
        return k;
    }

    public static void PostaviSvojstvo(object objekat, string naziv, object? vrednost) =>
        objekat.GetType().GetProperty(naziv, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(objekat, vrednost);
}
