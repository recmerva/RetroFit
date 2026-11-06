using RetroFit.Application.Features.Razgovori;
using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Trgovina;

public sealed record RezervacijaDto
{
    public int Id { get; init; }
    public OglasUkratkoDto Oglas { get; init; } = new();
    public int KupacId { get; init; }
    public string KupacIme { get; init; } = string.Empty;
    public int ProdavacId { get; init; }
    public string ProdavacIme { get; init; } = string.Empty;
    public int? PonudaId { get; init; }
    public decimal? DogovorenaCena { get; init; }
    public StatusRezervacije Status { get; init; }
    public DateTime KreiranoUtc { get; init; }
    public DateTime IsticeUtc { get; init; }
    public DateTime? ZavrsenoUtc { get; init; }
    public string? RazlogOtkazivanja { get; init; }
    public int? TransakcijaId { get; init; }
}

public sealed record PlacanjeDto
{
    public int Id { get; init; }
    public TipPlacanja Tip { get; init; }
    public StatusPlacanja Status { get; init; }
    public decimal Iznos { get; init; }
    public string Valuta { get; init; } = "RSD";
    public string ReferencaProcesora { get; init; } = string.Empty;
    public string Opis { get; init; } = string.Empty;
    public DateTime KreiranoUtc { get; init; }
}

/// <summary>
/// Transakcija - vidljiva samo učesnicima, pa sme da sadrži adresu dostave i telefon druge strane.
/// </summary>
public sealed record TransakcijaDto
{
    public int Id { get; init; }
    public OglasUkratkoDto Oglas { get; init; } = new();
    public int RezervacijaId { get; init; }
    public int KupacId { get; init; }
    public string KupacIme { get; init; } = string.Empty;
    public int ProdavacId { get; init; }
    public string ProdavacIme { get; init; } = string.Empty;
    public decimal? Cena { get; init; }
    public NacinPreuzimanja NacinPreuzimanja { get; init; }
    public string? AdresaDostave { get; init; }
    public StatusTransakcije Status { get; init; }
    public StatusEscrowa StatusEscrowa { get; init; }
    public DateTime KreiranoUtc { get; init; }
    public DateTime? KupacPotvrdioUtc { get; init; }
    public DateTime? ProdavacPotvrdioUtc { get; init; }
    public DateTime? ZavrsenoUtc { get; init; }
    public DateTime? OtkazanoUtc { get; init; }
    public string? RazlogOtkazivanja { get; init; }
    public bool KupacOstavioRecenziju { get; init; }
    public bool ProdavacOstavioRecenziju { get; init; }
    public IReadOnlyList<PlacanjeDto> Placanja { get; init; } = [];
    public string? TelefonDrugeStrane { get; init; }
}

public sealed record RecenzijaDto
{
    public int Id { get; init; }
    public int TransakcijaId { get; init; }
    public int OcenjivacId { get; init; }
    public string OcenjivacIme { get; init; } = string.Empty;
    public int OcenjeniKorisnikId { get; init; }
    public int Ocena { get; init; }
    public string? Komentar { get; init; }
    public DateTime KreiranoUtc { get; init; }
    public string OglasNaslov { get; init; } = string.Empty;
    /// <summary>"Kupac" ako je ocenjivač bio kupac, inače "Prodavac".</summary>
    public string UlogaOcenjivaca { get; init; } = string.Empty;
}

public sealed record SazetakOcenaDto
{
    public decimal ProsecnaOcena { get; init; }
    public int BrojRecenzija { get; init; }
    public IReadOnlyDictionary<int, int> RaspodelaOcena { get; init; } = new Dictionary<int, int>();
}
