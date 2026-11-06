using RetroFit.Application.Features.Oglasi;
using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Zahtevi;

public sealed record ZahtevDto
{
    public int Id { get; init; }
    public string Naslov { get; init; } = string.Empty;
    public string Opis { get; init; } = string.Empty;
    public int KategorijaId { get; init; }
    public string KategorijaNaziv { get; init; } = string.Empty;
    public int? GradId { get; init; }
    public string? GradNaziv { get; init; }
    public decimal? MaksimalnaCena { get; init; }
    public StatusZahteva Status { get; init; }
    public DateTime KreiranoUtc { get; init; }
    public int KorisnikId { get; init; }
    public string KorisnikIme { get; init; } = string.Empty;
    public int BrojOdgovora { get; init; }
}

public sealed record OdgovorNaZahtevDto
{
    public int Id { get; init; }
    public OglasKarticaDto Oglas { get; init; } = new();
    public string? Poruka { get; init; }
    public DateTime KreiranoUtc { get; init; }
}

public sealed record ZahtevDetaljiDto
{
    public ZahtevDto Zahtev { get; init; } = new();
    public IReadOnlyList<OdgovorNaZahtevDto> Odgovori { get; init; } = [];
    public bool JeMoj { get; init; }
}
