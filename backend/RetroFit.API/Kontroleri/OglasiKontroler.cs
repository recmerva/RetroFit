using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Oglasi;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;

namespace RetroFit.API.Kontroleri;

/// <summary>Parametri pretrage oglasa (query string).</summary>
public sealed class PretragaParametri
{
    /// <summary>Tekst pretrage (Full-Text Search nad naslovom i opisom).</summary>
    public string? Upit { get; set; }
    /// <summary>Kategorija; uključuje i sve podkategorije.</summary>
    public int? KategorijaId { get; set; }
    public int? GradId { get; set; }
    public decimal? CenaOd { get; set; }
    public decimal? CenaDo { get; set; }
    /// <summary>Jedno ili više stanja: ?stanja=Novo&amp;stanja=KaoNovo</summary>
    public List<StanjeProizvoda>? Stanja { get; set; }
    public TipOglasa? Tip { get; set; }
    public bool? SamoSaDostavom { get; set; }
    /// <summary>Geografska širina korisnika (za pretragu po udaljenosti).</summary>
    public double? Lat { get; set; }
    /// <summary>Geografska dužina korisnika.</summary>
    public double? Lng { get; set; }
    /// <summary>Radijus u kilometrima oko lokacije (ili oko izabranog grada).</summary>
    public double? RadijusKm { get; set; }
    public SortiranjeOglasa Sortiranje { get; set; } = SortiranjeOglasa.Najnovije;
    public int Stranica { get; set; } = 1;
    public int Velicina { get; set; } = 20;
}

public sealed record OglasIzmenaZahtev(int KategorijaId, int GradId, string Naslov, string Opis, TipOglasa Tip, decimal? Cena,
    StanjeProizvoda Stanje, bool DostavaMoguca, bool LicnoPreuzimanjeMoguce, string? ZeljenaZamena, IReadOnlyDictionary<int, string?>? Atributi);

public sealed record RedosledSlikaZahtev(IReadOnlyList<int> SlikeIdURedosledu);

public sealed record PodizanjeZahtev(int BrojDana, string? KljucIdempotentnosti);

public sealed record SacuvanaPretragaZahtev(string Naziv, string? Upit, int? KategorijaId, int? GradId, decimal? CenaOd, decimal? CenaDo,
    StanjeProizvoda? Stanje, TipOglasa? Tip, bool Obavestavaj);

