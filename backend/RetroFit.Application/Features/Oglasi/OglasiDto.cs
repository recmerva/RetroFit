using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Korisnici;
using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Oglasi;

/// <summary>Kartica oglasa u listi rezultata.</summary>
public sealed record OglasKarticaDto
{
    public int Id { get; init; }
    public string Naslov { get; init; } = string.Empty;
    public decimal? Cena { get; init; }
    public TipOglasa Tip { get; init; }
    public StanjeProizvoda Stanje { get; init; }
    public StatusOglasa Status { get; init; }
    public int GradId { get; init; }
    public string GradNaziv { get; init; } = string.Empty;
    public int KategorijaId { get; init; }
    public string KategorijaNaziv { get; init; } = string.Empty;
    public DateTime? ObjavljenoUtc { get; init; }
    public DateTime? IsticeUtc { get; init; }
    public string? GlavnaSlika { get; init; }
    public KorisnikKratkoDto Prodavac { get; init; } = new();
    public bool JeIstaknut { get; init; }
    public int BrojOmiljenih { get; init; }
    public int BrojPregleda { get; init; }
    public double? UdaljenostKm { get; init; }
    /// <summary>Popunjava handler za prijavljenog korisnika (srce na kartici).</summary>
    public bool JeOmiljen { get; init; }
}

public sealed record SlikaDto
{
    public int Id { get; init; }
    public string Putanja { get; init; } = string.Empty;
    public int Redosled { get; init; }
}

public sealed record AtributVrednostDto
{
    public int DefinicijaAtributaId { get; init; }
    public string Naziv { get; init; } = string.Empty;
    public string Kljuc { get; init; } = string.Empty;
    public TipAtributa Tip { get; init; }
    public string Vrednost { get; init; } = string.Empty;
    public string? JedinicaMere { get; init; }
}

/// <summary>Detalji oglasa. TelefonProdavca je popunjen SAMO kada pravila privatnosti to dozvoljavaju.</summary>
public sealed record OglasDetaljiDto
{
    public int Id { get; init; }
    public string Naslov { get; init; } = string.Empty;
    public string Opis { get; init; } = string.Empty;
    public decimal? Cena { get; init; }
    public TipOglasa Tip { get; init; }
    public StanjeProizvoda Stanje { get; init; }
    public StatusOglasa Status { get; init; }
    public string? ZeljenaZamena { get; init; }
    public bool DostavaMoguca { get; init; }
    public bool LicnoPreuzimanjeMoguce { get; init; }
    public int GradId { get; init; }
    public string GradNaziv { get; init; } = string.Empty;
    public int KategorijaId { get; init; }
    public string KategorijaNaziv { get; init; } = string.Empty;
    public int? NadredjenaKategorijaId { get; init; }
    public string? NadredjenaKategorijaNaziv { get; init; }
    public DateTime KreiranoUtc { get; init; }
    public DateTime? ObjavljenoUtc { get; init; }
    public DateTime? IsticeUtc { get; init; }
    public int BrojPregleda { get; init; }
    public int BrojOmiljenih { get; init; }
    public string? RazlogUklanjanja { get; init; }
    public IReadOnlyList<SlikaDto> Slike { get; init; } = [];
    public IReadOnlyList<AtributVrednostDto> Atributi { get; init; } = [];
    public JavniProfilDto Prodavac { get; init; } = new();

    // Polja zavisna od korisnika koji gleda oglas (popunjava ih handler):
    public bool JeMoj { get; init; }
    public bool JeOmiljen { get; init; }
    public bool JeIstaknut { get; init; }
    public string? TelefonProdavca { get; init; }
    public int? MojRazgovorId { get; init; }
}

/// <summary>Kriterijumi pretrage oglasa.</summary>
public sealed record KriterijumiPretrage
{
    public string? Upit { get; init; }
    /// <summary>Uključuje i sve podkategorije izabrane kategorije.</summary>
    public int? KategorijaId { get; init; }
    public int? GradId { get; init; }
    public decimal? CenaOd { get; init; }
    public decimal? CenaDo { get; init; }
    public IReadOnlyList<StanjeProizvoda>? Stanja { get; init; }
    public TipOglasa? Tip { get; init; }
    public bool? SamoSaDostavom { get; init; }
    public double? GeografskaSirina { get; init; }
    public double? GeografskaDuzina { get; init; }
    public double? RadijusKm { get; init; }
    public SortiranjeOglasa Sortiranje { get; init; } = SortiranjeOglasa.Najnovije;
    public Stranicenje Stranicenje { get; init; } = new();
}

public sealed record SacuvanaPretragaDto
{
    public int Id { get; init; }
    public string Naziv { get; init; } = string.Empty;
    public string? Upit { get; init; }
    public int? KategorijaId { get; init; }
    public string? KategorijaNaziv { get; init; }
    public int? GradId { get; init; }
    public string? GradNaziv { get; init; }
    public decimal? CenaOd { get; init; }
    public decimal? CenaDo { get; init; }
    public StanjeProizvoda? Stanje { get; init; }
    public TipOglasa? Tip { get; init; }
    public bool Obavestavaj { get; init; }
    public DateTime KreiranoUtc { get; init; }
}

public sealed record IstaknutiOglasDto
{
    public int Id { get; init; }
    public int OglasId { get; init; }
    public string OglasNaslov { get; init; } = string.Empty;
    public TipIsticanja Tip { get; init; }
    public DateTime PocetakUtc { get; init; }
    public DateTime KrajUtc { get; init; }
    public DateTime? OtkazanoUtc { get; init; }
    public string KreiraoIme { get; init; } = string.Empty;
    public decimal? PlaceniIznos { get; init; }
}
