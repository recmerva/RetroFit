using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Administracija;
using RetroFit.Application.Features.Katalog;
using RetroFit.Application.Features.Korisnici;
using RetroFit.Application.Features.Moderacija;
using RetroFit.Application.Features.Obavestenja;
using RetroFit.Application.Features.Oglasi;
using RetroFit.Application.Features.Razgovori;
using RetroFit.Application.Features.Trgovina;
using RetroFit.Application.Features.Zahtevi;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Common.Interfejsi.Persistencija;

// Specifični repozitorijumi. Metode koje vraćaju entitete služe komandama (izmena stanja),
// a metode koje vraćaju DTO služe upitima (samo čitanje, AsNoTracking + projekcija u SQL-u).

public interface IKorisnikRepository : IRepository<Korisnik>
{
    Task<Korisnik?> PronadjiPoEmailuAsync(string normalizovanEmail, CancellationToken ct = default);
    Task<Korisnik?> PronadjiSaUlogamaAsync(int id, CancellationToken ct = default);
    Task<bool> PostojiEmailAsync(string normalizovanEmail, CancellationToken ct = default);
    Task<bool> TelefonZauzetAsync(string telefon, int osimKorisnikaId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> UlogeKorisnikaAsync(int korisnikId, CancellationToken ct = default);
    Task<MojProfilDto?> MojProfilAsync(int id, CancellationToken ct = default);
    Task<JavniProfilDto?> JavniProfilAsync(int id, CancellationToken ct = default);
    Task<StranicaRezultata<KorisnikAdminDto>> PretraziZaAdminaAsync(string? upit, Stranicenje stranicenje, DateTime sada, CancellationToken ct = default);

    /// <summary>
    /// Atomično ažuriranje proseka ocena JEDNOM UPDATE naredbom (bez čitanja u memoriju), pa dve
    /// istovremene recenzije za istog korisnika ne mogu da "pregaze" jedna drugu (lost update).
    /// </summary>
    Task DodajOcenuAsync(int korisnikId, int ocena, CancellationToken ct = default);

    /// <summary>Atomično uvećanje brojača završenih transakcija za kupca i prodavca.</summary>
    Task UvecajZavrseneTransakcijeAsync(IReadOnlyCollection<int> korisnici, CancellationToken ct = default);
}

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> PronadjiPoHashuAsync(string hash, CancellationToken ct = default);
    Task<int> OpozoviPorodicuAsync(Guid porodicaId, string razlog, DateTime sada, CancellationToken ct = default);
    Task<int> OpozoviSveZaKorisnikaAsync(int korisnikId, string razlog, DateTime sada, CancellationToken ct = default);
    Task<int> ObrisiZastareleAsync(DateTime starijeOd, CancellationToken ct = default);
}

public interface IBlokadaRepository : IRepository<BlokadaKorisnika>
{
    Task<BlokadaKorisnika?> AktivnaZaKorisnikaAsync(int korisnikId, DateTime sada, CancellationToken ct = default);
    Task<StranicaRezultata<BlokadaKorisnikaDto>> ListaAsync(bool samoAktivne, Stranicenje stranicenje, DateTime sada, CancellationToken ct = default);
}

public interface IGradRepository : IRepository<Grad>
{
    Task<IReadOnlyList<GradDto>> SviAsync(CancellationToken ct = default);
}

public interface IKategorijaRepository : IRepository<Kategorija>
{
    Task<IReadOnlyList<KategorijaDto>> SveRavnoAsync(bool samoAktivne, CancellationToken ct = default);
    Task<Kategorija?> PronadjiSaAtributimaAsync(int id, CancellationToken ct = default);
    /// <summary>Definicije atributa kategorije i svih njenih nadređenih kategorija.</summary>
    Task<IReadOnlyList<DefinicijaAtributa>> DefinicijeZaKategorijuAsync(int kategorijaId, CancellationToken ct = default);
    /// <summary>Id kategorije i svih njenih podkategorija (rekurzivni CTE u SQL-u).</summary>
    Task<IReadOnlyList<int>> IdSaPodkategorijamaAsync(int kategorijaId, CancellationToken ct = default);
    Task<bool> ImaPodkategorijeAsync(int id, CancellationToken ct = default);
}

