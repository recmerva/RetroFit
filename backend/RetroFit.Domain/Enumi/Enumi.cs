namespace RetroFit.Domain.Enumi;

// Svi enumi se u bazi čuvaju kao tekst (nvarchar) uz CHECK ograničenje IN (...),
// da bi podaci bili čitljivi direktno u SSMS-u, a baza i dalje odbija nevalidne vrednosti.

/// <summary>Životni ciklus oglasa. Dozvoljeni prelazi su definisani u klasi Oglas.</summary>
public enum StatusOglasa
{
    Nacrt,
    Aktivan,
    Rezervisan,
    Prodat,
    Istekao,
    Uklonjen
}

public enum StanjeProizvoda
{
    Novo,
    KaoNovo,
    Polovno,
    ZaDelove
}

public enum TipOglasa
{
    Prodaja,
    Razmena,
    Poklon
}

public enum NacinPreuzimanja
{
    LicnoPreuzimanje,
    Dostava
}

public enum StatusPonude
{
    NaCekanju,
    Prihvacena,
    Odbijena,
    Kontraponuda,
    Istekla
}

public enum StatusRezervacije
{
    Aktivna,
    Realizovana,
    Otkazana,
    Istekla
}

public enum StatusTransakcije
{
    UToku,
    Zavrsena,
    Otkazana
}

/// <summary>Stanje mock escrow naloga: novac kupca se "drži" dok obe strane ne potvrde.</summary>
public enum StatusEscrowa
{
    NijeKoriscen,
    CekaUplatu,
    Deponovano,
    Isplaceno,
    Vraceno
}

public enum TipPlacanja
{
    EscrowUplata,
    EscrowIsplata,
    EscrowPovracaj,
    Isticanje
}

public enum StatusPlacanja
{
    Uspesno,
    Neuspesno
}

public enum TipPoruke
{
    Tekst,
    BrzaPoruka,
    Sistemska
}

public enum TipAtributa
{
    Tekst,
    Broj,
    DaNe,
    Izbor
}

public enum TipVerifikacije
{
    Telefon
}

public enum StatusZahteva
{
    Otvoren,
    Ispunjen,
    Zatvoren
}

public enum RazlogPrijave
{
    Prevara,
    ZabranjenSadrzaj,
    PogresnaKategorija,
    Duplikat,
    Spam,
    UvredljivSadrzaj,
    Ostalo
}

public enum StatusPrijave
{
    Otvorena,
    Prihvacena,
    Odbijena
}

public enum TipIsticanja
{
    /// <summary>Admin ručno izdvaja oglas na početnoj strani.</summary>
    Izdvojen,
    /// <summary>Korisnik plaća "podizanje" (boost) svog oglasa.</summary>
    Podignut
}

public enum TipObavestenja
{
    NovaPoruka,
    NovaPonuda,
    PonudaPrihvacena,
    PonudaOdbijena,
    Kontraponuda,
    PonudaIstekla,
    RezervacijaKreirana,
    RezervacijaOtkazana,
    RezervacijaIstekla,
    TransakcijaAzurirana,
    TransakcijaZavrsena,
    NovaRecenzija,
    PoklapanjePretrage,
    OdgovorNaZahtev,
    OglasIstekao,
    OglasUklonjen,
    NalogBlokiran,
    Sistemsko
}

public enum KategorijaAudita
{
    Podaci,
    Bezbednost,
    Moderacija,
    Administracija
}

public enum SortiranjeOglasa
{
    Najnovije,
    CenaRastuce,
    CenaOpadajuce,
    Popularnost,
    Udaljenost
}
