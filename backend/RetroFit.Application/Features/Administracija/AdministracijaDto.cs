using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Administracija;

public sealed record StatistikaDto
{
    public int UkupnoKorisnika { get; init; }
    public int NoviKorisnici7Dana { get; init; }
    public int AktivniOglasi { get; init; }
    public int RezervisaniOglasi { get; init; }
    public int ProdatiOglasi { get; init; }
    public int NoviOglasi7Dana { get; init; }
    public int ZavrseneTransakcije { get; init; }
    public decimal UkupanPromet { get; init; }
    public decimal PrometEscrow { get; init; }
    public decimal PrihodOdIsticanja { get; init; }
    public int OtvorenePrijave { get; init; }
    public int AktivneBlokadeKorisnika { get; init; }
    public int AktivneBlokadeUredjaja { get; init; }
    public int BezbednosniDogadjaji24h { get; init; }
    public decimal ProsecnaOcenaPlatforme { get; init; }
    public IReadOnlyList<StavkaStatistikeDto> OglasiPoKategorijama { get; init; } = [];
    public IReadOnlyList<StavkaStatistikeDto> TransakcijePoDanima { get; init; } = [];
}

public sealed record StavkaStatistikeDto(string Oznaka, int Vrednost);

public sealed record AuditZapisDto
{
    public int Id { get; init; }
    public KategorijaAudita Kategorija { get; init; }
    public string Akcija { get; init; } = string.Empty;
    public string? NazivEntiteta { get; init; }
    public string? EntitetId { get; init; }
    public int? KorisnikId { get; init; }
    public string? KorisnikEmail { get; init; }
    public string? StareVrednosti { get; init; }
    public string? NoveVrednosti { get; init; }
    public string? IpAdresa { get; init; }
    public string? KorelacioniId { get; init; }
    public DateTime KreiranoUtc { get; init; }
}

public sealed record FilterAudita
{
    public KategorijaAudita? Kategorija { get; init; }
    public int? KorisnikId { get; init; }
    public string? NazivEntiteta { get; init; }
    public DateTime? OdUtc { get; init; }
    public DateTime? DoUtc { get; init; }
}