/// <summary>Oglasi: pretraga, detalji, kreiranje i izmena, slike, statusi i plaćeno podizanje.</summary>
[Route(Osnova)]
public sealed class OglasiKontroler : ApiKontroler
{
    /// <summary>
    /// Pretraga aktivnih oglasa sa filterima, sortiranjem i straničenjem.
    /// Rezervisani, prodati, istekli i uklonjeni oglasi se ne vraćaju.
    /// </summary>
    [HttpGet("oglasi")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StranicaRezultata<OglasKarticaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<OglasKarticaDto>>> Pretraga([FromQuery] PretragaParametri p, CancellationToken ct)
    {
        var kriterijumi = new KriterijumiPretrage
        {
            Upit = p.Upit,
            KategorijaId = p.KategorijaId,
            GradId = p.GradId,
            CenaOd = p.CenaOd,
            CenaDo = p.CenaDo,
            Stanja = p.Stanja,
            Tip = p.Tip,
            SamoSaDostavom = p.SamoSaDostavom,
            GeografskaSirina = p.Lat,
            GeografskaDuzina = p.Lng,
            RadijusKm = p.RadijusKm,
            Sortiranje = p.Sortiranje,
            Stranicenje = Stranica(p.Stranica, p.Velicina)
        };
        return Ok(await Mediator.Send(new PretragaOglasaQuery(kriterijumi), ct));
    }

    /// <summary>Izdvojeni i podignuti oglasi za početnu stranu.</summary>
    [HttpGet("oglasi/izdvojeni")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<OglasKarticaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OglasKarticaDto>>> Izdvojeni([FromQuery] int maks = 8, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new IzdvojeniOglasiQuery(maks), ct));

    /// <summary>Detalji oglasa. Telefon prodavca je vidljiv samo kupcu sa prihvaćenom ponudom/rezervacijom.</summary>
    [HttpGet("oglasi/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OglasDetaljiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OglasDetaljiDto>> Detalji(int id, CancellationToken ct)
    {
        var oglas = await Mediator.Send(new OglasDetaljiQuery(id), ct);
        // Pregled se beleži zasebnom komandom (CQRS: upit ne menja stanje).
        if (oglas.Status == StatusOglasa.Aktivan)
        {
            await Mediator.Send(new ZabeleziPregledCommand(id, oglas.Prodavac.Id), ct);
        }

        return Ok(oglas);
    }

    /// <summary>Novi oglas. Ako je objaviOdmah = true, odmah je aktivan (ističe za 60 dana); inače je nacrt.</summary>
    [HttpPost("oglasi")]
    [Authorize]
    [ProducesResponseType(typeof(OglasDetaljiDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OglasDetaljiDto>> Kreiraj(KreirajOglasCommand komanda, CancellationToken ct)
    {
        var oglas = await Mediator.Send(komanda, ct);
        return CreatedAtAction(nameof(Detalji), new { id = oglas.Id, version = "1.0" }, oglas);
    }

    /// <summary>Izmena sopstvenog oglasa (dozvoljena za nacrt, aktivan i istekao oglas).</summary>
    [HttpPut("oglasi/{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(OglasDetaljiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OglasDetaljiDto>> Izmeni(int id, OglasIzmenaZahtev z, CancellationToken ct) =>
        Ok(await Mediator.Send(new IzmeniOglasCommand(id, z.KategorijaId, z.GradId, z.Naslov, z.Opis, z.Tip, z.Cena, z.Stanje,
            z.DostavaMoguca, z.LicnoPreuzimanjeMoguce, z.ZeljenaZamena, z.Atributi), ct));

    /// <summary>Brisanje sopstvenog oglasa (meko brisanje; nije dozvoljeno dok je rezervisan).</summary>
    [HttpDelete("oglasi/{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Obrisi(int id, CancellationToken ct)
    {
        await Mediator.Send(new ObrisiOglasCommand(id), ct);
        return NoContent();
    }

    /// <summary>Objavljivanje nacrta.</summary>
    [HttpPost("oglasi/{id:int}/objava")]
    [Authorize]
    [ProducesResponseType(typeof(OglasDetaljiDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OglasDetaljiDto>> Objavi(int id, CancellationToken ct) =>
        Ok(await Mediator.Send(new PromeniStatusOglasaCommand(id, AkcijaOglasa.Objavi), ct));

    /// <summary>Obnova isteklog oglasa (ili produženje kada ističe za manje od 7 dana) za novih 60 dana.</summary>
    [HttpPost("oglasi/{id:int}/obnova")]
    [Authorize]
    [ProducesResponseType(typeof(OglasDetaljiDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OglasDetaljiDto>> Obnovi(int id, CancellationToken ct) =>
        Ok(await Mediator.Send(new PromeniStatusOglasaCommand(id, AkcijaOglasa.Obnovi), ct));

    /// <summary>Označavanje aktivnog oglasa kao prodatog (prodaja van platforme).</summary>
    [HttpPost("oglasi/{id:int}/prodat")]
    [Authorize]
    [ProducesResponseType(typeof(OglasDetaljiDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OglasDetaljiDto>> OznaciProdatim(int id, CancellationToken ct) =>
        Ok(await Mediator.Send(new PromeniStatusOglasaCommand(id, AkcijaOglasa.OznaciProdatim), ct));

    /// <summary>Otpremanje slika (multipart/form-data, polje "slike"). Najviše 10 slika po oglasu, do 5 MB svaka.</summary>
    [HttpPost("oglasi/{id:int}/slike")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(PoslovnaPravila.MaksSlikaPoOglasu * PoslovnaPravila.MaksVelicinaSlikeBajtova + 1_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = PoslovnaPravila.MaksSlikaPoOglasu * PoslovnaPravila.MaksVelicinaSlikeBajtova + 1_000_000)]
    [ProducesResponseType(typeof(IReadOnlyList<SlikaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SlikaDto>>> DodajSlike(int id, [FromForm] IFormFileCollection slike, CancellationToken ct)
    {
        var fajlovi = slike.Select(f => new OtpremljenFajl(f.OpenReadStream(), f.FileName, f.ContentType, f.Length)).ToList();
        try
        {
            return Ok(await Mediator.Send(new DodajSlikeCommand(id, fajlovi), ct));
        }
        finally
        {
            foreach (var f in fajlovi)
            {
                await f.Sadrzaj.DisposeAsync();
            }
        }
    }

    /// <summary>Uklanjanje slike; preostale slike se automatski prenumerišu.</summary>
    [HttpDelete("oglasi/{id:int}/slike/{slikaId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UkloniSliku(int id, int slikaId, CancellationToken ct)
    {
        await Mediator.Send(new UkloniSlikuCommand(id, slikaId), ct);
        return NoContent();
    }

    /// <summary>Promena redosleda slika (prva u listi postaje glavna slika).</summary>
    [HttpPut("oglasi/{id:int}/slike/redosled")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PromeniRedosled(int id, RedosledSlikaZahtev z, CancellationToken ct)
    {
        await Mediator.Send(new PromeniRedosledSlikaCommand(id, z.SlikeIdURedosledu), ct);
        return NoContent();
    }

    /// <summary>Plaćeno podizanje oglasa (mock plaćanje, 99 RSD po danu). Ključ idempotentnosti sprečava duplu naplatu.</summary>
    [HttpPost("oglasi/{id:int}/podizanje")]
    [Authorize]
    [ProducesResponseType(typeof(PodizanjeRezultatDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PodizanjeRezultatDto>> Podigni(int id, PodizanjeZahtev z, CancellationToken ct) =>
        Ok(await Mediator.Send(new PodigniOglasCommand(id, z.BrojDana, z.KljucIdempotentnosti), ct));

    /// <summary>Moji oglasi (svi statusi osim obrisanih), opciono filtrirani po statusu.</summary>
    [HttpGet("oglasi/moji")]
    [Authorize]
    [ProducesResponseType(typeof(StranicaRezultata<OglasKarticaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<OglasKarticaDto>>> Moji([FromQuery] StatusOglasa? status, [FromQuery] int stranica = 1,
        [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new MojiOglasiQuery(status, Stranica(stranica, velicina)), ct));

    /// <summary>Aktivni oglasi jednog prodavca (javni profil).</summary>
    [HttpGet("korisnici/{korisnikId:int}/oglasi")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StranicaRezultata<OglasKarticaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<OglasKarticaDto>>> OglasiKorisnika(int korisnikId, [FromQuery] int stranica = 1,
        [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new OglasiKorisnikaQuery(korisnikId, Stranica(stranica, velicina)), ct));

    // ---------------- Omiljeni ----------------

    /// <summary>Moji omiljeni oglasi.</summary>
    [HttpGet("omiljeni")]
    [Authorize]
    [ProducesResponseType(typeof(StranicaRezultata<OglasKarticaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<OglasKarticaDto>>> Omiljeni([FromQuery] int stranica = 1, [FromQuery] int velicina = 20,
        CancellationToken ct = default) =>
        Ok(await Mediator.Send(new OmiljeniQuery(Stranica(stranica, velicina)), ct));

    /// <summary>Dodavanje u omiljene (idempotentno).</summary>
    [HttpPut("omiljeni/{oglasId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DodajUOmiljene(int oglasId, CancellationToken ct)
    {
        await Mediator.Send(new DodajUOmiljeneCommand(oglasId), ct);
        return NoContent();
    }

    /// <summary>Uklanjanje iz omiljenih (idempotentno).</summary>
    [HttpDelete("omiljeni/{oglasId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UkloniIzOmiljenih(int oglasId, CancellationToken ct)
    {
        await Mediator.Send(new UkloniIzOmiljenihCommand(oglasId), ct);
        return NoContent();
    }

    // ---------------- Sačuvane pretrage ----------------

    /// <summary>Moje sačuvane pretrage.</summary>
    [HttpGet("sacuvane-pretrage")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<SacuvanaPretragaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SacuvanaPretragaDto>>> SacuvanePretrage(CancellationToken ct) =>
        Ok(await Mediator.Send(new SacuvanePretrageQuery(), ct));

    /// <summary>Čuvanje pretrage. Ako je obavestavaj = true, korisnik dobija obaveštenje za svaki novi oglas koji odgovara.</summary>
    [HttpPost("sacuvane-pretrage")]
    [Authorize]
    [ProducesResponseType(typeof(SacuvanaPretragaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SacuvanaPretragaDto>> SacuvajPretragu(SacuvanaPretragaZahtev z, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Mediator.Send(new SacuvajPretraguCommand(null, z.Naziv, z.Upit, z.KategorijaId,
            z.GradId, z.CenaOd, z.CenaDo, z.Stanje, z.Tip, z.Obavestavaj), ct));

    /// <summary>Izmena sačuvane pretrage.</summary>
    [HttpPut("sacuvane-pretrage/{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(SacuvanaPretragaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SacuvanaPretragaDto>> IzmeniPretragu(int id, SacuvanaPretragaZahtev z, CancellationToken ct) =>
        Ok(await Mediator.Send(new SacuvajPretraguCommand(id, z.Naziv, z.Upit, z.KategorijaId, z.GradId, z.CenaOd, z.CenaDo, z.Stanje,
            z.Tip, z.Obavestavaj), ct));

    /// <summary>Brisanje sačuvane pretrage.</summary>
    [HttpDelete("sacuvane-pretrage/{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ObrisiPretragu(int id, CancellationToken ct)
    {
        await Mediator.Send(new ObrisiPretraguCommand(id), ct);
        return NoContent();
    }
}
