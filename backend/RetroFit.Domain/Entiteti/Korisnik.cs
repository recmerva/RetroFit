using RetroFit.Domain.Common;
using RetroFit.Domain.Pravila;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Korisnik platforme. Isti korisnik može biti i kupac i prodavac.
/// Osetljivi podaci (LozinkaHash, Telefon) se NIKAD ne mapiraju u javne DTO-e.
/// </summary>
public class Korisnik : AuditabilniEntitet, IMekoBrisanje
{
    private readonly List<KorisnikUloga> _uloge = [];

    private Korisnik() { }

    public Korisnik(string email, string ime, string prezime, int? gradId)
    {
        PostaviEmail(email);
        Ime = Provera.Tekst(ime, "Ime", 50);
        Prezime = Provera.Tekst(prezime, "Prezime", 50);
        GradId = gradId;
        PrikaziTelefonKupcu = true;
    }

    public string Email { get; private set; } = string.Empty;
    public string NormalizovanEmail { get; private set; } = string.Empty;
    public string LozinkaHash { get; private set; } = string.Empty;
    public string Ime { get; private set; } = string.Empty;
    public string Prezime { get; private set; } = string.Empty;
    public string? OKorisniku { get; private set; }
    public int? GradId { get; private set; }
    public Grad? Grad { get; private set; }

    /// <summary>Privatan podatak. Prikazuje se samo kupcu sa prihvaćenom ponudom ili aktivnom rezervacijom.</summary>
    public string? Telefon { get; private set; }
    public DateTime? TelefonVerifikovanUtc { get; private set; }

    /// <summary>Podešavanje privatnosti: da li kupac sa prihvaćenom ponudom vidi broj telefona.</summary>
    public bool PrikaziTelefonKupcu { get; private set; }

    // Denormalizovana polja za brz prikaz ocene na karticama oglasa
    // (ažuriraju se u istoj transakciji kao i nova recenzija).
    public decimal ProsecnaOcena { get; private set; }
    public int BrojRecenzija { get; private set; }
    public int BrojZavrsenihTransakcija { get; private set; }

    public DateTime? PoslednjaPrijavaUtc { get; private set; }
    public bool Obrisan { get; private set; }
    public DateTime? ObrisanoUtc { get; private set; }

    public IReadOnlyCollection<KorisnikUloga> Uloge => _uloge;

    public string PrikaznoIme => Obrisan ? "Obrisan korisnik" : $"{Ime} {Prezime[..1]}.";
    public bool TelefonVerifikovan => TelefonVerifikovanUtc.HasValue;

    public static string Normalizuj(string email) => email.Trim().ToUpperInvariant();

    public void PostaviLozinkuHash(string hash)
    {
        Provera.Uslov(!string.IsNullOrWhiteSpace(hash), "validacija", "Heš lozinke ne sme biti prazan.");
        LozinkaHash = hash;
    }

    public void DodeliUlogu(Uloga uloga, DateTime sada)
    {
        if (_uloge.Any(u => ReferenceEquals(u.Uloga, uloga) || (uloga.Id != 0 && u.UlogaId == uloga.Id)))
        {
            return;
        }

        _uloge.Add(new KorisnikUloga(this, uloga, sada));
    }

    public void UkloniUlogu(int ulogaId)
    {
        var postojeca = _uloge.FirstOrDefault(u => u.UlogaId == ulogaId);
        if (postojeca is not null)
        {
            _uloge.Remove(postojeca);
        }
    }

    public bool ImaUlogu(string naziv) => _uloge.Any(u => u.Uloga?.Naziv == naziv);

    public void AzurirajProfil(string ime, string prezime, string? oKorisniku, int? gradId, bool prikaziTelefonKupcu)
    {
        ProveriDaNijeObrisan();
        Ime = Provera.Tekst(ime, "Ime", 50);
        Prezime = Provera.Tekst(prezime, "Prezime", 50);
        OKorisniku = Provera.OpcioniTekst(oKorisniku, "O korisniku", 500);
        GradId = gradId;
        PrikaziTelefonKupcu = prikaziTelefonKupcu;
    }

    /// <summary>Promena broja telefona poništava prethodnu verifikaciju.</summary>
    public void PostaviTelefon(string? telefon)
    {
        ProveriDaNijeObrisan();
        var novi = string.IsNullOrWhiteSpace(telefon) ? null : telefon.Trim();
        if (novi != Telefon)
        {
            Telefon = novi;
            TelefonVerifikovanUtc = null;
        }
    }

    public void PotvrdiTelefon(string telefon, DateTime sada)
    {
        Provera.Uslov(Telefon == telefon, "verifikacija_neispravna", "Broj telefona je u međuvremenu promenjen.");
        TelefonVerifikovanUtc = sada;
    }

    public void EvidentirajPrijavu(DateTime sada) => PoslednjaPrijavaUtc = sada;

    /// <summary>Inkrementalno ažuriranje proseka: novi = (stari * n + ocena) / (n + 1).</summary>
    public void DodajOcenu(int ocena)
    {
        Provera.Uslov(ocena is >= 1 and <= 5, "validacija", "Ocena mora biti između 1 i 5.");
        var ukupno = ProsecnaOcena * BrojRecenzija + ocena;
        BrojRecenzija++;
        ProsecnaOcena = Math.Round(ukupno / BrojRecenzija, 2);
    }

    public void EvidentirajZavrsenuTransakciju() => BrojZavrsenihTransakcija++;

    /// <summary>Reputacija 0-100 (formula u ReputacijaPravila).</summary>
    public int Reputacija =>
        ReputacijaPravila.Izracunaj(ProsecnaOcena, BrojRecenzija, BrojZavrsenihTransakcija, TelefonVerifikovan);

    public bool PouzdanProdavac =>
        ReputacijaPravila.JePouzdan(ProsecnaOcena, BrojRecenzija, BrojZavrsenihTransakcija);

    /// <summary>
    /// Brisanje naloga = anonimizacija ličnih podataka (privatnost).
    /// Red ostaje u bazi jer se na njega oslanjaju transakcije i recenzije.
    /// </summary>
    public void Obrisi(DateTime sada)
    {
        if (Obrisan)
        {
            return;
        }

        Obrisan = true;
        ObrisanoUtc = sada;
        Email = $"obrisan-{Id}-{sada.Ticks}@retrofit.local";
        NormalizovanEmail = Normalizuj(Email);
        Ime = "Obrisan";
        Prezime = "Korisnik";
        OKorisniku = null;
        Telefon = null;
        TelefonVerifikovanUtc = null;
        LozinkaHash = "OBRISAN";
    }

    private void PostaviEmail(string email)
    {
        var vrednost = Provera.Tekst(email, "Email", 256);
        Provera.Uslov(vrednost.Contains('@'), "validacija", "Email adresa nije ispravna.");
        Email = vrednost;
        NormalizovanEmail = Normalizuj(vrednost);
    }

    private void ProveriDaNijeObrisan() =>
        Provera.Uslov(!Obrisan, "korisnik_obrisan", "Nalog je obrisan.");
}