public interface IOglasRepository : IRepository<Oglas>
{
    Task<Oglas?> PronadjiZaIzmenuAsync(int id, CancellationToken ct = default);
    Task<OglasDetaljiDto?> DetaljiAsync(int id, DateTime sada, CancellationToken ct = default);
    Task<StranicaRezultata<OglasKarticaDto>> PretraziAsync(KriterijumiPretrage kriterijumi, IReadOnlyList<int>? kategorije,
        DateTime sada, CancellationToken ct = default);
    Task<StranicaRezultata<OglasKarticaDto>> OglasiProdavcaAsync(int prodavacId, bool samoJavni, StatusOglasa? status,
        Stranicenje stranicenje, DateTime sada, CancellationToken ct = default);
    Task<IReadOnlyList<OglasKarticaDto>> KarticeAsync(IReadOnlyCollection<int> idOglasa, DateTime sada, CancellationToken ct = default);
    Task<IReadOnlyList<Oglas>> ZaIstekAsync(DateTime sada, int maks, CancellationToken ct = default);
    /// <summary>Promena redosleda slika JEDNOM UPDATE naredbom (vidi Oglas.PripremiNoviRedosled).</summary>
    Task PromeniRedosledSlikaAsync(int oglasId, IReadOnlyDictionary<int, int> noviRedosled, CancellationToken ct = default);
    /// <summary>Atomično uvećanje brojača (UPDATE ... SET BrojPregleda = BrojPregleda + 1), bez učitavanja reda.</summary>
    Task UvecajBrojPregledaAsync(int oglasId, CancellationToken ct = default);
    Task<IReadOnlyList<int>> NoviOglasiZaPretraguAsync(SacuvanaPretraga pretraga, IReadOnlyList<int>? kategorije,
        DateTime odUtc, DateTime doUtc, int maks, CancellationToken ct = default);
    bool FullTextDostupan { get; }
}

public interface IOmiljeniRepository
{
    Task<OmiljeniOglas?> PronadjiAsync(int korisnikId, int oglasId, CancellationToken ct = default);
    Task DodajAsync(OmiljeniOglas omiljeni, CancellationToken ct = default);
    void Ukloni(OmiljeniOglas omiljeni);
    Task<StranicaRezultata<OglasKarticaDto>> OmiljeniKorisnikaAsync(int korisnikId, Stranicenje stranicenje, DateTime sada, CancellationToken ct = default);
    Task<IReadOnlyList<int>> IdOmiljenihAsync(int korisnikId, IReadOnlyCollection<int> idOglasa, CancellationToken ct = default);
}

public interface ISacuvanaPretragaRepository : IRepository<SacuvanaPretraga>
{
    Task<IReadOnlyList<SacuvanaPretragaDto>> ZaKorisnikaAsync(int korisnikId, CancellationToken ct = default);
    Task<IReadOnlyList<SacuvanaPretraga>> ZaObavestavanjeAsync(DateTime proveraPre, int maks, CancellationToken ct = default);
}

public interface IIstaknutiOglasRepository : IRepository<IstaknutiOglas>
{
    Task<IReadOnlyList<int>> IzdvojeniIdAsync(int maks, DateTime sada, CancellationToken ct = default);
    Task<bool> ImaAktivnoAsync(int oglasId, TipIsticanja tip, DateTime sada, CancellationToken ct = default);
    Task<StranicaRezultata<IstaknutiOglasDto>> ListaAsync(bool samoAktivna, Stranicenje stranicenje, DateTime sada, CancellationToken ct = default);
}

public interface IRazgovorRepository : IRepository<Razgovor>
{
    Task<Razgovor?> PronadjiZaOglasIKupcaAsync(int oglasId, int kupacId, CancellationToken ct = default);
    Task<Razgovor?> PronadjiSaOglasomAsync(int id, CancellationToken ct = default);
    Task<StranicaRezultata<RazgovorPregledDto>> ZaKorisnikaAsync(int korisnikId, Stranicenje stranicenje, CancellationToken ct = default);
    Task<RazgovorDetaljiDto?> DetaljiAsync(int id, int korisnikId, CancellationToken ct = default);
    Task<StranicaRezultata<PorukaDto>> PorukeAsync(int razgovorId, Stranicenje stranicenje, CancellationToken ct = default);
    Task<int> OznaciProcitaneAsync(int razgovorId, int korisnikId, DateTime sada, CancellationToken ct = default);
    Task<int> BrojNeprocitanihAsync(int korisnikId, CancellationToken ct = default);
}

public interface IPonudaRepository : IRepository<Ponuda>
{
    Task<Ponuda?> PronadjiSaRazgovoromAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Ponuda>> NaCekanjuZaOglasAsync(int oglasId, CancellationToken ct = default);
    Task<Ponuda?> NaCekanjuURazgovoruAsync(int razgovorId, CancellationToken ct = default);
    Task<StranicaRezultata<PonudaDto>> ZaKorisnikaAsync(int korisnikId, bool? kaoKupac, StatusPonude? status, Stranicenje stranicenje, CancellationToken ct = default);
    Task<PonudaDto?> DtoAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Ponuda>> IstekleAsync(DateTime sada, int maks, CancellationToken ct = default);
    Task<int> BrojZaMojOdgovorAsync(int korisnikId, CancellationToken ct = default);
}

