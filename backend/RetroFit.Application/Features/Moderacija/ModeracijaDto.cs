using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Moderacija;

public sealed record PrijavaDto
{
    public int Id { get; init; }
    public int OglasId { get; init; }
    public string OglasNaslov { get; init; } = string.Empty;
    public StatusOglasa OglasStatus { get; init; }
    public int ProdavacId { get; init; }
    public string ProdavacIme { get; init; } = string.Empty;
    public int PrijavioKorisnikId { get; init; }
    public string PrijavioIme { get; init; } = string.Empty;
    public RazlogPrijave Razlog { get; init; }
    public string? Opis { get; init; }
    public StatusPrijave Status { get; init; }
    public DateTime KreiranoUtc { get; init; }
    public string? ObradioIme { get; init; }
    public DateTime? ObradjenoUtc { get; init; }
    public string? NapomenaModeratora { get; init; }
    /// <summary>Koliko otvorenih prijava ukupno ima ovaj oglas (pomaže moderatoru da odredi prioritet).</summary>
    public int BrojOtvorenihPrijavaZaOglas { get; init; }
}

public sealed record BlokadaKorisnikaDto
{
    public int Id { get; init; }
    public int KorisnikId { get; init; }
    public string KorisnikIme { get; init; } = string.Empty;
    public string KorisnikEmail { get; init; } = string.Empty;
    public string BlokiraoIme { get; init; } = string.Empty;
    public string Razlog { get; init; } = string.Empty;
    public DateTime PocetakUtc { get; init; }
    public DateTime? KrajUtc { get; init; }
    public DateTime? UkinutoUtc { get; init; }
    public bool Aktivna { get; init; }
}

public sealed record OtisakUredjajaDto
{
    public int Id { get; init; }
    /// <summary>Samo prvih 12 znakova heša - dovoljno za prepoznavanje, bez otkrivanja celog otiska.</summary>
    public string OtisakSkraceno { get; init; } = string.Empty;
    public string? OpisKlijenta { get; init; }
    public DateTime PrviPutVidjenUtc { get; init; }
    public DateTime PoslednjiPutVidjenUtc { get; init; }
    public long BrojZahteva { get; init; }
    public int BrojRegistracija { get; init; }
    public int NajveciRizik { get; init; }
    public bool Blokiran { get; init; }
}

public sealed record BlokadaUredjajaDto
{
    public int Id { get; init; }
    public int OtisakUredjajaId { get; init; }
    public string OtisakSkraceno { get; init; } = string.Empty;
    public string Razlog { get; init; } = string.Empty;
    public bool Automatska { get; init; }
    public DateTime PocetakUtc { get; init; }
    public DateTime? KrajUtc { get; init; }
    public DateTime? UkinutoUtc { get; init; }
}
