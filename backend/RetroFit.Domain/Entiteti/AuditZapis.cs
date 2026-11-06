using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Zapis u audit logu: ko je, kada i šta promenio. Popunjava se:
///  - automatski za izmene podataka (EF interceptor beleži stare i nove vrednosti kao JSON);
///  - eksplicitno za bezbednosne i moderatorske događaje (prijava, blokada, uklanjanje oglasa, sumnjiv bot...).
/// Lozinke, tokeni i telefoni se NIKAD ne upisuju u audit (maskiraju se).
/// </summary>
public class AuditZapis : Entitet
{
    private AuditZapis() { }

    public AuditZapis(KategorijaAudita kategorija, string akcija, string? nazivEntiteta, string? entitetId,
        int? korisnikId, string? stareVrednosti, string? noveVrednosti, string? ipAdresa, string? korelacioniId, DateTime sada)
    {
        Kategorija = kategorija;
        Akcija = akcija.Length > 100 ? akcija[..100] : akcija;
        NazivEntiteta = nazivEntiteta;
        EntitetId = entitetId;
        KorisnikId = korisnikId;
        StareVrednosti = stareVrednosti;
        NoveVrednosti = noveVrednosti;
        IpAdresa = ipAdresa;
        KorelacioniId = korelacioniId;
        KreiranoUtc = sada;
    }

    public KategorijaAudita Kategorija { get; private set; }
    public string Akcija { get; private set; } = string.Empty;
    public string? NazivEntiteta { get; private set; }
    public string? EntitetId { get; private set; }
    public int? KorisnikId { get; private set; }
    public Korisnik? Korisnik { get; private set; }
    public string? StareVrednosti { get; private set; }
    public string? NoveVrednosti { get; private set; }
    public string? IpAdresa { get; private set; }
    public string? KorelacioniId { get; private set; }
    public DateTime KreiranoUtc { get; private set; }
}