public interface IRezervacijaRepository : IRepository<Rezervacija>
{
    Task<Rezervacija?> PronadjiSaOglasomAsync(int id, CancellationToken ct = default);
    Task<Rezervacija?> AktivnaZaOglasAsync(int oglasId, CancellationToken ct = default);
    Task<IReadOnlyList<Rezervacija>> IstekleAsync(DateTime sada, int maks, CancellationToken ct = default);
    Task<StranicaRezultata<RezervacijaDto>> ZaKorisnikaAsync(int korisnikId, StatusRezervacije? status, Stranicenje stranicenje, CancellationToken ct = default);
    Task<RezervacijaDto?> DtoAsync(int id, CancellationToken ct = default);
    /// <summary>
    /// Pravilo privatnosti: kupac sme da vidi telefon prodavca samo ako za taj oglas ima
    /// aktivnu ili realizovanu rezervaciju (nastaje iz prihvaćene ponude).
    /// </summary>
    Task<bool> KupacImaPravoNaKontaktAsync(int oglasId, int kupacId, CancellationToken ct = default);
}

public interface ITransakcijaRepository : IRepository<Transakcija>
{
    Task<Transakcija?> PronadjiSaOglasomAsync(int id, CancellationToken ct = default);
    Task<TransakcijaDto?> DetaljiAsync(int id, CancellationToken ct = default);
    Task<StranicaRezultata<TransakcijaDto>> ZaKorisnikaAsync(int korisnikId, StatusTransakcije? status, Stranicenje stranicenje, CancellationToken ct = default);
}

public interface IRecenzijaRepository : IRepository<Recenzija>
{
    Task<StranicaRezultata<RecenzijaDto>> ZaKorisnikaAsync(int korisnikId, Stranicenje stranicenje, CancellationToken ct = default);
    Task<SazetakOcenaDto> SazetakAsync(int korisnikId, CancellationToken ct = default);
}

public interface IZahtevTrazimRepository : IRepository<ZahtevTrazim>
{
    Task<ZahtevTrazim?> PronadjiSaOdgovorimaAsync(int id, CancellationToken ct = default);
    Task<StranicaRezultata<ZahtevDto>> PretraziAsync(string? upit, IReadOnlyList<int>? kategorije, int? gradId, int? korisnikId,
        StatusZahteva? status, Stranicenje stranicenje, CancellationToken ct = default);
    Task<ZahtevDto?> DtoAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<OdgovorNaZahtev>> OdgovoriAsync(int zahtevId, CancellationToken ct = default);
}

public interface IPrijavaRepository : IRepository<Prijava>
{
    Task<StranicaRezultata<PrijavaDto>> ListaAsync(StatusPrijave? status, Stranicenje stranicenje, CancellationToken ct = default);
    Task<IReadOnlyList<Prijava>> OtvoreneZaOglasAsync(int oglasId, CancellationToken ct = default);
}

public interface IObavestenjeRepository : IRepository<Obavestenje>
{
    Task<StranicaRezultata<ObavestenjeDto>> ZaKorisnikaAsync(int korisnikId, bool samoNeprocitana, Stranicenje stranicenje, CancellationToken ct = default);
    Task<int> BrojNeprocitanihAsync(int korisnikId, CancellationToken ct = default);
    Task<int> OznaciSvaProcitanimAsync(int korisnikId, DateTime sada, CancellationToken ct = default);
}

public interface IOtisakUredjajaRepository : IRepository<OtisakUredjaja>
{
    Task<OtisakUredjaja?> PoHashuAsync(string hash, CancellationToken ct = default);
    Task<IReadOnlyList<string>> AktivnoBlokiraniHesheviAsync(DateTime sada, CancellationToken ct = default);
    Task DodajBlokaduAsync(BlokadaUredjaja blokada, CancellationToken ct = default);
    Task<BlokadaUredjaja?> BlokadaPoIdAsync(int id, CancellationToken ct = default);
    Task<bool> ImaAktivnuBlokaduAsync(int otisakId, DateTime sada, CancellationToken ct = default);
    Task<StranicaRezultata<OtisakUredjajaDto>> SumnjiviAsync(int minRizik, Stranicenje stranicenje, DateTime sada, CancellationToken ct = default);
    Task<StranicaRezultata<BlokadaUredjajaDto>> BlokadeAsync(bool samoAktivne, Stranicenje stranicenje, DateTime sada, CancellationToken ct = default);
}

public interface IAuditRepository
{
    Task DodajAsync(AuditZapis zapis, CancellationToken ct = default);
    Task<StranicaRezultata<AuditZapisDto>> ListaAsync(FilterAudita filter, Stranicenje stranicenje, CancellationToken ct = default);
}

public interface IStatistikaRepository
{
    Task<StatistikaDto> IzracunajAsync(DateTime sada, CancellationToken ct = default);
}
