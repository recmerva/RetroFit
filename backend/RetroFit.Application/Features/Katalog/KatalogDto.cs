using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Katalog;

public sealed record GradDto
{
    public int Id { get; init; }
    public string Naziv { get; init; } = string.Empty;
    public string PostanskiBroj { get; init; } = string.Empty;
    public double GeografskaSirina { get; init; }
    public double GeografskaDuzina { get; init; }
}

public sealed record KategorijaDto
{
    public int Id { get; init; }
    public string Naziv { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Ikonica { get; init; }
    public int Redosled { get; init; }
    public bool Aktivna { get; init; }
    public int? RoditeljId { get; init; }
    public int BrojAktivnihOglasa { get; init; }
    public List<KategorijaDto> Podkategorije { get; init; } = [];
}

public sealed record DefinicijaAtributaDto
{
    public int Id { get; init; }
    public int KategorijaId { get; init; }
    public string Naziv { get; init; } = string.Empty;
    public string Kljuc { get; init; } = string.Empty;
    public TipAtributa Tip { get; init; }
    public bool Obavezan { get; init; }
    public IReadOnlyList<string> Opcije { get; init; } = [];
    public string? JedinicaMere { get; init; }
    public int Redosled { get; init; }
}
