using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Obavestenja;

public sealed record ObavestenjeDto
{
    public int Id { get; init; }
    public TipObavestenja Tip { get; init; }
    public string Naslov { get; init; } = string.Empty;
    public string Sadrzaj { get; init; } = string.Empty;
    public string? Link { get; init; }
    public DateTime KreiranoUtc { get; init; }
    public DateTime? ProcitanoUtc { get; init; }
}

public sealed record BrojaciDto(int NeprocitanaObavestenja, int NeprocitanePoruke, int PonudeNaCekanju);
