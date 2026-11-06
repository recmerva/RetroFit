using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Razgovori;

public sealed record OglasUkratkoDto
{
    public int Id { get; init; }
    public string Naslov { get; init; } = string.Empty;
    public decimal? Cena { get; init; }
    public TipOglasa Tip { get; init; }
    public StatusOglasa Status { get; init; }
    public string? GlavnaSlika { get; init; }
    public int ProdavacId { get; init; }
}

public sealed record RazgovorPregledDto
{
    public int Id { get; init; }
    public OglasUkratkoDto Oglas { get; init; } = new();
    public int SagovornikId { get; init; }
    public string SagovornikIme { get; init; } = string.Empty;
    public bool JaSamKupac { get; init; }
    public string? PoslednjaPoruka { get; init; }
    public DateTime PoslednjaPorukaUtc { get; init; }
    public int BrojNeprocitanih { get; init; }
}

public sealed record PorukaDto
{
    public int Id { get; init; }
    public int RazgovorId { get; init; }
    public int PosiljalacId { get; init; }
    public string Sadrzaj { get; init; } = string.Empty;
    public TipPoruke Tip { get; init; }
    public DateTime PoslatoUtc { get; init; }
    public DateTime? ProcitanoUtc { get; init; }
    public int? PonudaId { get; init; }
}

public sealed record PonudaDto
{
    public int Id { get; init; }
    public int OglasId { get; init; }
    public string OglasNaslov { get; init; } = string.Empty;
    public decimal? CenaOglasa { get; init; }
    public string? OglasSlika { get; init; }
    public int RazgovorId { get; init; }
    public int KupacId { get; init; }
    public string KupacIme { get; init; } = string.Empty;
    public int ProdavacId { get; init; }
    public string ProdavacIme { get; init; } = string.Empty;
    public int PonudioKorisnikId { get; init; }
    public decimal Iznos { get; init; }
    public string? Napomena { get; init; }
    public StatusPonude Status { get; init; }
    public DateTime KreiranoUtc { get; init; }
    public DateTime IsticeUtc { get; init; }
    public DateTime? OdgovorenoUtc { get; init; }
    public int? PrethodnaPonudaId { get; init; }
}

/// <summary>Detalji razgovora. TelefonProdavca se popunjava samo kupcu sa aktivnom/realizovanom rezervacijom.</summary>
public sealed record RazgovorDetaljiDto
{
    public int Id { get; init; }
    public OglasUkratkoDto Oglas { get; init; } = new();
    public int KupacId { get; init; }
    public string KupacIme { get; init; } = string.Empty;
    public int ProdavacId { get; init; }
    public string ProdavacIme { get; init; } = string.Empty;
    public bool JaSamKupac { get; init; }
    public DateTime KreiranoUtc { get; init; }
    public IReadOnlyList<PonudaDto> Ponude { get; init; } = [];
    public int? AktivnaRezervacijaId { get; init; }
    public string? TelefonProdavca { get; init; }
}

public sealed record BrzaPorukaDto(string Kljuc, string Tekst);
