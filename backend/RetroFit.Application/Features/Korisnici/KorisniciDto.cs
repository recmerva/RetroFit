using RetroFit.Domain.Pravila;

namespace RetroFit.Application.Features.Korisnici;

/// <summary>
/// Kratak javni prikaz korisnika (na karticama, u porukama...). NE sadrži email ni telefon.
/// </summary>
public sealed record KorisnikKratkoDto
{
    public int Id { get; init; }
    public string PrikaznoIme { get; init; } = string.Empty;
    public decimal ProsecnaOcena { get; init; }
    public int BrojRecenzija { get; init; }
    public bool TelefonVerifikovan { get; init; }
}

/// <summary>Javni profil korisnika. Privatni podaci (email, telefon) se ne prikazuju.</summary>
public sealed record JavniProfilDto
{
    public int Id { get; init; }
    public string PrikaznoIme { get; init; } = string.Empty;
    public string? GradNaziv { get; init; }
    public string? OKorisniku { get; init; }
    public decimal ProsecnaOcena { get; init; }
    public int BrojRecenzija { get; init; }
    public int BrojZavrsenihTransakcija { get; init; }
    public bool TelefonVerifikovan { get; init; }
    public DateTime ClanOdUtc { get; init; }
    public int BrojAktivnihOglasa { get; init; }

    public int Reputacija => ReputacijaPravila.Izracunaj(ProsecnaOcena, BrojRecenzija, BrojZavrsenihTransakcija, TelefonVerifikovan);
    public bool PouzdanProdavac => ReputacijaPravila.JePouzdan(ProsecnaOcena, BrojRecenzija, BrojZavrsenihTransakcija);
    public IReadOnlyList<string> Bedzevi => ReputacijaPravila.Bedzevi(TelefonVerifikovan, PouzdanProdavac, ClanOdUtc, DateTime.UtcNow);
}

/// <summary>Profil prijavljenog korisnika (vidi ga samo on) - sadrži i privatne podatke.</summary>
public sealed record MojProfilDto
{
    public int Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Ime { get; init; } = string.Empty;
    public string Prezime { get; init; } = string.Empty;
    public string PrikaznoIme { get; init; } = string.Empty;
    public string? OKorisniku { get; init; }
    public int? GradId { get; init; }
    public string? GradNaziv { get; init; }
    public string? Telefon { get; init; }
    public bool TelefonVerifikovan { get; init; }
    public bool PrikaziTelefonKupcu { get; init; }
    public IReadOnlyList<string> Uloge { get; init; } = [];
    public decimal ProsecnaOcena { get; init; }
    public int BrojRecenzija { get; init; }
    public int BrojZavrsenihTransakcija { get; init; }
    public DateTime KreiranoUtc { get; init; }

    public int Reputacija => ReputacijaPravila.Izracunaj(ProsecnaOcena, BrojRecenzija, BrojZavrsenihTransakcija, TelefonVerifikovan);
    public bool PouzdanProdavac => ReputacijaPravila.JePouzdan(ProsecnaOcena, BrojRecenzija, BrojZavrsenihTransakcija);
    public IReadOnlyList<string> Bedzevi => ReputacijaPravila.Bedzevi(TelefonVerifikovan, PouzdanProdavac, KreiranoUtc, DateTime.UtcNow);
}

/// <summary>Pregled korisnika za admin/moderator panel.</summary>
public sealed record KorisnikAdminDto
{
    public int Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string ImePrezime { get; init; } = string.Empty;
    public IReadOnlyList<string> Uloge { get; init; } = [];
    public DateTime KreiranoUtc { get; init; }
    public DateTime? PoslednjaPrijavaUtc { get; init; }
    public bool Blokiran { get; init; }
    public bool Obrisan { get; init; }
    public int BrojOglasa { get; init; }
}
